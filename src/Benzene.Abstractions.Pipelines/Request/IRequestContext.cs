namespace Benzene.Abstractions.Request;

public interface IRequestContext<TRequest>
{
    TRequest Request { get; }
}