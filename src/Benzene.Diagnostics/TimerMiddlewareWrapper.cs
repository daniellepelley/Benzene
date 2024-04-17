using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Diagnostics.Timers;

namespace Benzene.Diagnostics;

public class TimerMiddlewareWrapper : IMiddlewareWrapper
{
    private readonly IProcessTimerFactory[] _processTimerFactories;

    public TimerMiddlewareWrapper(IEnumerable<IProcessTimerFactory> processTimerFactories)
    {
        _processTimerFactories = processTimerFactories.ToArray();
    }
    
    public IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return new TimerMiddlewareDecorator<TContext>(middleware, _processTimerFactories);
    }
}
