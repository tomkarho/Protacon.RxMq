using System;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protacon.RxMq.AzureServiceBus.Tests.Messages;
using Xunit;

namespace Protacon.RxMq.AzureServiceBus.Tests
{
    public class AzureBusIntegrationTests
    {
        [Fact]
        public void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var subscriber = new AzureBusSubscriber(TestSettings.MqSettingsOptions(), Substitute.For<ILogger<AzureBusSubscriber>>());
            var publisher = new AzureBusPublisher(TestSettings.MqSettingsOptions(), new AzureQueueManagement(TestSettings.MqSettingsOptions()), Substitute.For<ILogger<AzureBusPublisher>>());

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessage
            {
                ExampleId = Guid.NewGuid()
            }).Wait();

            subscriber.Messages<TestMessage>()
                .Where(x => x.Message.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            // Arrange.
            var testQueueName = $"testque_{Guid.NewGuid()}";

            var settings = TestSettings.MqSettingsOptions();

            settings.Value.RouteBuilderForPublisher = _ => testQueueName;
            settings.Value.RouteBuilderForSubscriber = _ => testQueueName;

            var publisher = new AzureBusPublisher(settings, new AzureQueueManagement(settings), Substitute.For<ILogger<AzureBusPublisher>>());
            var receiver = new AzureBusSubscriber(settings, Substitute.For<ILogger<AzureBusSubscriber>>());

            var message = new TestMessage
            {
                ExampleId = Guid.NewGuid(),
                Something = "abc"
            };

            // Act.
            publisher
                .Invoking(x => x.SendAsync(message).Wait())
                .Should().NotThrow<Exception>();

            // Assert.
            var result = await receiver.Messages<TestMessage>()
                .Timeout(TimeSpan.FromSeconds(20))
                .FirstAsync();

            result.Message.ExampleId.Should().Be(message.ExampleId);
        }
    }
}
