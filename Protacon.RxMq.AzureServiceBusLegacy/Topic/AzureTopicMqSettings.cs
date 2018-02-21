using System;
using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;

namespace Protacon.RxMq.AzureServiceBusLegacy.Topic
{
    public class AzureTopicMqSettings : MqSettingsBase
    {
        public string TopicSubscriberId { get; set; } = Environment.MachineName;

        public Func<Type, string> TopicNameBuilderForSubscriber { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);

            if (instance is ITopic)
            {
                return ((ITopic) instance).TopicName;
            }

            throw new InvalidOperationException(
                $"Default implementation of queue name builder expects used objects to extend '{nameof(ITopic)}'");
        };

        public Func<object, string> TopicNameBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is ITopic)
            {
                return ((ITopic)instance).TopicName;
            }

            throw new InvalidOperationException($"Default implementation of topic name builder expects used objects to extend '{nameof(ITopic)}'");
        };

        public Dictionary<string, Filter> AzureSubscriptionRules { get; set; } = new Dictionary<string, Filter> { { "getEverything", new TrueFilter() } };
        public Func<object, Dictionary<string, object>> AzureMessagePropertyBuilder { get; set; } = message => new Dictionary<string, object>();

        public TopicDescription TopicBuilderConfig(TopicDescription topicDescription, Type type)
        {
            topicDescription.DefaultMessageTimeToLive = TimeSpan.FromSeconds(60);
            return topicDescription;
        }

        public SubscriptionDescription SubscriptionBuilderConfig(SubscriptionDescription subscriptionDescription, Type type)
        {
            subscriptionDescription.DefaultMessageTimeToLive = TimeSpan.FromSeconds(60);
            return subscriptionDescription;
        }
    }
}