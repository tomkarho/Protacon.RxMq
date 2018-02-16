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
        public string AppDeploymentId { get; set; } = Environment.MachineName;

        public Func<Type, string> QueueNameBuilderForSubscriber { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);

            if (instance is IQueueItem)
            {
                return ((IQueueItem)instance).QueueName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(IQueueItem)}'");
        };

        public Func<Type, string> TopicNameBuilderForSubscriber { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);

            if (instance is Abstractions.ITopic)
            {
                return ((Abstractions.ITopic)instance).TopicName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(Abstractions.ITopic)}'");
        };

        public Func<object, string> QueueNameBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is IQueueItem)
            {
                return ((IQueueItem)instance).QueueName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(IQueueItem)}'");
        };

        public Func<object, string> TopicNameBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is Abstractions.ITopic)
            {
                return ((Abstractions.ITopic)instance).TopicName;
            }

            throw new InvalidOperationException($"Default implementation of topic name builder expects used objects to extend '{nameof(Abstractions.ITopic)}'");
        };

        public Action<Microsoft.Azure.Management.ServiceBus.Fluent.Queue.Definition.IBlank, Type> AzureQueueBuilder { get; set; } = (queu, messageType) =>
        {
            queu
                .WithSizeInMB(1024)
                .WithDefaultMessageTTL(TimeSpan.FromSeconds(60*5))
                .WithMessageMovedToDeadLetterQueueOnMaxDeliveryCount(10)
                .WithMessageLockDurationInSeconds(30)
                .Create();
        };

        public Action<Microsoft.Azure.Management.ServiceBus.Fluent.Topic.Definition.IBlank, Type> AzureTopicBuilder { get; set; } = (create, messageType) =>
        {
            create
                .WithSizeInMB(1024)
                .WithDefaultMessageTTL(TimeSpan.FromSeconds(60*5))
                .Create();
        };
    }
}
