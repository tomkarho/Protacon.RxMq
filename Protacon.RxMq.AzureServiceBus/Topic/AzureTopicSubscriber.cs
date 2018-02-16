using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json.Linq;
using Protacon.RxMq.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureTopicSubscriber: IMqTopicSubscriber
    {
        private readonly AzureBusTopicSettings _settings;
        private readonly AzureBusTopicManagement _queueManagement;
        private readonly ILogger<AzureTopicSubscriber> _logging;
        private readonly Dictionary<Type, IDisposable> _bindings = new Dictionary<Type, IDisposable>();

        private class Binding<T>: IDisposable where T: new()
        {
            internal Binding(AzureBusTopicSettings settings, ILogger<AzureTopicSubscriber> logging, AzureBusTopicManagement queueManagement)
            {
                var topicName = settings.TopicNameBuilderForSubscriber(typeof(T));
                var subscriptionName = $"{topicName}.{settings.TopicSubscriberId}";

                queueManagement.CreateSubscriptionIfMissing(topicName, subscriptionName, typeof(T));

                var subscriptionClient = new SubscriptionClient(settings.ConnectionString, topicName, subscriptionName);

                subscriptionClient.RegisterMessageHandler(
                    async (message, _) =>
                    {
                        try
                        {
                            var body = Encoding.UTF8.GetString(message.Body);

                            logging.LogInformation($"Received '{subscriptionName}': {body}");

                            var asObject = AsObject(body);

                            Subject.OnNext(asObject);
                        }
                        catch (Exception ex)
                        {
                            logging.LogError($"Message {subscriptionName}': {message} -> consumer error: {ex}");
                        }
                    }, new MessageHandlerOptions(async e =>
                    {
                        logging.LogError($"At route '{subscriptionName}' error occurred: {e.Exception}");
                    }));
            }

            private static T AsObject(string body)
            {
                var parsed = JObject.Parse(body);

                if (parsed["data"] == null)
                    throw new InvalidOperationException("Library expects data wrapped as { data: { ... } }");

                return parsed["data"].ToObject<T>();
            }

            public Subject<T> Subject { get; } = new Subject<T>();

            public void Dispose()
            {
                Subject?.Dispose();
            }
        }

        public AzureTopicSubscriber(IOptions<AzureBusTopicSettings> settings, AzureBusTopicManagement queueManagement, ILogger<AzureTopicSubscriber> logging)
        {
            _settings = settings.Value;
            _queueManagement = queueManagement;
            _logging = logging;
        }

        public IObservable<T> Messages<T>() where T: new()
        {
            if(!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_settings, _logging, _queueManagement));

            return ((Binding<T>) _bindings[typeof(T)]).Subject;
        }

        public void Dispose()
        {
            _bindings.Select(x => x.Value)
                .ToList()
                .ForEach(x => x.Dispose());
        }
    }
}
