using System;
using Microsoft.Extensions.DependencyInjection;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddAzureBusQueues(this IServiceCollection services, Action<AzureBusTopicSettings> configure)
        {
            services.Configure(configure);
            services.AddTransient<AzureBusQueueManagement>();
            services.AddSingleton<IMqQueSubscriber, AzureQueueSubscriber>();
            services.AddSingleton<IMqQuePublisher, AzureQueuePublisher>();
            return services;
        }

        public static IServiceCollection AddAzureBusTopics(this IServiceCollection services, Action<AzureBusTopicSettings> configure)
        {
            services.Configure(configure);
            services.AddTransient<AzureBusQueueManagement>();
            services.AddSingleton<IMqTopicSubscriber, IMqTopicSubscriber>();
            services.AddSingleton<IMqTopicPublisher, AzureTopicPublisher>();
            return services;
        }
    }
}
