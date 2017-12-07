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
    public class AzureBusSubscriber: IMqSubscriber
    {
        private readonly MqSettings _settings;
        private readonly AzureQueueManagement _queueManagement;
        private readonly ILogger<AzureBusSubscriber> _logging;
        private readonly Dictionary<Type, IDisposable> _bindings = new Dictionary<Type, IDisposable>();

        private class Binding<T>: IDisposable where T: new()
        {
            internal Binding(MqSettings settings, ILogger<AzureBusSubscriber> logging, AzureQueueManagement queueManagement)
            {
                var queueName = settings.QueueNameBuilderForSubscriber(typeof(T));

                queueManagement.CreateIfMissing(queueName);

                var queueClient = new QueueClient(settings.ConnectionString, queueName);

                queueClient.RegisterMessageHandler(
                    async (message, _) =>
                    {
                        try
                        {
                            var body = Encoding.UTF8.GetString(message.Body);

                            logging.LogInformation($"Received '{queueName}': {body}");

                            var asObject = AsObject(body);

                            Subject.OnNext(
                                new Envelope<T>(asObject,
                                new MessageAckAzureServiceBus(queueClient, message.SystemProperties.LockToken)));
                        }
                        catch (Exception ex)
                        {
                            logging.LogError($"Message {queueName}': {message} -> consumer error: {ex}");
                        }
                    }, new MessageHandlerOptions(async e =>
                    {
                        logging.LogError($"At route '{queueName}' error occurred: {e.Exception}");
                    }));
            }

            private static T AsObject(string body)
            {
                var parsed = JObject.Parse(body);

                if (parsed["data"] == null)
                    throw new InvalidOperationException("Library expects data wrapped as { data: { ... } }");

                return parsed["data"].ToObject<T>();
            }

            public Subject<Envelope<T>> Subject { get; } = new Subject<Envelope<T>>();

            public void Dispose()
            {
                Subject?.Dispose();
            }
        }

        public AzureBusSubscriber(IOptions<MqSettings> settings, AzureQueueManagement queueManagement, ILogger<AzureBusSubscriber> logging)
        {
            _settings = settings.Value;
            _queueManagement = queueManagement;
            _logging = logging;
        }

        public IObservable<Envelope<T>> Messages<T>() where T: new()
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
