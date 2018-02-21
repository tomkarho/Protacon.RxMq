using System;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqQueSubscriber: IDisposable
    {
        IObservable<T> Messages<T>() where T : new();
    }
}