using System;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqSubscriber: IDisposable
    {
        IObservable<Envelope<T>> Messages<T>() where T : new();
    }
}