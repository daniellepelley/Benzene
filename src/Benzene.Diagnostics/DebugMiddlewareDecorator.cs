using System.Diagnostics;
using Benzene.Abstractions.Middleware;

namespace Benzene.Diagnostics;

public class DebugMiddlewareDecorator<TContext> : IMiddleware<TContext>
{
    private readonly IMiddleware<TContext> _inner;

    public DebugMiddlewareDecorator(IMiddleware<TContext> inner)
    {
        _inner = inner;
    }

    public string Name => _inner.Name;

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        Debug.WriteLine($"Middleware - {_inner.Name} starting");
        await _inner.HandleAsync(context, next);
        Debug.WriteLine($"Middleware - {_inner.Name} completed");
    }
}