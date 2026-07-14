using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Default <see cref="IResponseHandlerContainer{TContext}"/> implementation: runs every registered
/// <see cref="IResponseHandler{TContext}"/> (dispatching sync vs. async ones appropriately, in
/// registration order) against the handler's result, then finalizes the response via
/// <see cref="IBenzeneResponseAdapter{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public class ResponseHandlerContainer<TContext> : IResponseHandlerContainer<TContext>
    where TContext : class
{
    private readonly IResponseHandler<TContext>[] _responseHandlers;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseHandlerContainer{TContext}"/> class.
    /// </summary>
    /// <param name="responseAdapter">Finalizes the response once every response handler has run.</param>
    /// <param name="responseHandlers">Every registered response handler to run, in order.</param>
    public ResponseHandlerContainer(IBenzeneResponseAdapter<TContext> responseAdapter, IEnumerable<IResponseHandler<TContext>> responseHandlers)
    {
        _responseAdapter = responseAdapter;
        _responseHandlers = responseHandlers.ToArray();
    }

    /// <inheritdoc />
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
