using Benzene.Abstractions.Middleware;
using Polly;

namespace Benzene.Resilience.Polly;

/// <summary>
/// Pipeline-builder extensions for adding Polly v8 resilience to a Benzene middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Runs the rest of the pipeline through the given pre-built Polly
    /// <see cref="ResiliencePipeline"/>. Strategies fire on exceptions thrown by downstream
    /// middleware/handlers.
    /// </summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="pipeline">The Polly resilience pipeline.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseResiliencePipeline<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, ResiliencePipeline pipeline)
    {
        return app.Use(_ => new PollyResilienceMiddleware<TContext>(pipeline));
    }

    /// <summary>
    /// Runs the rest of the pipeline through the given pre-built Polly
    /// <see cref="ResiliencePipeline"/>, additionally treating an unsuccessful result (per
    /// <paramref name="isFailure"/>) as a handled outcome so it can be retried, trip a breaker, or be
    /// fallen back from - not just thrown exceptions. Configure the pipeline to handle
    /// <see cref="BenzeneFailureResultException"/> for this to take effect.
    /// </summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="pipeline">The Polly resilience pipeline.</param>
    /// <param name="isFailure">Predicate reporting whether the context holds an unsuccessful result after <c>next</c> ran.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseResiliencePipeline<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, ResiliencePipeline pipeline, Func<TContext, bool> isFailure)
    {
        return app.Use(_ => new PollyResilienceMiddleware<TContext>(pipeline, isFailure));
    }

    /// <summary>
    /// Builds a Polly <see cref="ResiliencePipeline"/> inline via <paramref name="configure"/> and
    /// runs the rest of the pipeline through it.
    /// </summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="configure">Configures the Polly <see cref="ResiliencePipelineBuilder"/> (add retry, circuit breaker, timeout, ...).</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseResiliencePipeline<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<ResiliencePipelineBuilder> configure)
    {
        var builder = new ResiliencePipelineBuilder();
        configure(builder);
        return app.UseResiliencePipeline(builder.Build());
    }

    /// <summary>
    /// Builds a Polly <see cref="ResiliencePipeline"/> inline via <paramref name="configure"/> and
    /// runs the rest of the pipeline through it, treating an unsuccessful result (per
    /// <paramref name="isFailure"/>) as a handled outcome - see the pipeline+predicate overload.
    /// </summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="app">The pipeline builder.</param>
    /// <param name="configure">Configures the Polly <see cref="ResiliencePipelineBuilder"/>.</param>
    /// <param name="isFailure">Predicate reporting whether the context holds an unsuccessful result after <c>next</c> ran.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseResiliencePipeline<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<ResiliencePipelineBuilder> configure, Func<TContext, bool> isFailure)
    {
        var builder = new ResiliencePipelineBuilder();
        configure(builder);
        return app.UseResiliencePipeline(builder.Build(), isFailure);
    }
}
