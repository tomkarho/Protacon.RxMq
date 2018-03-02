using System;
using System.Linq;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus.Topic
{
    public class AzureBusTopicManagement
    {
        private readonly AzureBusTopicSettings _settings;

        public AzureBusTopicManagement(IOptions<AzureBusTopicSettings> settings)
        {
            _settings = settings.Value;
        }

        public void CreateSubscriptionIfMissing(string topicName, string subscriptionName, Type messageType)
        {
            var @namespace = NamespaceUtils.GetNamespace(_settings);

            CreateTopicIfMissing(topicName, messageType, @namespace);

            var topic = @namespace.Topics.List().FirstOrDefault(x => x.Name == topicName);

            if(topic == null)
            {
                throw new InvalidOperationException($"Cannot find topic '{topicName}' from topics '{string.Join(", ", @namespace.Topics.List().Select(x => x.Name))}'");
            }

            if (!topic.Subscriptions.List().Any(x => x.Name == subscriptionName))
            {
                _settings.AzureSubscriptionBuilder(topic.Subscriptions.Define(subscriptionName), messageType);
            }
        }

        public void CreateTopicIfMissing(string topicName, Type messageType)
        {
            var @namespace = NamespaceUtils.GetNamespace(_settings);
            CreateTopicIfMissing(topicName, messageType, @namespace);
        }

        private void CreateTopicIfMissing(string topicName, Type messageType, IServiceBusNamespace @namespace)
        {
            var topic = @namespace.Topics.List().FirstOrDefault(x => x.Name == topicName);
            if (topic == null)
            {
                _settings.AzureTopicBuilder(@namespace.Topics.Define(topicName), messageType);
            }
        }


    }
}
