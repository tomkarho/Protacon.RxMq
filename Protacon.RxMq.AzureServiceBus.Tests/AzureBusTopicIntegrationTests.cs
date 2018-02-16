using System;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
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
            var settings = TestSettings.TopicSettingsOptions();
            var subscriber = new AzureTopicSubscriber(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicSubscriber>>());
            var publisher = new AzureTopicPublisher(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicPublisher>>());

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = Guid.NewGuid()
            }).Wait();

            subscriber.Messages<TestMessageForTopic>()
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void WhenFiltersAreSet_Then()
        {
            var settings = TestSettings.TopicSettingsOptions();
            var publisher = new AzureTopicPublisher(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicPublisher>>());
            var subscriber = new AzureTopicSubscriber(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicSubscriber>>());

            var settingsNonMatching = TestSettings.TopicSettingsOptions(s => s.AzureSubscriptionFilters.Add("filter", new SqlFilter("")));
            var nonMatchingSubscriber = new AzureTopicSubscriber(settingsNonMatching, new AzureBusTopicManagement(settingsNonMatching), Substitute.For<ILogger<AzureTopicSubscriber>>());

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
