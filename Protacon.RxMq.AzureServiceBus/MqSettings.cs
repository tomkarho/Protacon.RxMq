using System;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus
{
    public class MqSettings
    {
        public string ConnectionString { get; set; }

        public string AzureSpAppId { get; set; }
        public string AzureSpPassword { get; set; }
        public string AzureSpTenantId { get; set; }

        public Func<Type, string> RouteBuilderForSubscriber { get; set; } = type =>
        {
            var instance = Activator.CreateInstance(type);
            
            if(instance is IRoutingKey)
            {
                return ((IRoutingKey)instance).RoutingKey;
            }

            throw new InvalidOperationException($"Default implementation of route builder expects used objects to extend '{nameof(IRoutingKey)}'");
        };

        public Func<object, string> RouteBuilderForPublisher { get; set; } = instance =>
        {
            if (instance is IRoutingKey)
            {
                return ((IRoutingKey)instance).RoutingKey;
            }

            throw new InvalidOperationException($"Default implementation of route builder expects used objects to extend '{nameof(IRoutingKey)}'");
        };
    }
}
