using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Protacon.RxMq.AzureServiceBusLegacy.Queue;
using Protacon.RxMq.AzureServiceBusLegacy.Topic;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public static class TestSettings
    {
        public static AzureQueueMqSettings MqSettingsForQueue()
        {
            var  secretFile = Path.Combine(Environment.CurrentDirectory, "client-secrets.json");
            JObject secretFileContent = new JObject();

            if(File.Exists(secretFile))
            {
                secretFileContent = JObject.Parse(File.ReadAllText(secretFile));
            }

            var queueName = Guid.NewGuid().ToString();
            return new AzureQueueMqSettings
            {
                ConnectionString = secretFileContent["ConnectionString"]?.ToString()
                    ?? Environment.GetEnvironmentVariable("ConnectionString") ??
                    throw new InvalidOperationException("Missing secrets."),
                QueueNameBuilderForPublisher = (_) => queueName,
                QueueNameBuilderForSubscriber= (_) => queueName
            };
        }

        public static AzureTopicMqSettings MqSettingsForTopic()
        {
            var secretFile = Path.Combine(Environment.CurrentDirectory, "client-secrets.json");
            JObject secretFileContent = new JObject();

            if (File.Exists(secretFile))
            {
                secretFileContent = JObject.Parse(File.ReadAllText(secretFile));
            }
            var topicName = "testtopic_" + Guid.NewGuid();
            return new AzureTopicMqSettings
            {
                ConnectionString = secretFileContent["ConnectionString"]?.ToString()
                                   ?? Environment.GetEnvironmentVariable("ConnectionString") ??
                                   throw new InvalidOperationException("Missing secrets."),
                TopicSubscriberId = Guid.NewGuid().ToString().Substring(0, 12),
                TopicNameBuilder = _ => topicName.Substring(0, 12)
            };
        }
    }
}
