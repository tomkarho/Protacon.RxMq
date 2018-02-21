using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent;

namespace Protacon.RxMq.AzureServiceBus
{
    public static class NamespaceUtils
    {
        public static IServiceBusNamespace GetNamespace(AzureMqSettingsBase settings)
        {
            var azureCredentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    clientId: settings.AzureSpAppId,
                    clientSecret: settings.AzureSpPassword,
                    tenantId: settings.AzureSpTenantId,
                    environment: AzureEnvironment.AzureGlobalCloud);

            var serviceBusManager = ServiceBusManager.Authenticate(azureCredentials, settings.AzureSubscriptionId);

            var @namespace = serviceBusManager.Namespaces.GetByResourceGroup(settings.AzureResourceGroup, settings.AzureNamespace);

            if (@namespace == null)
                throw new InvalidOperationException($"Azure namespace '{settings.AzureNamespace}' not found.");

            return @namespace;
        }
    }
}