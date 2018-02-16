using System;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Protacon.RxMq.AzureServiceBus.Tests.Messages;
using Xunit;

namespace Protacon.RxMq.AzureServiceBus.Tests
{
    public class AzureBusTopicIntegrationTests
    {
        [Fact]
        public void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var settings = TestSettings.MqSettingsOptions();
            var subscriber = new AzureTopicSubscriber(settings, new AzureRxMqManagement(settings), Substitute.For<ILogger<AzureBusSubscriber>>());
            var publisher = new AzureTopicPublisher(settings, new AzureRxMqManagement(settings), Substitute.For<ILogger<AzureTopicPublisher>>());

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = Guid.NewGuid()
            }).Wait();

            subscriber.Messages<TestMessageForTopic>()
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5));
        }
    }
}
