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
        public async void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var subscriber = new AzureBusSubscriber(TestSettings.MqSettings, _ => { }, _ => { });
            var publisher = new AzureBusPublisher(TestSettings.MqSettings, _ => { }, _ => { });

            var id = Guid.NewGuid();

            await publisher.SendAsync(new TestMessage
            {
                ExampleId = id
            });

            await subscriber.Messages<TestMessage>()
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync(x => x.Message.ExampleId == id);

            subscriber.Dispose();
        }

        [Fact]
        public async void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            var settings = TestSettings.MqSettings;

            var newQueueName = $"testquelegacy_{Guid.NewGuid()}";

            settings.QueueNameBuilderForPublisher = _ => newQueueName;
            settings.QueueNameBuilderForSubscriber = _ => newQueueName;

            var subscriber = new AzureBusSubscriber(settings, _ => { }, _ => { });
            var publisher = new AzureBusPublisher(settings, _ => { }, _ => { });

            var message = new TestMessage
            {
                ExampleId = Guid.NewGuid()
            };

            publisher.Invoking(x => x.SendAsync(message).Wait())
                .ShouldNotThrow<Exception>();

            await subscriber.Messages<TestMessage>()
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstOrDefaultAsync(x => x.Message.ExampleId == message.ExampleId);

            subscriber.Dispose();
        }
    }
}
