using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerNoResultWrapper<TRequest, TResponse> : IMessageHandler<TRequest, TResponse>
{
    private readonly IMessageHandler<TRequest> _inner;

    public MessageHandlerNoResultWrapper(IMessageHandler<TRequest> inner)
    {
        _inner = inner;
    }

    public async Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request)
    {
        await _inner.HandleAsync(request);
        return BenzeneResult.Accepted<TResponse>();
    }
}