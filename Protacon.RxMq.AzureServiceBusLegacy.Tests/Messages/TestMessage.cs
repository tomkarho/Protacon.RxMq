using System;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages
{
    public class TestMessage: IQueueItem
    {
        public Guid ExampleId { get; set; }
        public string QueueName { get; } = "testmessages";
    }
}
