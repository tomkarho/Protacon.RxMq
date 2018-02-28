using System;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqQueueSubscriber: IDisposable
    {
        IObservable<T> Messages<T>() where T : new();
    }
}