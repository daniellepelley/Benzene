namespace Benzene.Abstractions.MessageHandling;

public interface IRequestFactory
{
    TRequest GetRequest<TRequest>() where TRequest : class;
}