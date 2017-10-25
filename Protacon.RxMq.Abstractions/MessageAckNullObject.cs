namespace Protacon.RxMq.Abstractions
{
    public class MessageAckNullObject: IMessageAck
    {
        public void Ack()
        {
        }

        public void Nack()
        {
        }
    }
}
