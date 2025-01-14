using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;

namespace Protacon.RxMq.AzureServiceBus.Topic
{
    public class AzureBusTopicSettings : AzureMqSettingsBase
    {
        public string TopicSubscriberId { get; set; } = Environment.MachineName;

        public Func<Type, string> TopicNameBuilder { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);

            if (instance is ITopicItem t)
            {
                return t.TopicName;
            }

            throw new InvalidOperationException($"Default implementation of queue name builder expects used objects to extend '{nameof(ITopicItem)}'");
        };

        public Action<Microsoft.Azure.Management.ServiceBus.Fluent.Topic.Definition.IBlank, Type> AzureTopicBuilder { get; set; } = (create, messageType) =>
        {
            create
                .WithSizeInMB(1024)
                .WithDefaultMessageTTL(TimeSpan.FromSeconds(60 * 5))
                .Create();
        };

        public Action<Microsoft.Azure.Management.ServiceBus.Fluent.Subscription.Definition.IBlank, Type> AzureSubscriptionBuilder { get; set; } = (create, messageType) =>
        {
            create
                .WithDefaultMessageTTL(TimeSpan.FromSeconds(60 * 5))
                .Create();
        };

        public Dictionary<string, Filter> AzureSubscriptionRules { get; set; } = new Dictionary<string, Filter> { { "getEverything", new TrueFilter() } };
        public Func<object, Dictionary<string, object>> AzureMessagePropertyBuilder { get; set; } = message => new Dictionary<string, object>();
    }
}
