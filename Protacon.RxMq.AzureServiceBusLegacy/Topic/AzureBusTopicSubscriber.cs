using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy.Topic
{
    public class AzureBusTopicSubscriber: IMqQueSubscriber
    {
        private readonly AzureTopicMqSettings _settings;
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly Dictionary<Type, IDisposable> _bindings = new Dictionary<Type, IDisposable>();
        private readonly MessagingFactory _factory;
        private readonly NamespaceManager _namespaceManager;

        private class Binding<T> : IDisposable where T : new()
        {
            private readonly SubscriptionClient _receiver;

            internal Binding(MessagingFactory messagingFactory, NamespaceManager namespaceManager, AzureTopicMqSettings settings, Action<string> logMessage, Action<string> logError)
            {
                var topicPath = settings.TopicNameBuilder(typeof(T));
                var subscriptionName = $"{topicPath}.{settings.TopicSubscriberId}";

                if (!namespaceManager.TopicExists(topicPath))
                {
                    var queueDescription = new TopicDescription(topicPath);
                    namespaceManager.CreateTopic(settings.TopicBuilderConfig(queueDescription, typeof(T)));
                }

                if (!namespaceManager.SubscriptionExists(topicPath, subscriptionName))
                {
                    var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName);
                    namespaceManager.CreateSubscription(settings.SubscriptionBuilderConfig(subscriptionDescription, typeof(T)));
                }

                _receiver = messagingFactory.CreateSubscriptionClient(topicPath, subscriptionName);
                _receiver.RemoveRule("$default");

                settings.AzureSubscriptionRules
                    .ToList()
                    .ForEach(x => _receiver.AddRule(x.Key, x.Value));

                _receiver.OnMessage(message =>
                {
                    try
                    {
                        var bodyStream = message.GetBody<Stream>();

                        using (var reader = new StreamReader(bodyStream))
                        {
                            var body = reader.ReadToEnd();

                            logMessage($"Received '{topicPath}': {body}");

                            Subject.OnNext(JObject.Parse(body)["data"].ToObject<T>());
                        }
                    }
                    catch (Exception ex)
                    {
                        logError($"Message {topicPath}': {message} -> consumer error: {ex}");
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

        public AzureBusTopicSubscriber(AzureTopicMqSettings settings, Action<string> logMessage, Action<string> logError)
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