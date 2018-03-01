using System;
using Microsoft.Extensions.DependencyInjection;
using Protacon.RxMq.Abstractions;
using Protacon.RxMq.AzureServiceBus.Queue;
using Protacon.RxMq.AzureServiceBus.Topic;

namespace Protacon.RxMq.AzureServiceBus
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddAzureBusQueues(this IServiceCollection services, Action<AzureBusTopicSettings> configure)
        {
            services.Configure(configure);
            services.AddTransient<AzureBusQueueManagement>();
            services.AddSingleton<IMqQueueSubscriber, AzureQueueSubscriber>();
            services.AddSingleton<IMqQueuePublisher, AzureQueuePublisher>();
            return services;
        }

        public static IServiceCollection AddAzureBusTopics(this IServiceCollection services, Action<AzureBusTopicSettings> configure)
        {
            services.Configure(configure);
            services.AddTransient<AzureBusTopicManagement>();
            services.AddSingleton<IMqTopicSubscriber, AzureTopicSubscriber>();
            services.AddSingleton<IMqTopicPublisher, AzureTopicPublisher>();
            return services;
        }
    }
}