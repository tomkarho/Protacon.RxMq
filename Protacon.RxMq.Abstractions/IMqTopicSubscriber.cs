using System;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqTopicSubscriber: IDisposable
    {
        IObservable<T> Messages<T>() where T : new();
    }
}