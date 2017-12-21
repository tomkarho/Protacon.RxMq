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
        private readonly ILogger<AzureBusPublisher> _logger;
        private readonly Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();

        private class Binding: IDisposable
        {
            private readonly QueueClient _queueClient;
            private readonly ILogger<AzureBusPublisher> _logger;

            internal Binding(
                MqSettings settings,
                ILogger<AzureBusPublisher> logging,
                AzureQueueManagement queueManagement,
                string queue,
                Type type,
                ILogger<AzureBusPublisher> logger)
            {
                queueManagement.CreateIfMissing(queue, type);

                _queueClient = new QueueClient(settings.ConnectionString, queue);

                logging.LogDebug($"Created new MQ binding '{queue}'.");
                _logger = logger;
            }

            public Task SendAsync(object message)
            {
                var asJson = JsonConvert.SerializeObject(
                        new {Data = message},
                        Formatting.None,
                        new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });

                _logger.LogDebug($"Sending message to queue '{message}'");

                var contentJsonBytes = Encoding.UTF8.GetBytes(asJson);

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
            _logger = logging;
        }

        public void Dispose()
        {
            _bindings.Select(x => x.Value)
                .ToList()
                .ForEach(x => x.Dispose());
        }
        public Task SendAsync<T>(T message) where T : new()
        {
            var queue = _settings.QueueNameBuilderForPublisher(message);

            if (!_bindings.ContainsKey(queue))
                _bindings.Add(queue, new Binding(_settings, _logger, _queueManagement, queue, typeof(T), _logger));


            return _bindings[queue].SendAsync(message);
        }
    }
}