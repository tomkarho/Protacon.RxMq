using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus.Topic
{
    public class AzureTopicPublisher: IMqTopicPublisher
    {
        private readonly AzureBusTopicSettings _settings;
        private readonly AzureBusTopicManagement _topicManagement;
        private readonly ILogger<AzureTopicPublisher> _logger;
        private readonly Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

        private class Binding: IDisposable
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
                queueManagement.CreateTopicIfMissing(topic, type);

                _topicClient = new TopicClient(settings.ConnectionString, topic);

                logger.LogInformation($"Created new MQ binding '{topic}'.");
                _logger = logger;
                _settings = settings;
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

                _logger.LogDebug($"Sending message to queue '{message}'");

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

        public AzureTopicPublisher(IOptions<AzureBusTopicSettings> settings, AzureBusTopicManagement topicManagement, ILogger<AzureTopicPublisher> logging)
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
            var topic = _settings.TopicNameBuilderForPublisher(message);

            if (!_bindings.ContainsKey(topic))
                _bindings.Add(topic, new Binding(_settings, _topicManagement, topic, typeof(T), _logger));

            return _bindings[topic].SendAsync(message);
        }
    }
}