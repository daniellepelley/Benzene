using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Response;

public interface IAsyncResponseHandler<TContext> : IResponseHandler<TContext> where TContext : class, IHasMessageResult
{
    Task HandleAsync(TContext context);
}
