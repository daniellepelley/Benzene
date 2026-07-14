using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers.Filters;

/// <summary>
/// <see cref="IHandlerMiddlewareBuilder"/> that adds a <see cref="FiltersMiddleware{TRequest,TResponse}"/>
/// step to every handler pipeline. Added by <c>UseFilters</c> so filters run for every handler,
/// regardless of whether a matching <see cref="IFilter{T}"/> is actually registered for its request type.
/// </summary>
public class FiltersMiddlewareBuilder : IHandlerMiddlewareBuilder
{
    /// <inheritdoc />
    public IMiddleware<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        return new FiltersMiddleware<TRequest, TResponse>(serviceResolver);
    }
}
