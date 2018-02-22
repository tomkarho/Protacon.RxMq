using System;
using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;

namespace Protacon.RxMq.AzureServiceBusLegacy.Topic
{
    public class AzureTopicMqSettings : MqSettingsBase
    {
        public string TopicSubscriberId { get; set; } = Environment.MachineName;

        public Func<Type, string> TopicNameBuilder { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);

            if (instance is ITopicItem)
            {
                return ((ITopicItem) instance).TopicName;
            }

            throw new InvalidOperationException(
                $"Default implementation of queue name builder expects used objects to extend '{nameof(ITopicItem)}'");
        };

        public Dictionary<string, Filter> AzureSubscriptionRules { get; set; } = new Dictionary<string, Filter> { { "getEverything", new TrueFilter() } };
        public Func<object, Dictionary<string, object>> AzureMessagePropertyBuilder { get; set; } = message => new Dictionary<string, object>();

        public Func<TopicDescription, Type, TopicDescription> TopicBuilderConfig { get; set; } =
            (topicDescription, type) =>
            {
                topicDescription.DefaultMessageTimeToLive = TimeSpan.FromSeconds(60);
                return topicDescription;
            };

        public Func<SubscriptionDescription, Type, SubscriptionDescription> SubscriptionBuilderConfig { get; set; } =
            (subscriptionDescription, type) =>
            {
                subscriptionDescription.DefaultMessageTimeToLive = TimeSpan.FromSeconds(60);
                return subscriptionDescription;
            };
    }
}