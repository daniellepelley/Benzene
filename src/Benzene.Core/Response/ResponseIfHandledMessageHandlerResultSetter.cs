using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.Response;

public class ResponseIfHandledMessageHandlerResultSetter<TContext> : IMessageHandlerResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseIfHandledMessageHandlerResultSetter(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (messageHandlerResult.Topic != null && messageHandlerResult.Topic.Id != Constants.Missing)
        {
            await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
        }
    }
}

