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
            services.AddTransient<AzureRxMqManagement>();
            services.AddSingleton<IMqQueSubscriber, AzureBusSubscriber>();
            services.AddSingleton<IMqQuePublisher, AzureBusPublisher>();
            return services;
        }
    }
}
