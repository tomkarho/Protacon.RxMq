namespace Protacon.RxMq.Abstractions
{
    public interface IMessageAck
    {
        void Ack();
        void Nack();
    }
}