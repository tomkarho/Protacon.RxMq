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

            if (instance is IQueueItem)
            {
                return ((IQueueItem)instance).QueueName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(IQueueItem)}'");
        };

        public Func<object, string> QueueNameBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is IQueueItem)
            {
                return ((IQueueItem)instance).QueueName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(IQueueItem)}' or ");
        };

        public Action<IBlank, Type> QueueBuilder { get; set; } = (queu, messageType) =>
        {
            queu
                .WithSizeInMB(1024)
                .WithMessageMovedToDeadLetterQueueOnMaxDeliveryCount(10)
                .WithMessageLockDurationInSeconds(30)
                .Create();
        };
    }
}
