using Protacon.RxMq.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;

namespace Protacon.RxMq.AzureServiceBusLegacy
{
    public class AzureBusSubscriber: IMqSubscriber
    {
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly Dictionary<Type, IDisposable> _bindings = new Dictionary<Type, IDisposable>();
        private readonly MessagingFactory _factory;
        private readonly NamespaceManager _namespaceManager;

        private class Binding<T> : IDisposable where T : new()
        {
            private readonly MessageReceiver _receiver;
            public Type Type { get; } = typeof(T);

            internal Binding(MessagingFactory messagingFactory, NamespaceManager namespaceManager, Action<string> logMessage, Action<string> logError)
            {
                // TODO: Implement dynamic routing.
                var route = ((IRoutingKey)new T()).RoutingKey;

                _receiver = messagingFactory.CreateMessageReceiver(route, ReceiveMode.PeekLock);

                if (!namespaceManager.QueueExists(route))
                {
                    namespaceManager.CreateQueue(route);
                }

                _receiver.OnMessage(message =>
                {
                    try
                    {
                        var bodyStream = message.GetBody<Stream>();

                        using (var reader = new StreamReader(bodyStream))
                        {
                            var body = reader.ReadToEnd();

                            logMessage($"Received '{route}': {body}");

                            Subject.OnNext(new Envelope<T>(JObject.Parse(body)["data"].ToObject<T>(),
                                new MessageAckAzureServiceBus(message)));
                        }
                    }
                    catch (Exception ex)
                    {
                        logError($"Message {route}': {message} -> consumer error: {ex}");
                    }
                }, new OnMessageOptions { AutoComplete = true });
            }

            public Subject<Envelope<T>> Subject { get; } = new Subject<Envelope<T>>();

            public void Dispose()
            {
                Subject?.Dispose();
                _receiver.Close();
            }
        }

        public AzureBusSubscriber(MqSettings settings, Action<string> logMessage, Action<string> logError)
        {
            _logMessage = logMessage;
            _logError = logError;
            _factory = MessagingFactory.CreateFromConnectionString(settings.ConnectionString);

            _namespaceManager =
                NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
        }

        public IObservable<Envelope<T>> Messages<T>() where T : new()
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
    }
}
