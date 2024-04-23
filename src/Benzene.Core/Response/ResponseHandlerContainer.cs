using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Response;

public class ResponseHandlerContainer<TContext> : IResponseHandlerContainer<TContext>
    where TContext : class, IHasMessageResult
{
    private readonly IResponseHandler<TContext>[] _responseHandlers;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    public ResponseHandlerContainer(IBenzeneResponseAdapter<TContext> responseAdapter, IEnumerable<IResponseHandler<TContext>> responseHandlers)
    {
        _responseAdapter = responseAdapter;
        _responseHandlers = responseHandlers.ToArray();
    }

    public async Task HandleAsync(TContext context)
    {
        foreach (var responseHandler in _responseHandlers)
        {
            switch (responseHandler)
            {
                case ISyncResponseHandler<TContext> syncResponseHandler:
                    syncResponseHandler.HandleAsync(context);
                    break;
                case IAsyncResponseHandler<TContext> asyncResponseHandler:
                    await asyncResponseHandler.HandleAsync(context);
                    break;
            }
        }

        await _responseAdapter.FinalizeAsync(context);
    }
}
