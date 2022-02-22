using System;
using System.Threading;
using Protacon.RxMq.Abstractions;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;
using Protacon.RxMq.AzureServiceBusLegacy.Tests;
using Protacon.RxMq.AzureServiceBusLegacy.Topic;

namespace Protacon.RxMq.LegacyConsoleExample
{
    public class LegacyMessageService : IDisposable
    {
        private readonly IMqTopicPublisher _publisher;
        private readonly IMqTopicSubscriber _subscriber;
        private readonly Timer _timer;

        public LegacyMessageService()
        {
            var settings = TestSettings.MqSettingsForTopic();
            _publisher = new AzureBusTopicPublisher(settings, Console.WriteLine, Console.WriteLine);
            _subscriber = new AzureBusTopicSubscriber(settings, Console.WriteLine, Console.WriteLine);

            Console.WriteLine("Starting legacy message service");
            var listener1 = _subscriber.Messages<CorrelationLegacyTestMessage1>();
            var listener2 = _subscriber.Messages<CorrelationLegacyTestMessage2>();
            var listener3 = _subscriber.Messages<CorrelationLegacyTestMessage3>();

            listener1.Subscribe(HandleFirstCorrelation);
            listener2.Subscribe(HandleSecondCorrelation);
            listener3.Subscribe(HandleThirdCorrelation);

            _timer = new Timer(Execute, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        }

        private void HandleFirstCorrelation(CorrelationLegacyTestMessage1 message)
        {
            Console.WriteLine("Received legacy correlation message 1. Correlation ID " + message.CorrelationId);
            _publisher.SendAsync(new CorrelationLegacyTestMessage2
            {
                CorrelationId = message.CorrelationId
            });
        }

        private void HandleSecondCorrelation(CorrelationLegacyTestMessage2 message)
        {
            Console.WriteLine("Received legacy correlation message 2. Correlation ID " + message.CorrelationId);
            _publisher.SendAsync(new CorrelationLegacyTestMessage3
            {
                CorrelationId = message.CorrelationId
            });
        }

        private void HandleThirdCorrelation(CorrelationLegacyTestMessage3 message)
        {
            Console.WriteLine("Received legacy correlation message 3. Correlation ID " + message.CorrelationId);
            Console.WriteLine("This is last legacy message of correlation saga.");
        }

        private void Execute(object state)
        {
            var correlationId = Guid.NewGuid();
            var correlationKey = $"COR-{correlationId}";
            Console.WriteLine("Starting legacy correlation chain with " + correlationKey);
            var testMessage = new CorrelationLegacyTestMessage1
            {
                CorrelationId = correlationKey
            };
            _publisher.SendAsync(testMessage);
        }

        public void Dispose()
        {
            _timer.Dispose();
            _subscriber.Dispose();
        }
    }

    public class CorrelationLegacyTestMessage1 : ITopicItem, IHasCorrelationId
    {
        public string TopicName => "correlation-legacy-test-message1";
        public string CorrelationId { get; set; }
    }

    public class CorrelationLegacyTestMessage2 : ITopicItem, IHasCorrelationId
    {
        public string TopicName => "correlation-legacy-test-message2";
        public string CorrelationId { get; set; }
    }

    public class CorrelationLegacyTestMessage3 : ITopicItem, IHasCorrelationId
    {
        public string TopicName => "correlation-legacy-test-message3";
        public string CorrelationId { get; set; }
    }
}
