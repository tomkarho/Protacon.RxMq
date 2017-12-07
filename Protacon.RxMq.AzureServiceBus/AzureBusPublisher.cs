using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly AzureQueueManagement _queueManagement;
        private readonly ILogger<AzureBusPublisher> _logging;
        private readonly Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

        private class Binding: IDisposable
        {
            private readonly QueueClient _queueClient;

            internal Binding(MqSettings settings, ILogger<AzureBusPublisher> logging, AzureQueueManagement queueManagement, string queue)
            {
                queueManagement.CreateIfMissing(queue);
                _queueClient = new QueueClient(settings.ConnectionString, queue);

                logging.LogDebug($"Created new MQ binding '{queue}'.");

                queueManagement.CreateIfMissing(queue);
            }

            public Task SendAsync(object message)
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
                _queueClient.CloseAsync();
            }
        }

        public AzureBusPublisher(IOptions<MqSettings> settings, AzureQueueManagement queueManagement, ILogger<AzureBusPublisher> logging)
        {
            _settings = settings.Value;
            _queueManagement = queueManagement;
            _logging = logging;
        }

        public void Dispose()
        {
            _bindings.Select(x => x.Value)
                .ToList()
                .ForEach(x => x.Dispose());
        }
        public Task SendAsync<T>(T message) where T : IRoutingKey, new()
        {
            var queue = _settings.QueueNameBuilderForPublisher(message);

            if (!_bindings.ContainsKey(queue))
                _bindings.Add(queue, new Binding(_settings, _logging, _queueManagement, queue));

            _logging.LogDebug($"Sending message to queue '{message}'");

            return _bindings[queue].SendAsync(message);
        }
    }
}