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

    public ResponseHandlerContainer(IEnumerable<IResponseHandler<TContext>> responseHandlers)
    {
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
    }
}
