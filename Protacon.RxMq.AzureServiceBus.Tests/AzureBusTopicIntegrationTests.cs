using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
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
            var listener = subscriber.Messages<TestMessageForTopic>();

            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id
            });

            await listener
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(10))
                .FirstAsync();
        }

        [Fact]
        public async void WhenFiltersAreSet_ThenDontReturnInvalidTenantMessages()
        {
            // Arrrange.
            var correctTenantId = Guid.NewGuid();
            var invalidTenantId = Guid.NewGuid();

            var settings = TestSettings.TopicSettingsOptions(s => {
                s.AzureMessagePropertyBuilder = message => new Dictionary<string, object> { {"tenant", ((TestMessageForTopic)message).TenantId } };
                s.AzureSubscriptionFilters.Add("filter", new SqlFilter($"user.tenant='{correctTenantId}'"));
            });

            var publisher = new AzureTopicPublisher(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicPublisher>>());
            var subscriber = new AzureTopicSubscriber(settings, new AzureBusTopicManagement(settings), Substitute.For<ILogger<AzureTopicSubscriber>>());

            var id = Guid.NewGuid();
            var invalidTenantMessageId = Guid.NewGuid();
            var listener = subscriber.Messages<TestMessageForTopic>();

            // Act.
            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id,
                TenantId = correctTenantId.ToString(),
                Something = "valid"
            });

            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = invalidTenantMessageId,
                TenantId = invalidTenantId.ToString(),
                Something = "invalid"
            });

            // Assert.
            await listener
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(100))
                .FirstAsync();

            var foo = await listener
                .Where(x => {
                    return x.ExampleId == invalidTenantMessageId;
                })
                .Timeout(TimeSpan.FromSeconds(10))
                .FirstAsync();
        }

        [Fact]
        public void WhenMultipleThreadsAreWriting_ThenSubscribingWorksAsExpected()
        {
        }
    }
}
