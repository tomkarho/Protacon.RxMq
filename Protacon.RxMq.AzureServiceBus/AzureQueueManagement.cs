using System;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureQueueManagement
    {
        private readonly MqSettings _settings;

        public AzureQueueManagement(IOptions<MqSettings> settings)
        {
            _settings = settings.Value;
        }

        public void CreateIfMissing(string queuName, Type messageType)
        {
            var azureCredentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    clientId: _settings.AzureSpAppId, 
                    clientSecret: _settings.AzureSpPassword, 
                    tenantId: _settings.AzureSpTenantId,
                    environment: AzureEnvironment.AzureGlobalCloud);
                
            var serviceBusManager = ServiceBusManager.Authenticate(azureCredentials, _settings.AzureSubscriptionId);

            var @namespace = serviceBusManager.Namespaces.GetByResourceGroup(_settings.AzureResourceGroup, _settings.AzureNamespace);

            if(@namespace == null)
                throw new InvalidOperationException($"Azure namespace '{_settings.AzureNamespace}' not found.");

            var queue = @namespace.Queues.List().FirstOrDefault(x => x.Name == queuName);
            if (queue == null)
            {
                _settings.QueueBuilder(@namespace.Queues.Define(queuName), messageType);
            }
        }
    }
}
