using System;
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
        private readonly Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

        private class Binding : IDisposable
        {
            private readonly TopicClient _topicClient;
            private readonly ILogger<AzureTopicPublisher> _logger;
            private readonly AzureBusTopicSettings _settings;

            internal Binding(
                AzureBusTopicSettings settings,
                AzureBusTopicManagement queueManagement,
                string topic,
                Type type,
                ILogger<AzureTopicPublisher> logger)
            {
                _logger = logger;
                _settings = settings;

                queueManagement.CreateTopicIfMissing(topic, type);

                _topicClient = new TopicClient(settings.ConnectionString, topic);

                _logger.LogInformation($"Created new MQ binding '{topic}'.");

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

                _logger.LogInformation($"Sending message to queue '{message}'");

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
            var topic = _settings.TopicNameBuilder(message.GetType());

            if (!_bindings.ContainsKey(topic))
            {
                return TryCreateBinding(topic, typeof(T), message, 5, 10); // TODO 60
            }

            return _bindings[topic].SendAsync(message);
        }

        /// <summary>
        /// Creates new Binding for topic in a safe way. If initial tries fail, it will fallback to intervalled retrys.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="instantRecoveryTries"></param>
        /// <param name="lifeCycleRecoveryInterval"></param>
        private Task TryCreateBinding(string topic, Type type, object message, int instantRecoveryTries, int lifeCycleRecoveryInterval)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Binding binding;
            int lifeCycleTryCount = 0;

            for (var i = 1; i <= instantRecoveryTries; i++)
            {
                _logger.LogInformation(
                    $"TryCreateBinding: Try {i} of {instantRecoveryTries} for binding ('{topic}')");

                binding = TryBinding(topic, type, message);
                if (binding != null)
                {
                    _logger.LogInformation(
                        $"TryCreateBinding: Binding successful ('{topic}', binding {i} of {instantRecoveryTries})");
                    _bindings.Add(topic, binding);
                    return _bindings[topic].SendAsync(message);
                }
            }

            _logger.LogInformation(
                $"TryCreateBinding: Could not create binding in instantRecoveryTries tries ('{topic}', {instantRecoveryTries} tries). " +
                $"Trying again every {lifeCycleRecoveryInterval} sec.");

            RepeatActionEvery(TryLifeCycleBinding, lifeCycleRecoveryInterval, cancellation.Token)
                .Wait();
            return _bindings[topic].SendAsync(message);

            void TryLifeCycleBinding()
            {
                lifeCycleTryCount++;
                _logger.LogInformation(
                    $"TryCreateBinding: Try {lifeCycleTryCount} of lifeCycleRecoveryInterval ('{topic}')");
                binding = TryBinding(topic, type, message);
                if (binding != null)
                {
                    _logger.LogInformation(
                        $"TryCreateBinding: Binding successful ('{topic}', binding {lifeCycleTryCount} of lifeCycleRecoveryInterval)");
                    _bindings.Add(topic, binding);
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
                _logger.LogError(
                    $"TryCreateBinding: Calling recovery on topic '{topic}' for new Binding. Cause: error occurred {e}");
                return null;
            }
        }

        private async Task RepeatActionEvery(Action action, int interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                action();
                Task task = Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
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