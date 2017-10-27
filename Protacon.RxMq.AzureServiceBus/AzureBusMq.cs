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
    public class AzureBusMq: IMqSubscriber, IDisposable
    {
        private readonly MqSettings _settings;
        private readonly ILogger<AzureBusMq> _logging;
        private readonly Dictionary<Type, IBinding> _bindings = new Dictionary<Type, IBinding>();

        private class Binding<T>: IBinding where T: IRoutingKey, new()
        {
            private readonly QueueClient _queueClient;
            public Type Type { get; } = typeof(T);

            internal Binding(MqSettings settings, ILogger<AzureBusMq> logging)
            {
                var route = new T().RoutingKey;

                _queueClient = new QueueClient(settings.ConnectionString, route);
                _queueClient.RegisterMessageHandler(
                    async (message, _) =>
                    {
                        var body = Encoding.UTF8.GetString(message.Body);

                        logging.LogInformation($"Received '{route}': {body}");

                        try
                        {
                            Subject.OnNext(new Envelope<T>(JObject.Parse(body)["data"].ToObject<T>(), new MessageAckAzureServiceBus(_queueClient, message.SystemProperties.LockToken)));
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

            public Task SendAsync(T message)
            {
                var body = new Message(Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new { Data = message },
                        Formatting.None,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    )));

                return _queueClient.SendAsync(body);
            }

            public void Dispose()
            {
                Subject?.Dispose();
            }
        }

        public AzureBusMq(IOptions<MqSettings> settings, ILogger<AzureBusMq> logging)
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

        public IObservable<Envelope<T>> SendRpc<T>(T message) where T : IRoutingKey, new()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync<T>(T message) where T : IRoutingKey, new()
        {
            if (!_bindings.ContainsKey(typeof(T)))
                _bindings.Add(typeof(T), new Binding<T>(_settings, _logging));

            return ((Binding<T>)_bindings[typeof(T)]).SendAsync(message);
        }
    }
}
