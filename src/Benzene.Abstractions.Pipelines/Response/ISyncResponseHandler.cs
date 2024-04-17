using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Response;

public interface ISyncResponseHandler<TContext> : IResponseHandler<TContext> where TContext : class, IHasMessageResult
{
    void HandleAsync(TContext context);
}
