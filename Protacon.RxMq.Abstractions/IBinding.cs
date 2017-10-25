using System;

namespace Protacon.RxMq.Abstractions
{
    public interface IBinding: IDisposable
    {
        Type Type { get; }
    }
}