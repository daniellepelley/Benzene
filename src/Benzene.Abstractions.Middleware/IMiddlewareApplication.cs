using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

public interface IMiddlewareApplication<TEvent>
{
    Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory);
}

public interface IMiddlewareApplication<TEvent, TResult>
{
    Task<TResult> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory);
}