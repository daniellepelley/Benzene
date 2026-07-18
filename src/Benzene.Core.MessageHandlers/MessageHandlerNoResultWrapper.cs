using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Adapts a fire-and-forget <see cref="IMessageHandler{TRequest}"/> (no response payload) to the
/// request/response <see cref="IMessageHandler{TRequest,TResponse}"/> shape, so it can flow through
/// the same handler pipeline as handlers that do produce a response.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request the inner handler accepts.</typeparam>
/// <typeparam name="TResponse">The response type to present the wrapped handler as. The inner handler never actually produces one.</typeparam>
/// <remarks>
/// After the inner handler completes, this always returns an accepted result with no payload -
/// there is no <typeparamref name="TResponse"/> value to report.
/// </remarks>
internal class MessageHandlerNoResultWrapper<TRequest, TResponse> : IMessageHandler<TRequest, TResponse>
{
    private readonly IMessageHandler<TRequest> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHandlerNoResultWrapper{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="inner">The no-response handler to wrap.</param>
    public MessageHandlerNoResultWrapper(IMessageHandler<TRequest> inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Invokes the wrapped no-response handler, then returns an accepted result with no payload.
    /// </summary>
    /// <param name="request">The strongly-typed request to handle.</param>
    /// <returns>An accepted <see cref="IBenzeneResult{TResponse}"/> with a default payload, once the inner handler completes.</returns>
    public async Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request)
    {
        await _inner.HandleAsync(request);
        return BenzeneResult.Accepted<TResponse>();
    }
}
