using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace Protacon.RxMq.AzureServiceBus
{
    public class AzureBusTopicSettings: AzureMqSettingsBase
    {
        public string TopicSubscriberId { get; set; } = Environment.MachineName;

        public Func<Type, string> TopicNameBuilderForSubscriber { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);

            if (instance is Abstractions.ITopic)
            {
                return ((Abstractions.ITopic)instance).TopicName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(Abstractions.ITopic)}'");
        };


        public Func<object, string> TopicNameBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is Abstractions.ITopic)
            {
                return ((Abstractions.ITopic)instance).TopicName;
            }

            throw new InvalidOperationException($"Default implementation of topic name builder expects used objects to extend '{nameof(Abstractions.ITopic)}'");
        };

        public Action<Microsoft.Azure.Management.ServiceBus.Fluent.Topic.Definition.IBlank, Type> AzureTopicBuilder { get; set; } = (create, messageType) =>
        {
            create
                .WithSizeInMB(1024)
                .WithDefaultMessageTTL(TimeSpan.FromSeconds(60*5))
                .Create();
        };

        public Action<Microsoft.Azure.Management.ServiceBus.Fluent.Subscription.Definition.IBlank, Type> AzureSubscriptionBuilder { get; set; } = (create, messageType) =>
        {
            create
                .WithDefaultMessageTTL(TimeSpan.FromSeconds(60*5))
                .Create();
        };

        public Dictionary<string, Filter> AzureSubscriptionFilters { get; set; } = new Dictionary<string, Filter>();
        public Func<object, Dictionary<string, object>> AzureMessagePropertyBuilder {get; set; } = message => new Dictionary<string, object>();
    }
}
