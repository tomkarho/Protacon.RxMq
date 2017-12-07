using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Protacon.RxMq.AzureServiceBus.Tests
{
    public static class TestSettings
    {
        public static IOptions<MqSettings> MqSettingsOptions() => Options.Create(MqSettings());

        public static MqSettings MqSettings()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("client-secrets.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            return new MqSettings
            {
                ConnectionString = config.GetConfig("ConnectionString"),
                AzureSpAppId = config.GetConfig("AzureSpAppId"),
                AzureSpPassword = config.GetConfig("AzureSpPassword"),
                AzureSpTenantId = config.GetConfig("AzureSpTenantId"),
                AzureNamespace = config.GetConfig("AzureNamespace"),
                AzureResourceGroup = config.GetConfig("AzureResourceGroup"),
                AzureSubscriptionId = config.GetConfig("AzureSubscriptionId")
            };
        }

        private static string GetConfig(this IConfigurationRoot root, string name)
        {
            return root[name] ?? throw new InvalidOperationException(name);
        }
    }
}
