using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy.Topic
{
    public class AzureBusTopicPublisher : IMqQuePublisher
    {
        private readonly AzureTopicMqSettings _settings;
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly Dictionary<string, IDisposable> _bindings = new Dictionary<string, IDisposable>();
        private readonly MessagingFactory _factory;
        private readonly NamespaceManager _namespaceManager;

        private class Binding<T> : IDisposable where T : new()
        {
            private readonly MessagingFactory _messagingFactory;
            private readonly Action<string> _logMessage;
            private readonly Action<string> _logError;
            private AzureTopicMqSettings _azureTopicMqSettings;

            internal Binding(
                MessagingFactory messagingFactory,
                NamespaceManager namespaceManager,
                AzureTopicMqSettings settings,
                string topicName,
                Action<string> logMessage, Action<string> logError)
            {
                _messagingFactory = messagingFactory;
                _logMessage = logMessage;
                _logError = logError;
                _azureTopicMqSettings = settings;

                if (!namespaceManager.TopicExists(topicName))
                {
                    var queueDescription = new TopicDescription(topicName);
                    namespaceManager.CreateTopic(settings.TopicBuilderConfig(queueDescription, typeof(T)));
                }
            }

            public Task SendAsync(T message, string queueName)
            {
                var sender = _messagingFactory.CreateMessageSender(queueName);

                var body =
                    JsonConvert.SerializeObject(
                        new {Data = message},
                        Formatting.None,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    );

                _logMessage($"{nameof(SendAsync)} sending message '{body}'");

                var bytes = Encoding.UTF8.GetBytes(body);
                var stream = new MemoryStream(bytes, writable: false);

                var brokeredMessage = new BrokeredMessage(stream)
                {
                    ContentType = "application/json"
                };

                _azureTopicMqSettings.AzureMessagePropertyBuilder(message)
                    .ToList()
                    .ForEach(x => brokeredMessage.Properties.Add(x.Key, x.Value));

                return sender.SendAsync(brokeredMessage)
                    .ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            _logError($"{nameof(SendAsync)} error occurred: {task.Exception}");
                        }

                        return task;
                    });
            }

            public void Dispose()
            {
            }
        }

        public AzureBusTopicPublisher(AzureTopicMqSettings settings, Action<string> logMessage, Action<string> logError)
        {
            _settings = settings;
            _logMessage = logMessage;
            _logError = logError;
            _factory = MessagingFactory.CreateFromConnectionString(settings.ConnectionString);

            _namespaceManager =
                NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
        }

        public Task SendAsync<T>(T message) where T : new()
        {
            var queueName = _settings.TopicNameBuilderForPublisher(message);

            if (!_bindings.ContainsKey(queueName))
                _bindings.Add(queueName, new Binding<T>(_factory, _namespaceManager, _settings, queueName
                    , _logMessage, _logError));

            return ((Binding<T>) _bindings[queueName]).SendAsync(message, queueName);
        }
    }
}