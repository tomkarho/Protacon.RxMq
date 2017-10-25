namespace Protacon.RxMq.Abstractions
{
    public class Envelope<T>
    {
        private readonly IMessageAck _messageAck;

        public Envelope(T message, IMessageAck messageAck)
        {
            _messageAck = messageAck;
            Message = message;
        }
        public T Message { get; }

        public void Ack()
        {
            _messageAck.Ack();
        }

        public void Nack()
        {
            _messageAck.Nack();
        }
    }
}
