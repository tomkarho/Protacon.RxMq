using Microsoft.Azure.ServiceBus;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBus
{
    public class MessageAckAzureServiceBus: IMessageAck
    {
        private readonly IQueueClient _client;
        private readonly string _lockTag;

        public MessageAckAzureServiceBus(IQueueClient client, string lockTag)
        {
            _client = client;
            _lockTag = lockTag;
        }

        public void Ack()
        {
            _client.CompleteAsync(_lockTag).Wait();
        }

        public void Nack()
        {
            _client.AbandonAsync(_lockTag).Wait();
        }
    }
}