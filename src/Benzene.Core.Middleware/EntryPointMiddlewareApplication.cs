using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class EntryPointMiddlewareApplication<TEvent>(
    IMiddlewareApplication<TEvent> middlewareApplication,
    IServiceResolverFactory serviceResolverFactory)
    : IEntryPointMiddlewareApplication<TEvent>
{
    public Task SendAsync(TEvent @event)
    {
        return middlewareApplication.HandleAsync(@event, serviceResolverFactory);
    }
}

public class EntryPointMiddlewareApplication<TEvent, TResult>(
    IMiddlewareApplication<TEvent, TResult> middlewareApplication,
    IServiceResolverFactory serviceResolverFactory)
    : IEntryPointMiddlewareApplication<TEvent, TResult>
{
    public Task<TResult> SendAsync(TEvent @event)
    {
        return middlewareApplication.HandleAsync(@event, serviceResolverFactory);
    }
}
