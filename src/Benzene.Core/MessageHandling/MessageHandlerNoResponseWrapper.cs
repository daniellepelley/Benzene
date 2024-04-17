using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Core.Results;
using Benzene.Results;

namespace Benzene.Core.MessageHandling;

public class MessageHandlerNoResponseWrapper<TRequest, TResponse> : IMessageHandler<TRequest, TResponse>
{
    private readonly IMessageHandler<TRequest> _inner;

    public MessageHandlerNoResponseWrapper(IMessageHandler<TRequest> inner)
    {
        _inner = inner;
    }

    public async Task<IServiceResult<TResponse>> HandleAsync(TRequest request)
    {
        await _inner.HandleAsync(request);
        return ServiceResult.Accepted<TResponse>();
    }
}