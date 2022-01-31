using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Protacon.RxMq.Abstractions;
using Protacon.RxMq.Abstractions.DefaultMessageRouting;

namespace Protacon.RxMq.ConsoleExample
{
    public class MessageService : IHostedService
    {
        private readonly ILogger<MessageService> _logger;
        private readonly IMqTopicPublisher _publisher;
        private readonly IMqTopicSubscriber _subscriber;
        private Timer _messageSender;

        private IDisposable[] _messageListeners;

        public MessageService(ILogger<MessageService> logger, IMqTopicPublisher publisher, IMqTopicSubscriber subscriber)
        {
            _logger = logger;
            _publisher = publisher;
            _subscriber = subscriber;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting message service");
            _messageListeners = new[]
            {
                _subscriber.Messages<CorrelationTestMessage1>().Subscribe(HandleFirstCorrelation),
                _subscriber.Messages<CorrelationTestMessage2>().Subscribe(HandleSecondCorrelation),
                _subscriber.Messages<CorrelationTestMessage3>().Subscribe(HandleThirdCorrelation)
            };

            _messageSender = new Timer(ExecuteTask, null, TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(-1));
            return Task.CompletedTask;
        }

        private void HandleFirstCorrelation(CorrelationTestMessage1 message)
        {
            _logger.LogInformation("Received correlation message 1. Correlatio ID {correlation}", message.CorrelationId);
            _publisher.SendAsync(new CorrelationTestMessage2
            {
                CorrelationId = message.CorrelationId
            });
        }

        private void HandleSecondCorrelation(CorrelationTestMessage2 message)
        {
            _logger.LogInformation("Received correlation message 2. Correlatio ID {correlation}", message.CorrelationId);
            _publisher.SendAsync(new CorrelationTestMessage3
            {
                CorrelationId = message.CorrelationId
            });
        }

        private void HandleThirdCorrelation(CorrelationTestMessage3 message)
        {
            _logger.LogInformation("Received correlation message 3. Correlatio ID {correlation}", message.CorrelationId);
            _logger.LogInformation("This is last message of correlation saga.");
        }

        private void ExecuteTask(object state)
        {
            var correlationId = Guid.NewGuid();
            var correlationKey = $"COR-{correlationId}";
            _logger.LogInformation("Starting correlation chain with {correlation}", correlationKey);
            var testMessage = new CorrelationTestMessage1
            {
                CorrelationId = correlationKey
            };
            _publisher.SendAsync(testMessage);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var disposable in _messageListeners)
            {
                disposable.Dispose();
            }
            await _messageSender.DisposeAsync();
        }
    }

    public class CorrelationTestMessage1 : ITopicItem, IHasCorrelationId
    {
        public string TopicName => "correlation-test-message1";

        public string CorrelationId { get; set; }
    }

    public class CorrelationTestMessage2 : ITopicItem, IHasCorrelationId
    {
        public string TopicName => "correlation-test-message2";

        public string CorrelationId { get; set; }
    }

    public class CorrelationTestMessage3 : ITopicItem, IHasCorrelationId
    {
        public string TopicName => "correlation-test-message3";

        public string CorrelationId { get; set; }
    }
}
