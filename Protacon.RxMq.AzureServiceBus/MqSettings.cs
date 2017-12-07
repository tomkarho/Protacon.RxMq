using System;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.CollectionActions;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.ServiceBus.Fluent.Queue.Definition;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus
{
    public class MqSettings
    {
        public string ConnectionString { get; set; }
        public string AzureSpAppId { get; set; }
        public string AzureSpPassword { get; set; }
        public string AzureSpTenantId { get; set; }
        public string AzureResourceGroup { get; set; }
        public string AzureNamespace { get; set; }
        public string AzureSubscriptionId { get; set; }

        public Func<Type, string> QueueNameBuilderForSubscriber { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);
            
            if(instance is IRoutingKey)
            {
                return ((IRoutingKey)instance).RoutingKey;
            }

            throw new InvalidOperationException($"Default implementation of route builder expects used objects to extend '{nameof(IRoutingKey)}'");
        };

        public Func<object, string> QueueNameBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is IRoutingKey)
            {
                return ((IRoutingKey)instance).RoutingKey;
            }

            throw new InvalidOperationException($"Default implementation of route builder expects used objects to extend '{nameof(IRoutingKey)}'");
        };

        public Action<IBlank> QueueBuilder { get; set; } = queu =>
        {
            queu
                .WithSizeInMB(1024)
                .WithMessageMovedToDeadLetterQueueOnMaxDeliveryCount(10)
                .WithMessageLockDurationInSeconds(30)
                .Create();
        };
    }
}
