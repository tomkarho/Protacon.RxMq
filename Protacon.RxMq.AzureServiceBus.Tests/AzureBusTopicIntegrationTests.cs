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
            var subscriber = new AzureTopicSubscriber(TestSettings.MqSettingsOptions(), new AzureRxMqManagement(TestSettings.MqSettingsOptions()), Substitute.For<ILogger<AzureBusSubscriber>>());
            var publisher = new AzureTopicPublisher(TestSettings.MqSettingsOptions(), new AzureRxMqManagement(TestSettings.MqSettingsOptions()), Substitute.For<ILogger<AzureTopicPublisher>>());

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
