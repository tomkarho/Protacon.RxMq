using System;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus.Tests.Messages
{
    public class TestMessage: IQueueItem
    {
        public Guid ExampleId { get; set; }
        public string Something { get; set; }
        public string TenantId { get; set; }
        public string QueueName { get; } = "testmessages";
    }
}
