namespace Benzene.Extras.Broadcast;

public interface IEventSender
{
    Task SendAsync<T>(string topic, T payload);
}