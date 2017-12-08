using System;
using Microsoft.ServiceBus.Messaging;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy
{
    public class MqSettings
    {
        public string ConnectionString { get; set; }

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

        public Func<QueueDescription, Type, QueueDescription> QueueBuilderConfig { get; set; } = (queueDescription, messageType) =>
        {
            queueDescription.MaxDeliveryCount = 10;
            queueDescription.MaxSizeInMegabytes = 1024;
            queueDescription.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
            return queueDescription;
        };
    }
}
