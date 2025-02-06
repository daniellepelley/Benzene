using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers;

public class ResponseMessageMessageHandlerResultSetterBase<TContext> : IMessageHandlerResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseMessageMessageHandlerResultSetterBase(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
    }
}