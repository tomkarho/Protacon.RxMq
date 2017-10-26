using System;
using System.Reactive.Linq;
using FluentAssertions;
using Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages;
using Xunit;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public class AzureBusIntegrationTests
    {
        [Fact]
        public void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var bus = new AzureBusMq(TestSettings.MqSettings, _ => { }, _ => { });

            var id = Guid.NewGuid();

            bus.SendAsync(new TestMessage
            {
                ExampleId = id
            }).Wait();

            bus.Messages<TestMessage>()
                .Where(x => x.Message.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync().Wait();
        }

        [Fact]
        public void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            var bus = new AzureBusMq(TestSettings.MqSettings, _ => {}, _ => {});

            OverridableQueueForTestingMessage.RoutingKeyOverride = $"queuegeneratortest_{Guid.NewGuid()}";
            var message = new OverridableQueueForTestingMessage();

            bus.Invoking(x => x.SendAsync(message).Wait())
                .ShouldNotThrow<Exception>();
        }
    }
}
