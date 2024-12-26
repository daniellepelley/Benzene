namespace Benzene.Abstractions.MessageHandlers;

public interface IRequestMapperThunk
{
    TRequest? GetRequest<TRequest>() where TRequest : class;
}