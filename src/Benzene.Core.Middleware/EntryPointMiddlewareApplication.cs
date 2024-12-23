using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class EntryPointMiddlewareApplication<TEvent> : IEntryPointMiddlewareApplication<TEvent>
{
    private readonly IMiddlewareApplication<TEvent> _middlewareApplication;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    public EntryPointMiddlewareApplication(IMiddlewareApplication<TEvent> middlewareApplication, IServiceResolverFactory serviceResolverFactory)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _middlewareApplication = middlewareApplication;
    }

    public Task SendAsync(TEvent @event)
    {
        return _middlewareApplication.HandleAsync(@event, _serviceResolverFactory.CreateScope());
    }
}

public class EntryPointMiddlewareApplication<TEvent, TResult> : IEntryPointMiddlewareApplication<TEvent, TResult>
{
    private readonly IMiddlewareApplication<TEvent,TResult> _middlewareApplication;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    public EntryPointMiddlewareApplication(IMiddlewareApplication<TEvent, TResult> middlewareApplication,
        IServiceResolverFactory serviceResolverFactory)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _middlewareApplication = middlewareApplication;
    }

    public Task<TResult> SendAsync(TEvent @event)
    {
        return _middlewareApplication.HandleAsync(@event, _serviceResolverFactory.CreateScope());
    }
}
