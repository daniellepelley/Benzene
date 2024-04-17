using Benzene.Abstractions.Middleware;
using Benzene.Diagnostics.Timers;

namespace Benzene.Diagnostics;

public class TimerMiddlewareDecorator<TContext> : IMiddleware<TContext>
{
    private readonly IMiddleware<TContext> _inner;
    private readonly IProcessTimerFactory[] _processTimerFactories;

    public TimerMiddlewareDecorator(IMiddleware<TContext> inner, IProcessTimerFactory[] processTimerFactories)
    {
        _processTimerFactories = processTimerFactories;
        _inner = inner;
    }

    public string Name => _inner.Name;

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        using var timer = new CompositeProcessTimerFactory(_processTimerFactories).Create(Name);
        await _inner.HandleAsync(context, next);
    }
}