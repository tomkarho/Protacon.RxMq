using System;
using System.Threading.Tasks;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqQueuePublisher
    {
        Task SendAsync<T>(T message) where T : new();
    }
}