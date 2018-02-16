using System;
using System.Collections.Generic;
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
        public async void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var settings = TestSettings.TopicSettingsOptions();
            var subscriber = new AzureTopicSubscriber(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicSubscriber>>());
            var publisher = new AzureTopicPublisher(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicPublisher>>());

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id
            }).Wait();

            await subscriber.Messages<TestMessageForTopic>()
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5))
                .FirstAsync();
        }

        [Fact]
        public async void WhenFiltersAreSet_Then()
        {
            var correctTenantId = Guid.NewGuid();
            var invalidTenantId = Guid.NewGuid();

            var settings = TestSettings.TopicSettingsOptions(s => {
                s.AzureMessagePropertyBuilder = message => new Dictionary<string, object> { {"tenant", correctTenantId } };
                s.AzureSubscriptionFilters.Add("filter", new SqlFilter($"tenant = '{correctTenantId}'"));
            });

            var publisher = new AzureTopicPublisher(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicPublisher>>());
            var subscriber = new AzureTopicSubscriber(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicSubscriber>>());

            var invalidTenantSettings = TestSettings.TopicSettingsOptions(s => {
                s.AzureMessagePropertyBuilder = message => new Dictionary<string, object> { {"tenant", invalidTenantId } };
                s.AzureSubscriptionFilters.Add("filter", new SqlFilter($"tenant = '{correctTenantId}'"));
            });

            var invalidSubscriber = new AzureTopicSubscriber(invalidTenantSettings, new AzureBusTopicManagement(invalidTenantSettings), Substitute.For<ILogger<AzureTopicSubscriber>>());

            var id = Guid.NewGuid();

            publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = Guid.NewGuid()
            }).Wait();

            await subscriber.Messages<TestMessageForTopic>()
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5))
                .FirstAsync();

            await subscriber.Messages<TestMessageForTopic>()
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(5))
                .FirstAsync();
        }
    }
}
