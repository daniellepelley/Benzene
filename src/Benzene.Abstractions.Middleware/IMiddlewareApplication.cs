using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

public interface IMiddlewareApplication<TEvent>
{
    Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory);
}

public interface IMiddlewareApplication<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest @event, IServiceResolverFactory serviceResolverFactory);
}