using Microsoft.Azure.Management.ResourceManager.Fluent.Core.CollectionActions;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Queue.Definition;

namespace Protacon.RxMq.AzureServiceBus
{
    public abstract class AzureMqSettingsBase
    {
        public string ConnectionString { get; set; }
        public string AzureSpAppId { get; set; }
        public string AzureSpPassword { get; set; }
        public string AzureSpTenantId { get; set; }
        public string AzureResourceGroup { get; set; }
        public string AzureNamespace { get; set; }
        public string AzureSubscriptionId { get; set; }
        public string AzureRetryMinimumBackoff { get; set; }
        public string AzureRetryMaximumBackoff { get; set; }
        public string AzureMaximumRetryCount { get; set; }
    }
}
