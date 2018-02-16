using System;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqQueSubscriber: IDisposable
    {
        IObservable<Envelope<T>> Messages<T>() where T : new();
    }
}