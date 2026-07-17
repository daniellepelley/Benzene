using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// Provides pipeline steps for consuming timer ticks and extension methods for dispatching them to
/// a built <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a terminal tick-processing step that receives the <see cref="TimerContext"/> - for
    /// scheduled work that doesn't need message-handler routing.
    /// </summary>
    /// <param name="app">The timer pipeline builder.</param>
    /// <param name="process">The delegate that consumes the tick.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TimerContext> UseTick(
        this IMiddlewarePipelineBuilder<TimerContext> app,
        Func<TimerContext, Task> process)
    {
        return app.Use("Tick", async (TimerContext context, Func<Task> next) =>
        {
            await process(context);
            await next();
        });
    }

    /// <summary>
    /// Adds a terminal tick-processing step that receives the timer info directly - the common case
    /// when you don't need the rest of the context.
    /// </summary>
    /// <param name="app">The timer pipeline builder.</param>
    /// <param name="process">The delegate that consumes the timer info.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TimerContext> UseTick(
        this IMiddlewarePipelineBuilder<TimerContext> app,
        Func<TimerTriggerInfo, Task> process)
    {
        return app.UseTick(context => process(context.Timer));
    }

    /// <summary>
    /// Dispatches a timer tick to the Azure Function app's timer entry point application.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="timer">The tick's timer information (bind the trigger parameter as <see cref="TimerTriggerInfo"/>).</param>
    /// <returns>A task that completes when the tick has been handled.</returns>
    public static Task HandleTimer(this IAzureFunctionApp source, TimerTriggerInfo timer)
    {
        return source.HandleAsync(timer);
    }

    /// <summary>
    /// Dispatches a timer tick with no schedule information - for triggers that don't bind the
    /// timer parameter.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <returns>A task that completes when the tick has been handled.</returns>
    public static Task HandleTimer(this IAzureFunctionApp source)
    {
        return source.HandleTimer(new TimerTriggerInfo());
    }
}
