using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus.Tests.Messages
{
    public class OverridableQueueForTestingMessage: IRoutingKey
    {
        public static string RoutingKeyOverride { get; set; }

        public OverridableQueueForTestingMessage()
        {
            RoutingKey = RoutingKeyOverride ?? nameof(OverridableQueueForTestingMessage).ToLower();
        }

        public string RoutingKey { get; }
    }
}
