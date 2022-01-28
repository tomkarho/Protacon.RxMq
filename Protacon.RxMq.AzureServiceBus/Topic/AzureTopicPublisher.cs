using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus.Topic
{
    public class AzureTopicPublisher : IMqTopicPublisher
    {
        private readonly AzureBusTopicSettings _settings;
        private readonly AzureBusTopicManagement _topicManagement;
        private readonly ILogger<AzureTopicPublisher> _logger;
        private readonly ConcurrentDictionary<string, Binding> _bindings = new ConcurrentDictionary<string, Binding>();
        private const int TriesBeforeInterval = 5;
        private int RetrySendMsgCount = 0;
        private const int IntervalOfBindingRetry = 60;
        private const int maxSendAsyncRetries = 3;

        private class Binding : IDisposable
        {
            private readonly TopicClient _topicClient;
            private readonly ILogger<AzureTopicPublisher> _logger;
            private readonly AzureBusTopicSettings _settings;
            private readonly string _topic;
            private readonly IList<string> _excludeTopicsFromLogging;

            internal Binding(
                AzureBusTopicSettings settings,
                AzureBusTopicManagement queueManagement,
                string topic,
                Type type,
                ILogger<AzureTopicPublisher> logger)
            {
                _logger = logger;
                _settings = settings;
                _topic = topic;
                _excludeTopicsFromLogging = new LoggingConfiguration().ExcludeTopicsFromLogging();
                queueManagement.CreateTopicIfMissing(_topic, type);

                var retryPolicy = new RetryExponential(
                    TimeSpan.FromSeconds(settings.AzureRetryMinimumBackoff),
                    TimeSpan.FromSeconds(settings.AzureRetryMaximumBackoff),
                    settings.AzureMaximumRetryCount
                );
                _topicClient = new TopicClient(settings.ConnectionString, _topic, retryPolicy);
                _logger.LogInformation("Created new MQ binding '{topic}'.", _topic);
            }

            public Task SendAsync(object message)
            {
                var asJson = JsonConvert.SerializeObject(
                    new { Data = message },
                    Formatting.None,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });

                if (!_excludeTopicsFromLogging.Contains(_topic))
                {
                    _logger.LogInformation("{method}/{topic} sending message to queue '{message}'", nameof(SendAsync), _topic, message);
                }

                var contentJsonBytes = Encoding.UTF8.GetBytes(asJson);

                var body = new Message(contentJsonBytes)
                {
                    ContentType = "application/json"
                };

                _settings.AzureMessagePropertyBuilder(message)
                    .ToList()
                    .ForEach(body.UserProperties.Add);

                return _topicClient.SendAsync(body);
            }

            public void Dispose()
            {
                _topicClient.CloseAsync();
            }
        }

        public AzureTopicPublisher(IOptions<AzureBusTopicSettings> settings, AzureBusTopicManagement topicManagement,
            ILogger<AzureTopicPublisher> logging)
        {
            _settings = settings.Value;
            _topicManagement = topicManagement;
            _logger = logging;
        }

        public void Dispose()
        {
            _bindings.Select(x => x.Value)
                .ToList()
                .ForEach(x => x.Dispose());
        }

        public Task SendAsync<T>(T message) where T : new()
        {
            try
            {
                var topic = _settings.TopicNameBuilder(message.GetType());

                if (!_bindings.ContainsKey(topic))
                    return TryCreateBinding(topic, typeof(T), message, TriesBeforeInterval, TimeSpan.FromSeconds(IntervalOfBindingRetry));

                return _bindings[topic].SendAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{method}: Failed to send async message '{message}' '{exceptionMessage}'{newLine}'{stackTrace}'", nameof(SendAsync), message, e.Message, Environment.NewLine, e.StackTrace);
                if (RetrySendMsgCount < maxSendAsyncRetries)
                {
                    RetrySendMsgCount++;
                    _logger.LogInformation("{method}: trying to send message again,  Trying to send message again '{sendCount}/{maxSendAsyncRetries}'", nameof(SendAsync), RetrySendMsgCount, maxSendAsyncRetries);
                    return SendAsync(message);
                }
                else
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Creates new Binding for topic in a safe way. If initial tries fail, it will fallback to intervalled retrys.
        /// </summary>
        /// <param name="topic">Name of the topic to bind</param>
        /// <param name="type">Message type for the topic builder</param>
        /// <param name="message">Message to send after binding</param>
        /// <param name="instantRecoveryTries">Number of instant tries for binding. Fallbacks to <see cref="lifeCycleRecoveryInterval"/>interval.</param>
        /// <param name="lifeCycleRecoveryInterval">Interval to try binding in seconds.</param>
        private Task TryCreateBinding(string topic, Type type, object message, int instantRecoveryTries, TimeSpan lifeCycleRecoveryInterval)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            int lifeCycleTryCount = 0;

            for (var i = 1; i <= instantRecoveryTries; i++)
            {
                _logger.LogDebug(
                    "{method}: Try {tryNumber} of {instantRecoveryTries} for binding ('{topic}')", nameof(TryCreateBinding), i, topic);

                var binding = TryBinding(topic, type, message);
                if (binding != null)
                {
                    bool added = _bindings.TryAdd(topic, binding);
                    var bindingInfo = added ? "added to bindings list" : "already existed in bindings list";
                    _logger.LogInformation(
                        "{method}: Binding successful ('{topic}', binding {tryNumber} of {instantRecoveryTries}, {bindingInfo})", nameof(TryCreateBinding), topic, i, instantRecoveryTries, bindingInfo);

                    return _bindings[topic].SendAsync(message);
                }
            }

            _logger.LogInformation(
                "{method}: Could not create binding in instantRecoveryTries tries ('{topic}', {instantRecoveryTries} tries). " +
                "Trying again every {lifeCycleRecoveryInterval} sec.", nameof(TryCreateBinding), topic, instantRecoveryTries, lifeCycleRecoveryInterval);

            RepeatActionEvery(TryLifeCycleBinding, lifeCycleRecoveryInterval, cancellation.Token)
                .Wait();
            return _bindings[topic].SendAsync(message);

            void TryLifeCycleBinding()
            {
                lifeCycleTryCount++;
                _logger.LogInformation(
                    "{method}: Try {lifeCycleTryCount} of lifeCycleRecoveryInterval ('{topic}')", nameof(TryCreateBinding), lifeCycleTryCount, topic);
                var binding = TryBinding(topic, type, message);
                if (binding != null)
                {
                    bool added = _bindings.TryAdd(topic, binding);
                    var bindingInfo = added ? "added to bindings list" : "already existed in bindings list";
                    _logger.LogInformation(
                        "{method}: Binding successful ('{topic}', binding {lifeCycleTryCount} of lifeCycleRecoveryInterval, {bindingInfo})", nameof(TryCreateBinding), topic, lifeCycleTryCount, bindingInfo);
                    cancellation.Cancel();
                }
            }
        }

        private Binding TryBinding(string topic, Type type, object message)
        {
            try
            {
                _topicManagement.CreateTopicIfMissing(topic, message.GetType());
                return new Binding(_settings, _topicManagement, topic, type, _logger);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "{method}: Calling recovery on topic '{topic}' for new Binding. Cause: error occurred {e}", nameof(TryBinding), topic, e);
                return null;
            }
        }

        private static async Task RepeatActionEvery(Action action, TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                action();
                Task task = Task.Delay(interval, cancellationToken);
                try
                {
                    await task;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}
