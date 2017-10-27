using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy
{
    public class AzureBusPublisher : IMqPublisher
    {
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly Dictionary<Type, IBinding> _bindings = new Dictionary<Type, IBinding>();
        private readonly MessagingFactory _factory;
        private readonly NamespaceManager _namespaceManager;

        private class Binding<T> : IBinding where T : IRoutingKey, new()
        {
            private readonly MessagingFactory _messagingFactory;
            private readonly Action<string> _logMessage;
            private readonly Action<string> _logError;
            public Type Type { get; } = typeof(T);

            internal Binding(MessagingFactory messagingFactory, NamespaceManager namespaceManager,
                Action<string> logMessage, Action<string> logError)
            {
                _messagingFactory = messagingFactory;
                _logMessage = logMessage;
                _logError = logError;

                var route = new T().RoutingKey;

                if (!namespaceManager.QueueExists(route))
                {
                    namespaceManager.CreateQueue(route);
                }
            }

            public Task SendAsync(T message)
            {
                var sender = _messagingFactory.CreateMessageSender(new T().RoutingKey);

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

                return sender.SendAsync(new BrokeredMessage(stream) { ContentType = "application/json" })
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

        public AzureBusPublisher(MqSettings settings, Action<string> logMessage, Action<string> logError)
        {
            _logMessage = logMessage;
            _logError = logError;
            _factory = MessagingFactory.CreateFromConnectionString(settings.ConnectionString);

            _namespaceManager =
                NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
        }

        public IObservable<Envelope<T>> SendRpc<T>(T message) where T : IRoutingKey, new()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync<T>(T message) where T : IRoutingKey, new()
        {
            if (!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_factory, _namespaceManager, _logMessage, _logError));

            return ((Binding<T>) _bindings[typeof(T)]).SendAsync(message);
        }
    }
}