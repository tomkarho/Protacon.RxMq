using System;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus.Tests.Messages
{
    public class TestMessage: IRoutingKey
    {
        public void SetNewRoutingKeyForTesting(string routingKey)
        {
            RoutingKey = routingKey;
        }

        public string RoutingKey { get; private set; } = "testmessages";
        public Guid ExampleId { get; set; }
    }
}
