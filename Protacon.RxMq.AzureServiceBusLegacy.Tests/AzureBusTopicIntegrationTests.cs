using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.ServiceBus;
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

            var publisher = new AzureBusTopicPublisher(settings, _ => {}, _ => throw new InvalidOperationException("Errorlogger"));
            var subscriber = new AzureBusTopicSubscriber(settings, _ => {}, _ => throw new InvalidOperationException("Errorlogger"));

            var id = Guid.NewGuid();
            var invalidTenantMessageId = Guid.NewGuid();
            var listener = subscriber.Messages<TestMessageForTopic>();

            // Act.
            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id,
                TenantId = correctTenantId
            });

            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = invalidTenantMessageId,
                TenantId = Guid.NewGuid()
            });

            // Assert.
            await listener
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync();

            listener
                .Where(x => x.ExampleId == invalidTenantMessageId)
                .Timeout(TimeSpan.FromSeconds(15))
                .Invoking(x => x.FirstAsync().Wait())
                .ShouldThrow<TimeoutException>();
        }

        [Fact]
        public async void WhenTopicIsRemoved_ThenTopicIsCreatedAgain()
        {
            var settings = TestSettings.MqSettingsForTopic();

            var subscriber = new AzureBusTopicSubscriber(settings, _ => { }, _ => { });
            var publisher = new AzureBusTopicPublisher(settings, _ => { }, _ => throw new InvalidOperationException("Errorlogger"));

            var id = Guid.NewGuid();
            var listener = subscriber.Messages<TestMessageForTopic>();


            var name = settings.TopicNameBuilder(typeof(TestMessageForTopic));
            var nameSpace = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
            var topic = await nameSpace.GetTopicAsync(name);
            topic.Should().NotBeNull("Topic should exist at this point");

            await nameSpace.DeleteTopicAsync(name);

            Thread.Sleep(TimeSpan.FromSeconds(30));

            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id
            });

            await listener
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync();
        }

        [Fact]
        public async void WhenSusbcriptionIsRemoved_ThenSubscriptionIsCreatedAgain()
        {
            var settings = TestSettings.MqSettingsForTopic();

            var subscriber = new AzureBusTopicSubscriber(settings, _ => { }, _ => { });
            var publisher = new AzureBusTopicPublisher(settings, _ => { }, _ => throw new InvalidOperationException("Errorlogger"));

            var id = Guid.NewGuid();
            var listener = subscriber.Messages<TestMessageForTopic>();


            var name = settings.TopicNameBuilder(typeof(TestMessageForTopic));
            var nameSpace = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
            var topic = await nameSpace.GetTopicAsync(name);

            var subscriptions = await nameSpace.GetSubscriptionsAsync(topic.Path);
            foreach (var subscriptionDescription in subscriptions)
            {
                await nameSpace.DeleteSubscriptionAsync(topic.Path, subscriptionDescription.Name);
            }

            Thread.Sleep(TimeSpan.FromSeconds(30));

            await publisher.SendAsync(new TestMessageForTopic
            {
                ExampleId = id
            });

            await listener
                .Where(x => x.ExampleId == id)
                .Timeout(TimeSpan.FromSeconds(30))
                .FirstAsync();
        }

        [Fact]
        public async void WhenSubscriptionMessagesListenerIsSubscribed_PreviousSubscriptionExists_ThenSubscriptionIsCreatedAgain()
        {
            var settings = TestSettings.MqSettingsForTopic();

            var tickSeconds = 10;
            var name = settings.TopicNameBuilder(typeof(TestMessageForTopic));
            var nameSpace = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);
            var topic = await nameSpace.CreateTopicAsync(name);
            var subscriptionName = $"{topic.Path}.{settings.TopicSubscriberId}";
            var initiallyCreated = DateTime.UtcNow;

            var subscriptionDescription = new SubscriptionDescription(topic.Path, subscriptionName);
            await nameSpace.CreateSubscriptionAsync(subscriptionDescription);

            Thread.Sleep(TimeSpan.FromSeconds(tickSeconds));

            var subscriber = new AzureBusTopicSubscriber(settings, _ => { }, _ => { });
            var listener = subscriber.Messages<TestMessageForTopic>();
            listener.Subscribe(_ => { });
            var subscription = await nameSpace.GetSubscriptionAsync(topic.Path, subscriptionName);

            var newlyCreated = subscription.CreatedAt > initiallyCreated.AddSeconds(tickSeconds);
            Assert.True(newlyCreated);
        }
    }
}
