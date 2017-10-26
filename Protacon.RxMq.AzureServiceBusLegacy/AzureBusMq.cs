using Protacon.RxMq.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Protacon.RxMq.AzureServiceBusLegacy
{
    public class AzureBusMq: IMq
    {
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly Dictionary<Type, IBinding> _bindings = new Dictionary<Type, IBinding>();
        private MessagingFactory _factory;
        private NamespaceManager _namespaceManager;

        private class Binding<T> : IBinding where T : IRoutingKey, new()
        {
            private readonly MessagingFactory _messagingFactory;
            public Type Type { get; } = typeof(T);

            internal Binding(MessagingFactory messagingFactory, NamespaceManager namespaceManager, Action<string> logMessage, Action<string> logError)
            {
                _messagingFactory = messagingFactory;

                var route = new T().RoutingKey;

                var receiver = messagingFactory.CreateMessageReceiver(route, ReceiveMode.PeekLock);

                if (!namespaceManager.QueueExists(route))
                {
                    namespaceManager.CreateQueue(route);
                }

                receiver.OnMessage(message =>
                {
                    try
                    {
                        var body = Encoding.UTF8.GetString(message.GetBody<byte[]>());

                        logMessage($"Received '{route}': {body}");

                        Subject.OnNext(new Envelope<T>(JObject.Parse(body)["data"].ToObject<T>(), new MessageAckAzureServiceBus(message)));
                        message.Complete();
                    }
                    catch (Exception ex)
                    {
                        logError($"Message {route}': {message} -> consumer error: {ex}");
                    }
                }, new OnMessageOptions { AutoComplete = true });
            }

            public Subject<Envelope<T>> Subject { get; } = new Subject<Envelope<T>>();

            public Task SendAsync(T message)
            {
                var sender = _messagingFactory.CreateMessageSender(new T().RoutingKey);
                var body = new BrokeredMessage(Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new { Data = message },
                        Formatting.None,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    )));

                return sender.SendAsync(body);
            }

            public void Dispose()
            {
                Subject?.Dispose();
            }
        }

        public AzureBusMq(MqSettings settings, Action<string> logMessage, Action<string> logError)
        {
            _logMessage = logMessage;
            _logError = logError;
            _factory = MessagingFactory.CreateFromConnectionString(settings.ConnectionString);

            _namespaceManager =
                NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
        }

        public IObservable<Envelope<T>> Messages<T>() where T : IRoutingKey, new()
        {
            if (!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_factory, _namespaceManager, _logMessage, _logError));

            return ((Binding<T>)_bindings[typeof(T)]).Subject;
        }

        public void Dispose()
        {
            _bindings.Select(x => x.Value)
                .ToList()
                .ForEach(x => x.Dispose());
        }

        public IObservable<Envelope<T>> SendRpc<T>(T message) where T : IRoutingKey, new()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync<T>(T message) where T : IRoutingKey, new()
        {
            if (!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_factory, _namespaceManager, _logMessage, _logError));

            return ((Binding<T>)_bindings[typeof(T)]).SendAsync(message);
        }
    }
}
