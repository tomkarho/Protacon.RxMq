using System;
using System.Threading.Tasks;

namespace Protacon.RxMq.Abstractions
{
    public interface IMq
    {
        IObservable<Envelope<T>> Messages<T>() where T : IRoutingKey, new();
        IObservable<Envelope<T>> SendRpc<T>(T message) where T : IRoutingKey, new();
        Task SendAsync<T>(T message) where T : IRoutingKey, new();
    }
}