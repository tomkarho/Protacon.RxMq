using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Protacon.RxMq.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureBusSubscriber: IMqSubscriber
    {
        private readonly MqSettings _settings;
        private readonly ILogger<AzureBusSubscriber> _logging;
        private readonly Dictionary<Type, IBinding> _bindings = new Dictionary<Type, IBinding>();

        private class Binding<T>: IBinding where T: IRoutingKey, new()
        {
            public Type Type { get; } = typeof(T);

            internal Binding(MqSettings settings, ILogger<AzureBusSubscriber> logging)
            {
                var route = new T().RoutingKey;

                var queueClient = new QueueClient(settings.ConnectionString, route);

                queueClient.RegisterMessageHandler(
                    async (message, _) =>
                    {
                        try
                        {
                            var body = Encoding.UTF8.GetString(message.Body);

                            logging.LogInformation($"Received '{route}': {body}");

                            Subject.OnNext(new Envelope<T>(JObject.Parse(body)["data"].ToObject<T>(), new MessageAckAzureServiceBus(queueClient, message.SystemProperties.LockToken)));
                        }
                        catch (Exception ex)
                        {
                            logging.LogError($"Message {route}': {message} -> consumer error: {ex}");
                        }
                    }, new MessageHandlerOptions(async e =>
                    {
                        logging.LogError($"At route '{route}' error occurred: {e.Exception}");
                    }));
            }

            public Subject<Envelope<T>> Subject { get; } = new Subject<Envelope<T>>();

            public void Dispose()
            {
                Subject?.Dispose();
            }
        }

        public AzureBusSubscriber(IOptions<MqSettings> settings, ILogger<AzureBusSubscriber> logging)
        {
            _settings = settings.Value;
            _logging = logging;
        }

        public IObservable<Envelope<T>> Messages<T>() where T: IRoutingKey, new()
        {
            if(!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_settings, _logging));

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
