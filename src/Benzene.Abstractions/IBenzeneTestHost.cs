namespace Benzene.Abstractions;

public interface IBenzeneTestHost
{
    Task<TResponse> SendEventAsync<TResponse>(object awsEvent);
}