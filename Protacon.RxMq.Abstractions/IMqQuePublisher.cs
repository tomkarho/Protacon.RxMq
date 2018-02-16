using System;
using System.Threading.Tasks;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqQuePublisher
    {
        Task SendAsync<T>(T message) where T : new();
    }
}