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
        public int AzureRetryMinimumBackoff { get; set; }
        public int AzureRetryMaximumBackoff { get; set; }
        public int AzureMaximumRetryCount { get; set; }
    }
}
