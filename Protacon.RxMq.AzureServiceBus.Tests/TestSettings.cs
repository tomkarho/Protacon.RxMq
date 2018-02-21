using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Protacon.RxMq.AzureServiceBus.Queue;
using Protacon.RxMq.AzureServiceBus.Topic;

namespace Protacon.RxMq.AzureServiceBus.Tests
{
    public static class TestSettings
    {
        public static IOptions<AzureBusQueueSettings> QueueSettingsOptions(Action<AzureBusQueueSettings> updates = null)
        {
            var settings = MqSettings();

            if(updates != null)
                updates.Invoke(settings);

            return Options.Create(settings);
        }

        public static IOptions<AzureBusTopicSettings> TopicSettingsOptions(Action<AzureBusTopicSettings> updates = null)
        {
            var settings = MqTopicSettings();

            if(updates != null)
                updates.Invoke(settings);

            return Options.Create(settings);
        }

        public static AzureBusQueueSettings MqSettings()
        {
            var config = new AzureBusQueueSettings();
            LoadCommonSettings(config);
            return config;
        }

        public static AzureBusTopicSettings MqTopicSettings()
        {
            var config = new AzureBusTopicSettings
            {
                // This makes running machine looks like unique on every test.
                TopicSubscriberId = Guid.NewGuid().ToString()
            };
            LoadCommonSettings(config);
            return config;
        }

        private static void LoadCommonSettings(AzureMqSettingsBase baseConfig)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("client-secrets.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

                baseConfig.ConnectionString = config.GetConfig("ConnectionString");
                baseConfig.AzureSpAppId = config.GetConfig("AzureSpAppId");
                baseConfig.AzureSpPassword = config.GetConfig("AzureSpPassword");
                baseConfig.AzureSpTenantId = config.GetConfig("AzureSpTenantId");
                baseConfig.AzureNamespace = config.GetConfig("AzureNamespace");
                baseConfig.AzureResourceGroup = config.GetConfig("AzureResourceGroup");
                baseConfig.AzureSubscriptionId = config.GetConfig("AzureSubscriptionId");
        }

        private static string GetConfig(this IConfigurationRoot root, string name)
        {
            return root[name] ?? throw new InvalidOperationException(name);
        }
    }
}
