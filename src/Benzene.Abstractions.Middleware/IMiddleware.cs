namespace Benzene.Abstractions.Middleware;

/// <summary>
/// Represents a middleware component in the Benzene pipeline that processes requests in a chain of responsibility pattern.
/// </summary>
/// <typeparam name="TContext">The type of context object passed through the middleware pipeline. Uses contravariance to allow flexible context composition.</typeparam>
/// <remarks>
/// Middleware components are executed in the order they are registered in the pipeline. Each middleware can:
/// - Perform processing before calling the next middleware
/// - Call the next middleware in the chain via the <paramref name="next"/> delegate
/// - Perform processing after the next middleware completes
/// - Short-circuit the pipeline by not calling <paramref name="next"/>
/// This interface uses contravariance (<c>in</c>) on the context type parameter to enable flexible middleware composition across different context types.
/// </remarks>
public interface IMiddleware<in TContext>
{
    /// <summary>
    /// Gets the unique name of this middleware component.
    /// </summary>
    /// <value>A string identifier for this middleware, typically used for logging, debugging, and diagnostics.</value>
    string Name { get; }

    /// <summary>
    /// Handles the middleware processing asynchronously.
    /// </summary>
    /// <param name="context">The context object containing request/response data and state for the current pipeline execution.</param>
    /// <param name="next">A delegate representing the next middleware in the pipeline. Invoke this to continue the pipeline; omit the call to short-circuit.</param>
    /// <returns>A task that represents the asynchronous middleware operation.</returns>
    /// <remarks>
    /// Implementations should:
    /// - Perform any pre-processing logic before calling <paramref name="next"/>
    /// - Invoke <paramref name="next"/> to continue the pipeline (unless short-circuiting intentionally)
    /// - Perform any post-processing logic after <paramref name="next"/> completes
    /// - Handle exceptions appropriately (either catch and handle, or allow to propagate)
    /// </remarks>
    Task HandleAsync(TContext context, Func<Task> next);
}
