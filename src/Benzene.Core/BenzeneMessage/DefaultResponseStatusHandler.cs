using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Core.BenzeneMessage;

public class DefaultResponseStatusHandler<TContext> : ISyncResponseHandler<TContext> where TContext : class, IHasMessageResult
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public DefaultResponseStatusHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }
    
    public void HandleAsync(TContext context)
    {
        _benzeneResponseAdapter.SetStatusCode(context, context.MessageResult.Status);
    }
}
