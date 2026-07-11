using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides middleware that catches and handles exceptions thrown during pipeline execution.
/// </summary>
/// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
/// <remarks>
/// This middleware wraps the execution of downstream middleware in a try-catch block, allowing
/// centralized exception handling. Exceptions are passed to the configured handler along with the
/// current context, enabling context-aware error handling and response generation.
/// </remarks>
public class ExceptionHandlerMiddleware<TContext>(Action<TContext, Exception> onException) : IMiddleware<TContext>
{
    /// <summary>
    /// Gets the name of this middleware component.
    /// </summary>
    public string Name => "ExceptionHandler";

    /// <summary>
    /// Handles the middleware execution by wrapping the next middleware in exception handling logic.
    /// </summary>
    /// <param name="context">The current context being processed.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            onException(context, ex);
        }
    }
}
