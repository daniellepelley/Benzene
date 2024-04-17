using System.Threading.Tasks;

namespace Benzene.Elements.Core.Broadcast;

public interface IEventSender
{
    Task SendAsync<T>(string topic, T payload);
}