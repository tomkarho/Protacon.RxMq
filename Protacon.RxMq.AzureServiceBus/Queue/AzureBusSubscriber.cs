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
    public class AzureQueueSubscriber: IMqQueSubscriber
    {
        private readonly AzureBusQueueSettings _settings;
        private readonly AzureBusQueueManagement _queueManagement;
        private readonly ILogger<AzureQueueSubscriber> _logging;
        private readonly Dictionary<Type, IDisposable> _bindings = new Dictionary<Type, IDisposable>();

        private class Binding<T>: IDisposable where T: new()
        {
            internal Binding(AzureBusQueueSettings settings, ILogger<AzureQueueSubscriber> logging, AzureBusQueueManagement queueManagement)
            {
                var queueName = settings.QueueNameBuilderForSubscriber(typeof(T));

                queueManagement.CreateQueIfMissing(queueName, typeof(T));

                var queueClient = new QueueClient(settings.ConnectionString, queueName);

                queueClient.RegisterMessageHandler(
                    async (message, _) =>
                    {
                        try
                        {
                            var body = Encoding.UTF8.GetString(message.Body);

                            logging.LogInformation($"Received '{queueName}': {body}");

                            var asObject = AsObject(body);

                            Subject.OnNext(asObject);
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

            public Subject<T> Subject { get; } = new Subject<T>();

            public void Dispose()
            {
                Subject?.Dispose();
            }
        }

        public AzureQueueSubscriber(IOptions<AzureBusQueueSettings> settings, AzureBusQueueManagement queueManagement, ILogger<AzureQueueSubscriber> logging)
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
