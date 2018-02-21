using System;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages
{
    public class TestMessageForTopic : ITopic
    {
        public Guid ExampleId { get; set; }
        public Guid TenantId { get; set; }
        public string TopicName => "testmessages_topic";
    }
}