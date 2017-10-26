using System;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages
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
