using System.Threading.Tasks;

namespace Benzene.Core.Broadcast;

public interface IEventSender
{
    Task SendAsync<T>(string topic, T payload);
}