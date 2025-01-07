using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers;

public class ResponseMessageResultSetterBase<TContext> : IResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseMessageResultSetterBase(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
    }
}