namespace Benzene.Abstractions.Request;

public interface IRequestMapper<in TContext>
{
    TRequest? GetBody<TRequest>(TContext context) where TRequest : class;
}