using Microsoft.ServiceBus.Messaging;
using Protacon.RxMq.Abstractions;

namespace Protacon.RxMq.AzureServiceBusLegacy
{
    public class MessageAckAzureServiceBus: IMessageAck
    {
        private readonly BrokeredMessage _message;

        public MessageAckAzureServiceBus(BrokeredMessage message)
        {
            _message = message;
        }

        public void Ack()
        {
            _message.Complete();
        }

        public void Nack()
        {
            _message.Abandon();
        }
    }
}