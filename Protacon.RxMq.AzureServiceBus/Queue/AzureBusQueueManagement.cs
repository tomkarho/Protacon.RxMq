using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus.Queue
{
    public class AzureBusQueueManagement
    {
        private readonly AzureBusQueueSettings _settings;

        public AzureBusQueueManagement(IOptions<AzureBusQueueSettings> settings)
        {
            _settings = settings.Value;
        }

        public void CreateQueIfMissing(string queuName, Type messageType)
        {
            var @namespace = NamespaceUtils.GetNamespace(_settings);

            var queue = @namespace.Queues.List().FirstOrDefault(x => x.Name == queuName);
            if (queue == null)
            {
                _settings.AzureQueueBuilder(@namespace.Queues.Define(queuName), messageType);
            }
        }
    }
}
