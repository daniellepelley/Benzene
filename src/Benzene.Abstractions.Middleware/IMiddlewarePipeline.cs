using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Represents an executable middleware pipeline that processes context objects through a chain of middleware components.
/// </summary>
/// <typeparam name="TContext">The type of context object that flows through the pipeline.</typeparam>
/// <remarks>
/// The middleware pipeline encapsulates a chain of middleware components that execute in sequence.
/// Pipelines are created using <see cref="IMiddlewarePipelineBuilder{TContext}"/> and are immutable once built.
/// Each middleware in the pipeline can:
/// - Process the context before calling the next middleware
/// - Delegate to the next middleware in the chain
/// - Process the context after the next middleware completes
/// - Short-circuit the pipeline by not calling the next middleware
/// Pipelines are reusable and thread-safe, making them suitable for concurrent execution across multiple requests.
/// </remarks>
public interface IMiddlewarePipeline<TContext>
{
    /// <summary>
    /// Executes the middleware pipeline asynchronously with the provided context.
    /// </summary>
    /// <param name="context">The context object to process through the pipeline. The context carries request data, state, and is modified by middleware.</param>
    /// <param name="serviceResolver">The service resolver for resolving dependencies required by middleware components during execution.</param>
    /// <returns>A task that represents the asynchronous pipeline execution.</returns>
    /// <remarks>
    /// This method initiates the pipeline execution, invoking each middleware in the registration order.
    /// The pipeline completes when:
    /// - All middleware have executed successfully
    /// - A middleware short-circuits the chain by not calling the next delegate
    /// - An unhandled exception is thrown by a middleware
    /// The context object is passed by reference through the pipeline and may be modified by any middleware.
    /// </remarks>
    Task HandleAsync(TContext context, IServiceResolver serviceResolver);
}