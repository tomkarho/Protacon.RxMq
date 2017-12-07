using System;
using Microsoft.Extensions.DependencyInjection;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddAzureBusMq(this IServiceCollection services, Action<MqSettings> configure)
        {
            services.Configure(configure);
            services.AddTransient<AzureQueueManagement>();
            services.AddSingleton<IMqSubscriber, AzureBusSubscriber>();
            services.AddSingleton<IMqPublisher, AzureBusPublisher>();
            return services;
        }
    }
}
