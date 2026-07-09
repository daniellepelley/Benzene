using System.Diagnostics;
using Benzene.Abstractions.Middleware;

namespace Benzene.Diagnostics;

public class TimerMiddleware<TContext>(Action<TContext, long> onTimer) : IMiddleware<TContext>
{
    public string Name => "Timer";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();
            onTimer(context, stopwatch.ElapsedMilliseconds);
        }
    }
}
