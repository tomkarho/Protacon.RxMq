using System;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages
{
    public class TestMessageForQueue: IQueueItem
    {
        public Guid ExampleId { get; set; }
        public string QueueName => "testmessages";
    }
}
