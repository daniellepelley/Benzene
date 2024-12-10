namespace Benzene.Abstractions.Middleware;

public interface IEntryPointMiddlewareApplication;

public interface IEntryPointMiddlewareApplication<in TEvent> : IEntryPointMiddlewareApplication
{
    Task HandleAsync(TEvent @event);
}
public interface IEntryPointMiddlewareApplication<in TEvent, TResult> : IEntryPointMiddlewareApplication
{
    Task<TResult> HandleAsync(TEvent @event);
}
