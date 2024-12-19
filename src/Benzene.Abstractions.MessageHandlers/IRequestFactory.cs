namespace Benzene.Abstractions.MessageHandlers;

public interface IRequestFactory
{
    TRequest? GetRequest<TRequest>() where TRequest : class;
}