using System.Threading.Tasks;

namespace Protacon.RxMq.Abstractions
{
    public interface IMqTopicPublisher
    {
        Task SendAsync<T>(T message) where T : new();
    }
}