using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Http;

public class HttpStatusCodeResponseHandler<TContext> : ISyncResponseHandler<TContext> where TContext : class, IHasMessageResult 
{
    private readonly IHttpStatusCodeMapper _httpStatusCodeMapper;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public HttpStatusCodeResponseHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IHttpStatusCodeMapper httpStatusCodeMapper)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _httpStatusCodeMapper = httpStatusCodeMapper;
    }

    public void HandleAsync(TContext context)
    {
        _benzeneResponseAdapter.SetStatusCode(context, _httpStatusCodeMapper.Map(context.MessageResult.Status));
    }
}
