using Benzene.Abstractions.Middleware;
using Polly;

namespace Benzene.Resilience.Polly;

/// <summary>
/// Middleware that runs the rest of the pipeline (<c>next</c>) through a Polly v8
/// <see cref="ResiliencePipeline"/> - so any strategy the pipeline is built with (retry, circuit
/// breaker, timeout, hedging, fallback, rate limiter, ...) applies to whatever <c>next</c> wraps: a
/// handler dispatch on an inbound pipeline, or a port/service call on an outbound one. The
/// <see cref="ResiliencePipeline"/> is supplied ready-built, so the only per-message cost is
/// <see cref="ResiliencePipeline.ExecuteAsync{TState}(System.Func{TState,System.Threading.CancellationToken,System.Threading.Tasks.ValueTask},TState,System.Threading.CancellationToken)"/>.
/// </summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
public class PollyResilienceMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly ResiliencePipeline _pipeline;
    private readonly Func<TContext, bool>? _isFailure;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResilienceMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="pipeline">The Polly resilience pipeline to execute <c>next</c> through.</param>
    /// <param name="isFailure">
    /// Optional predicate reporting whether the pipeline produced an unsuccessful result after
    /// <c>next</c> ran (e.g. <c>ctx =&gt; ctx.MessageResult?.IsSuccessful == false</c>). When supplied
    /// and it returns <c>true</c>, the middleware throws <see cref="BenzeneFailureResultException"/>
    /// so the Polly pipeline can treat the failure result as a handled outcome; the exception never
    /// escapes (see that type's docs). When <c>null</c> (the default), only thrown exceptions drive
    /// the pipeline's strategies.
    /// </param>
    public PollyResilienceMiddleware(ResiliencePipeline pipeline, Func<TContext, bool>? isFailure = null)
    {
        _pipeline = pipeline;
        _isFailure = isFailure;
    }

    /// <inheritdoc />
    public string Name => nameof(PollyResilienceMiddleware<TContext>);

    /// <inheritdoc />
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        try
        {
            await _pipeline.ExecuteAsync(static async (state, _) =>
            {
                await state.next();

                if (state._isFailure != null && state._isFailure(state.context))
                {
                    // Surface an unsuccessful result to Polly as a handled outcome. Swallowed below
                    // once the pipeline has finished, so the failure result on the context is what
                    // callers see - identical to running without this middleware.
                    throw new BenzeneFailureResultException();
                }
            }, (next, context, _isFailure)).ConfigureAwait(false);
        }
        catch (BenzeneFailureResultException)
        {
            // Retries (etc.) exhausted and the last attempt still produced a failure result. The
            // result is already on the context; do not propagate the sentinel.
        }
    }
}
