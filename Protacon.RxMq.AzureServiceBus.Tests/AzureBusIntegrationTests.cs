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
            var subscriber = new AzureBusSubscriber(TestSettings.MqSettings, Substitute.For<ILogger<AzureBusSubscriber>>());
            var publisher = new AzureBusPublisher(TestSettings.MqSettings, Substitute.For<ILogger<AzureBusPublisher>>());

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessage
            {
                ExampleId = Guid.NewGuid()
            }).Wait();

            subscriber.Messages<TestMessage>()
                .Where(x => x.Message.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5));
        }

        [Fact(Skip = "TODO: This is actually kind of hard requirement to fullfill with current state of library. https://github.com/Azure/azure-service-bus-dotnet/issues/65")]
        public void WhenQueueDoesntExistYet_ThenCreateNew()
        {
            var publisher = new AzureBusPublisher(TestSettings.MqSettings, Substitute.For<ILogger<AzureBusPublisher>>());

            OverridableQueueForTestingMessage.RoutingKeyOverride = $"queuegeneratortest_{Guid.NewGuid()}";
            var message = new OverridableQueueForTestingMessage();

            publisher.Invoking(x => x.SendAsync(message).Wait()).Should().NotThrow<Exception>();
        }
    }
}
