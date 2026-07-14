using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers.Filters;

/// <summary>
/// Handler middleware that runs any registered <see cref="IFilter{T}"/> for the request type before
/// letting the pipeline continue to the handler.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by the pipeline.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response produced by the pipeline.</typeparam>
/// <remarks>
/// Added to a handler's pipeline via <see cref="FiltersMiddlewareBuilder"/>, itself registered via
/// the <c>UseFilters</c> extension methods on <see cref="DependencyExtensions"/>.
/// If no <see cref="IFilter{T}"/> is registered for <typeparamref name="TRequest"/>, this middleware
/// always lets the request through.
/// </remarks>
public class FiltersMiddleware<TRequest, TResponse> : IMiddleware<IMessageHandlerContext<TRequest, TResponse>>
    where TRequest : class
{
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="FiltersMiddleware{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="serviceResolver">Used to resolve the registered <see cref="IFilter{T}"/> for <typeparamref name="TRequest"/>, if any.</param>
    public FiltersMiddleware(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public string Name => "Filters";

    /// <summary>
    /// Runs the registered <see cref="IFilter{T}"/> (if any) against the request, short-circuiting
    /// with an "ignored" result if it rejects the request; otherwise continues the pipeline.
    /// </summary>
    /// <param name="context">The current handler invocation's context.</param>
    /// <param name="next">Invoked to continue the pipeline when the filter allows the request (or none is registered).</param>
    public async Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, Func<Task> next)
    {
        var filter = _serviceResolver.TryGetService<IFilter<TRequest>>();
        if (filter != null)
        {
            var canProcess = filter.Filter(context.Request);
            if (!canProcess)
            {
                context.Response =
                    BenzeneResult.Ignored<TResponse>();
                return;
            }
        }
        await next();
    }
}
