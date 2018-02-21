using System;
using System.Reactive.Linq;
using FluentAssertions;
using Protacon.RxMq.AzureServiceBusLegacy.Queue;
using Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages;
using Xunit;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public class AzureBusQueueIntegrationTests
    {
        [Fact]
        public async void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var subscriber = new AzureBusQueueSubscriber(TestSettings.MqSettingsForQueue(), _ => { }, _ => { });
            var publisher = new AzureBusQueuePublisher(TestSettings.MqSettingsForQueue(), _ => { }, _ => { });

            var id = Guid.NewGuid();

            await publisher.SendAsync(new TestMessageForQueue
            {
                ExampleId = id
            });

            await subscriber.Messages<TestMessageForQueue>()
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync(x => x.ExampleId == id);

            subscriber.Dispose();
        }

        [Fact]
        public async void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            var settings = TestSettings.MqSettingsForQueue();

            var newQueueName = $"testquelegacy_{Guid.NewGuid()}";

            settings.QueueNameBuilderForPublisher = _ => newQueueName;
            settings.QueueNameBuilderForSubscriber = _ => newQueueName;

            var subscriber = new AzureBusQueueSubscriber(settings, _ => { }, _ => { });
            var publisher = new AzureBusQueuePublisher(settings, _ => { }, _ => { });

            var message = new TestMessageForQueue
            {
                ExampleId = Guid.NewGuid()
            };

            publisher.Invoking(x => x.SendAsync(message).Wait())
                .ShouldNotThrow<Exception>();

            await subscriber.Messages<TestMessageForQueue>()
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstOrDefaultAsync(x => x.ExampleId == message.ExampleId);

            subscriber.Dispose();
        }
    }
}
