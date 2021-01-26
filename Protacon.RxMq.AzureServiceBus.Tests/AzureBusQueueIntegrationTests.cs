using System;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protacon.RxMq.AzureServiceBus.Queue;
using Protacon.RxMq.AzureServiceBus.Tests.Messages;
using Xunit;

namespace Protacon.RxMq.AzureServiceBus.Tests
{
    public class AzureBusQueueIntegrationTests
    {
        [Fact]
        public async void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var subscriber = new AzureQueueSubscriber(TestSettings.QueueSettingsOptions(), new AzureBusQueueManagement(TestSettings.QueueSettingsOptions()), Substitute.For<ILogger<AzureQueueSubscriber>>());
            var publisher = new AzureQueuePublisher(TestSettings.QueueSettingsOptions(), new AzureBusQueueManagement(TestSettings.QueueSettingsOptions()), Substitute.For<ILogger<AzureQueuePublisher>>());

            var id = Guid.NewGuid();

            await publisher.SendAsync(new TestMessage
            {
                ExampleId = id
            });

            await subscriber.Messages<TestMessage>()
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync();
        }

        [Fact]
        public async void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            // Arrange.
            var testQueueName = $"testque_{Guid.NewGuid()}";

            var settings = TestSettings.QueueSettingsOptions();

            settings.Value.QueueNameBuilderForPublisher = _ => testQueueName;
            settings.Value.QueueNameBuilderForSubscriber = _ => testQueueName;

            var publisher = new AzureQueuePublisher(settings, new AzureBusQueueManagement(settings), Substitute.For<ILogger<AzureQueuePublisher>>());
            var receiver = new AzureQueueSubscriber(settings, new AzureBusQueueManagement(settings), Substitute.For<ILogger<AzureQueueSubscriber>>());

            var message = new TestMessage
            {
                ExampleId = Guid.NewGuid(),
                Something = "abc"
            };

            // Act.
            publisher
                .Invoking(x => x.SendAsync(message).Wait(TimeSpan.FromSeconds(10)))
                .Should().NotThrow<Exception>();

            // Assert.
            var result = await receiver.Messages<TestMessage>()
                .Timeout(TimeSpan.FromSeconds(20))
                .FirstAsync();

            result.ExampleId.Should().Be(message.ExampleId);
        }

        [Fact]
        public async void WhenDynamicQueuesAreUsed_ThenDeliverMessagesCorrectly()
        {
            var tenant2 = Guid.NewGuid();

            // Arrange.
            var settings = TestSettings.QueueSettingsOptions();

            settings.Value.QueueNameBuilderForPublisher = x =>
            {
                if (x is TestMessage m)
                {
                    return m.QueueName + "_" + m.TenantId;
                }
                throw new InvalidOperationException();
            };

            settings.Value.QueueNameBuilderForSubscriber = type =>
            {
                var instance = Activator.CreateInstance(type);

                if (instance is TestMessage m)
                {
                    return m.QueueName + "_" + tenant2;
                }
                throw new InvalidOperationException();
            };

            var publisher = new AzureQueuePublisher(settings, new AzureBusQueueManagement(settings), Substitute.For<ILogger<AzureQueuePublisher>>());
            var receiver = new AzureQueueSubscriber(settings, new AzureBusQueueManagement(settings), Substitute.For<ILogger<AzureQueueSubscriber>>());

            var message = new TestMessage
            {
                ExampleId = Guid.NewGuid(),
                Something = "abc",
                TenantId = tenant2.ToString()
            };

            // Act.
            publisher
                .Invoking(x => x.SendAsync(message).Wait(TimeSpan.FromSeconds(10)))
                .Should().NotThrow<Exception>();

            // Assert.
            var result = await receiver.Messages<TestMessage>()
                .Timeout(TimeSpan.FromSeconds(20))
                .FirstAsync();

            result.ExampleId.Should().Be(message.ExampleId);
        }
    }
}
