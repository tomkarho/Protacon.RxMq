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
            var subscriber = new AzureBusSubscriber(TestSettings.MqSettings, _ => { }, _ => { });
            var publisher = new AzureBusPublisher(TestSettings.MqSettings, _ => { }, _ => { });

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessage
            {
                ExampleId = id
            }).Wait();

            subscriber.Messages<TestMessage>()
                .Where(x => x.Message.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(10))
                .FirstAsync().Wait();

            subscriber.Dispose();
        }

        [Fact]
        public void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            var subscriber = new AzureBusSubscriber(TestSettings.MqSettings, _ => { }, _ => { });
            var publisher = new AzureBusPublisher(TestSettings.MqSettings, _ => { }, _ => { });

            OverridableQueueForTestingMessage.RoutingKeyOverride = $"queuegeneratortest_{Guid.NewGuid()}";
            var message = new OverridableQueueForTestingMessage();

            publisher.Invoking(x => x.SendAsync(message).Wait())
                .ShouldNotThrow<Exception>();

            subscriber.Dispose();
        }
    }
}
