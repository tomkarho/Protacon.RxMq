using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Protacon.RxMq.AzureServiceBusLegacy.Tests.Messages;
using Protacon.RxMq.AzureServiceBusLegacy.Topic;
using Xunit;

namespace Protacon.RxMq.AzureServiceBusLegacy.Tests
{
    public class AzureBusTopicIntegrationTests
    {
        [Fact]
        public async void WhenMessageIsSend_ThenItCanBeReceived()
        {
            var settings = TestSettings.MqSettingsForTopic();
            var publisher = new AzureBusTopicPublisher(settings, _ => { }, _ => { });
            var subscriber = new AzureBusTopicSubscriber(settings, _ => { }, _ => { });

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

            var settings = TestSettings.MqSettingsForTopic();
            settings.AzureMessagePropertyBuilder = message => new Dictionary<string, object> { { "tenant", ((TestMessageForTopic)message).TenantId } };
            settings.AzureSubscriptionRules.Clear();
            settings.AzureSubscriptionRules.Add("filter", new SqlFilter($"user.tenant='{correctTenantId}'"));

            var publisher = new AzureBusTopicPublisher(settings, _ => {}, _ => {});
            var subscriber = new AzureBusTopicSubscriber(settings, _ => {}, _ => {});

            var id = Guid.NewGuid();
            var invalidTenantMessageId = Guid.NewGuid();
            var listener = subscriber.Messages<TestMessageForTopic>();

            // Act.
            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id,
            });

            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = invalidTenantMessageId
            });

            // Assert.
            await listener
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(10))
                .FirstAsync();

            listener
                .Where(x => x.ExampleId == invalidTenantMessageId)
                .Timeout(TimeSpan.FromSeconds(10))
                .Invoking(x => x.FirstAsync().Wait())
                .ShouldThrow<TimeoutException>();
        }

        [Fact]
        public void WhenMultipleThreadsAreWriting_ThenSubscribingWorksAsExpected()
        {
        }
    }
}