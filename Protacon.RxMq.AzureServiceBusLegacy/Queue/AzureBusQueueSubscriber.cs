using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy.Queue
{
    public class AzureBusQueueSubscriber: IMqQueueSubscriber
    {
        private readonly AzureQueueMqSettings _settings;
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly Dictionary<Type, IDisposable> _bindings = new Dictionary<Type, IDisposable>();
        private readonly MessagingFactory _factory;
        private readonly NamespaceManager _namespaceManager;

        private class Binding<T> : IDisposable where T : new()
        {
            private readonly MessageReceiver _receiver;

            internal Binding(MessagingFactory messagingFactory, NamespaceManager namespaceManager, AzureQueueMqSettings settings, Action<string> logMessage, Action<string> logError)
            {
                var queueName = settings.QueueNameBuilderForSubscriber(typeof(T));

                _receiver = messagingFactory.CreateMessageReceiver(queueName, ReceiveMode.PeekLock);

                if (!namespaceManager.QueueExists(queueName))
                {
                    var queueDescription = new QueueDescription(queueName);
                    namespaceManager.CreateQueue(settings.QueueBuilderConfig(queueDescription, typeof(T)));
                }

                _receiver.OnMessage(message =>
                {
                    try
                    {
                        var bodyStream = message.GetBody<Stream>();

                        using (var reader = new StreamReader(bodyStream))
                        {
                            var body = reader.ReadToEnd();

                            logMessage($"Received '{queueName}': {body}");

                            Subject.OnNext(JObject.Parse(body)["data"].ToObject<T>());
                        }
                    }
                    catch (Exception ex)
                    {
                        logError($"Message {queueName}': {message} -> consumer error: {ex}");
                    }
                }, new OnMessageOptions { AutoComplete = true });
            }

            public ReplaySubject<T> Subject { get; } = new ReplaySubject<T>(TimeSpan.FromSeconds(30));

            public void Dispose()
            {
                Subject?.Dispose();
                _receiver.Close();
            }
        }

        public AzureBusQueueSubscriber(AzureQueueMqSettings settings, Action<string> logMessage, Action<string> logError)
        {
            _settings = settings;
            _logMessage = logMessage;
            _logError = logError;
            _factory = MessagingFactory.CreateFromConnectionString(settings.ConnectionString);

            _namespaceManager =
                NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
        }

        public IObservable<T> Messages<T>() where T : new()
        {
            if (!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_factory, _namespaceManager, _settings, _logMessage, _logError));

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
