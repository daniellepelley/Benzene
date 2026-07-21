using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides an abstract base class for middleware that routes requests to different handlers based on request properties.
/// </summary>
/// <typeparam name="TRequest">The type of request extracted from the context.</typeparam>
/// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
/// <remarks>
/// This abstract middleware enables routing patterns where different types or categories of requests
/// within the same context can be dispatched to different handlers. Derived classes implement the
/// extraction, routing logic, and handling behavior.
/// </remarks>
public abstract class MiddlewareRouter<TRequest, TContext>(IServiceResolver serviceResolver) : IMiddleware<TContext>
{
    /// <summary>
    /// Gets the name of this middleware component. Defaults to the concrete router's own type name
    /// (via <see cref="object.GetType"/>) rather than a fixed <c>"MiddlewareRouter"</c>, so tracing
    /// shows which flavour of router ran (e.g. <c>SqsLambdaHandler</c>, <c>ApiGatewayLambdaHandler</c>)
    /// instead of the same generic label for every one. Override to supply a custom name.
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// Handles the middleware execution by extracting the request, checking if it can be handled,
    /// and either routing it or passing control to the next middleware.
    /// </summary>
    /// <param name="context">The current context being processed.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task HandleAsync(TContext context, Func<Task> next)
    {
        var request = TryExtractRequest(context);

        if (request == null)
        {
            await next();
        }
        else
        {
            if (CanHandle(request))
            {
                await HandleFunction(request, context, serviceResolver.GetService<IServiceResolverFactory>());
            }
            else
            {
                await next();
            }
        }
    }

    /// <summary>
    /// Determines whether this router can handle the given request.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <returns>True if this router can handle the request; otherwise, false.</returns>
    protected abstract bool CanHandle(TRequest request);

    /// <summary>
    /// Handles the request by executing the appropriate handler.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="context">The current context being processed.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for dependency resolution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task HandleFunction(TRequest request, TContext context, IServiceResolverFactory serviceResolverFactory);

    /// <summary>
    /// Attempts to extract a request from the context.
    /// </summary>
    /// <param name="context">The context to extract the request from.</param>
    /// <returns>The extracted request, or null if no request could be extracted.</returns>
    protected abstract TRequest TryExtractRequest(TContext context);
}
