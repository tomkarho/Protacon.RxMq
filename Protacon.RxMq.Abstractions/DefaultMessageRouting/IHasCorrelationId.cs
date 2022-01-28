namespace Protacon.RxMq.Abstractions.DefaultMessageRouting
{
    public interface IHasCorrelationId
    {
        string CorrelationId { get; }
    }
}
