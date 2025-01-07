using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.Response;

public class ResponseHandlerContainer<TContext> : IResponseHandlerContainer<TContext>
    where TContext : class
{
    private readonly IResponseHandler<TContext>[] _responseHandlers;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    public ResponseHandlerContainer(IBenzeneResponseAdapter<TContext> responseAdapter, IEnumerable<IResponseHandler<TContext>> responseHandlers)
    {
        _responseAdapter = responseAdapter;
        _responseHandlers = responseHandlers.ToArray();
    }

    public async Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        foreach (var responseHandler in _responseHandlers)
        {
            switch (responseHandler)
            {
                case ISyncResponseHandler<TContext> syncResponseHandler:
                    syncResponseHandler.HandleAsync(context, messageHandlerResult);
                    break;
                case IAsyncResponseHandler<TContext> asyncResponseHandler:
                    await asyncResponseHandler.HandleAsync(context, messageHandlerResult);
                    break;
            }
        }

        await _responseAdapter.FinalizeAsync(context);
    }
}
