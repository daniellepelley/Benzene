namespace Benzene.Abstractions.Middleware;

public interface IEntryPointMiddlewareApplication;

public interface IEntryPointMiddlewareApplication<in TEvent> : IEntryPointMiddlewareApplication
{
    Task SendAsync(TEvent @event);
}

public interface IEntryPointMiddlewareApplication<in TEvent, TResult> : IEntryPointMiddlewareApplication
{
    Task<TResult> SendAsync(TEvent @event);
}
