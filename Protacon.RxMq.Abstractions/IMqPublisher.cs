using System;
using System.Threading.Tasks;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqPublisher
    {
        IObservable<Envelope<T>> SendRpc<T>(T message) where T : IRoutingKey, new();
        Task SendAsync<T>(T message) where T : IRoutingKey, new();
    }
}