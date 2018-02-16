using System;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureRxMqManagement
    {
        private readonly MqSettings _settings;

        public AzureRxMqManagement(IOptions<MqSettings> settings)
        {
            _settings = settings.Value;
        }

        public void CreateQueIfMissing(string queuName, Type messageType)
        {
            var @namespace = GetNamespace();

            var queue = @namespace.Queues.List().FirstOrDefault(x => x.Name == queuName);
            if (queue == null)
            {
                _settings.AzureQueueBuilder(@namespace.Queues.Define(queuName), messageType);
            }
        }

        public void CreateSubscriptionIfMissing(string subscriptionName, Type messageType)
        {
            var @namespace = GetNamespace();

            var queue = @namespace.Topics.List().FirstOrDefault(x => x.Subscriptions.List().Any(s => s.Name == subscriptionName));
            if (queue == null)
            {
                _settings.AzureQueueBuilder(@namespace.Queues.Define(subscriptionName), messageType);
            }
        }

        public void CreateTopicIfMissing(string topicName, Type messageType)
        {
            var @namespace = GetNamespace();

            var topic = @namespace.Topics.List().FirstOrDefault(x => x.Name == topicName);
            if (topic == null)
            {
                _settings.AzureTopicBuilder(@namespace.Topics.Define(topicName), messageType);
            }
        }

        private IServiceBusNamespace GetNamespace()
        {
            var azureCredentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    clientId: _settings.AzureSpAppId,
                    clientSecret: _settings.AzureSpPassword,
                    tenantId: _settings.AzureSpTenantId,
                    environment: AzureEnvironment.AzureGlobalCloud);

            var serviceBusManager = ServiceBusManager.Authenticate(azureCredentials, _settings.AzureSubscriptionId);

            var @namespace = serviceBusManager.Namespaces.GetByResourceGroup(_settings.AzureResourceGroup, _settings.AzureNamespace);

            if (@namespace == null)
                throw new InvalidOperationException($"Azure namespace '{_settings.AzureNamespace}' not found.");

            return @namespace;
        }
    }
}
