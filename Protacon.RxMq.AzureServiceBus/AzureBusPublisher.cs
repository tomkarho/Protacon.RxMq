using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureBusPublisher: IMqPublisher
    {
        private readonly MqSettings _settings;
        private readonly ILogger<AzureBusPublisher> _logging;
        private readonly Dictionary<Type, IBinding> _bindings = new Dictionary<Type, IBinding>();

        private class Binding<T>: IBinding where T: IRoutingKey, new()
        {
            private readonly ILogger<AzureBusPublisher> _logging;
            private readonly QueueClient _queueClient;
            public Type Type { get; } = typeof(T);

            internal Binding(MqSettings settings, ILogger<AzureBusPublisher> logging)
            {
                _logging = logging;

                var route = new T().RoutingKey;
                _queueClient = new QueueClient(settings.ConnectionString, route);
            }

            public Task SendAsync(T message)
            {
                var contentJsonBytes = Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new {Data = message},
                        Formatting.None,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        }
                    ));

                var body = new Message(contentJsonBytes) { ContentType = "application/json" };

                return _queueClient.SendAsync(body);
            }

            public void Dispose()
            {
            }
        }

        public AzureBusPublisher(IOptions<MqSettings> settings, ILogger<AzureBusPublisher> logging)
        {
            _settings = settings.Value;
            _logging = logging;
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