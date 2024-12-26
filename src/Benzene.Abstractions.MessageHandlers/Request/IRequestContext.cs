namespace Benzene.Abstractions.MessageHandlers.Request;

public interface IRequestContext<TRequest>
{
    TRequest Request { get; }
}