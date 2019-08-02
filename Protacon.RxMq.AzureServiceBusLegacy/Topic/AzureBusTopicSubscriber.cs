using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy.Topic
{
    public class AzureBusTopicSubscriber: IMqTopicSubscriber
    {
        private readonly AzureTopicMqSettings _settings;
        private readonly Action<string> _logMessage;
        private readonly Action<string> _logError;

        private readonly ConcurrentDictionary<Type, IDisposable> _bindings = new ConcurrentDictionary<Type, IDisposable>();
        private readonly MessagingFactory _factory;
        private readonly NamespaceManager _namespaceManager;

        private readonly BlockingCollection<IBinding> _errorActions = new BlockingCollection<IBinding>(1);
        private readonly CancellationTokenSource _source;

        private class Binding<T> : IDisposable, IBinding where T : new()
        {
            private readonly SubscriptionClient _receiver;
            private readonly OnMessageOptions _options;

            private readonly BlockingCollection<IBinding> _errorActions;
            private readonly Action<string> _logError;

            internal Binding(MessagingFactory messagingFactory, NamespaceManager namespaceManager, AzureTopicMqSettings settings, BlockingCollection<IBinding> errorActions, Action<string> logMessage, Action<string> logError)
            {
                _errorActions = errorActions;
                _logError = logError;
                var topicPath = settings.TopicNameBuilder(typeof(T));
                var subscriptionName = $"{topicPath}.{settings.TopicSubscriberId}";

                MakeSureTopicExists(namespaceManager, settings, topicPath);
                MakeSureSubscriptionExists(namespaceManager, settings, topicPath, subscriptionName);

                _receiver = messagingFactory.CreateSubscriptionClient(topicPath, subscriptionName);
                _receiver.RemoveRule("$default");

                settings.AzureSubscriptionRules
                    .ToList()
                    .ForEach(x => _receiver.AddRule(x.Key, x.Value));

                _options = new OnMessageOptions { AutoComplete = true };
                _options.ExceptionReceived += OptionsOnExceptionReceived;

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
                }, _options);
            }

            private static void MakeSureSubscriptionExists(NamespaceManager namespaceManager, AzureTopicMqSettings settings,
                string topicPath, string subscriptionName)
            {
                if (!namespaceManager.SubscriptionExists(topicPath, subscriptionName))
                {
                    var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName);
                    namespaceManager.CreateSubscription(settings.SubscriptionBuilderConfig(subscriptionDescription, typeof(T)));
                }
            }

            private static void MakeSureTopicExists(NamespaceManager namespaceManager, AzureTopicMqSettings settings,
                string topicPath)
            {
                if (!namespaceManager.TopicExists(topicPath))
                {
                    var queueDescription = new TopicDescription(topicPath);
                    namespaceManager.CreateTopic(settings.TopicBuilderConfig(queueDescription, typeof(T)));
                }
            }

            private void OptionsOnExceptionReceived(object sender, ExceptionReceivedEventArgs exceptionEventArgs)
            {
                _logError($"Action '{exceptionEventArgs.Action}' caused exception {exceptionEventArgs.Exception}.");
                if (exceptionEventArgs.Exception is MessagingEntityNotFoundException || exceptionEventArgs.Exception is MessagingCommunicationException)
                {
                    _errorActions.Add(this);
                }
            }

            public void ReCreate(AzureTopicMqSettings settings, NamespaceManager namespaceManager)
            {
                var topicPath = settings.TopicNameBuilder(typeof(T));
                var subscriptionName = $"{topicPath}.{settings.TopicSubscriberId}";
                MakeSureTopicExists(namespaceManager, settings, topicPath);
                MakeSureSubscriptionExists(namespaceManager, settings, topicPath, subscriptionName);
                UpdateRules(settings);
            }

            private void UpdateRules(AzureTopicMqSettings settings)
            {
                _receiver.RemoveRule("$default");

                settings.AzureSubscriptionRules
                    .ToList()
                    .ForEach(x => _receiver.AddRule(x.Key, x.Value));
            }

            public ReplaySubject<T> Subject { get; } = new ReplaySubject<T>(TimeSpan.FromSeconds(30));

            public void Dispose()
            {
                _options.ExceptionReceived -= OptionsOnExceptionReceived;
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

            _source = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                while (!_source.IsCancellationRequested)
                {
                    try
                    {
                        var action = _errorActions.Take(_source.Token);
                        try
                        {
                            action.ReCreate(_settings, _namespaceManager);
                        }
                        catch (Exception exception)
                        {
                            logError($"Unable to recreate subscription. {exception}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        logError($"Stopping {nameof(AzureBusTopicSubscriber)}");
                    }
                    catch (Exception exception)
                    {
                        logError($"Something went wrong while doing error actions ${exception}.");
                    }
                }
            }, _source.Token);
        }

        public IObservable<T> Messages<T>() where T : new()
        {
            if (!_bindings.ContainsKey(typeof(T)))
            {
                _bindings.TryAdd(typeof(T), new Binding<T>(_factory, _namespaceManager, _settings, _errorActions, _logMessage, _logError));
            }

            return ((Binding<T>)_bindings[typeof(T)]).Subject;
        }

        public void Dispose()
        {
            _source.Cancel();
            _bindings.Select(x => x.Value)
                .ToList()
                .ForEach(x => x.Dispose());
        }

        private interface IBinding
        {
            void ReCreate(AzureTopicMqSettings settings, NamespaceManager namespaceManager);
        }
    }
}