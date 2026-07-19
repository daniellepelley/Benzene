using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// <see cref="IHandlerMiddlewareBuilder"/> that contributes a
/// <see cref="ResponseEventsMiddleware{TRequest,TResponse}"/> carrying one pipeline's
/// <see cref="ResponseEventMappings"/> to every handler pipeline built for that transport pipeline.
/// </summary>
public class ResponseEventsMiddlewareBuilder : IHandlerMiddlewareBuilder
{
    private readonly ResponseEventMappings _mappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventsMiddlewareBuilder"/> class.
    /// </summary>
    /// <param name="mappings">The pipeline's mapping set and failure policy.</param>
    public ResponseEventsMiddlewareBuilder(ResponseEventMappings mappings)
    {
        _mappings = mappings;
    }

    /// <inheritdoc />
    public IMiddleware<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        return new ResponseEventsMiddleware<TRequest, TResponse>(_mappings, serviceResolver);
    }
}
