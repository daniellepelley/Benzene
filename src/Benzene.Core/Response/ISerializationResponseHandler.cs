using Benzene.Abstractions.Results;

namespace Benzene.Core.Response;

public interface ISerializationResponseHandler<TContext> where TContext : class, IHasMessageResult
{
    void HandleAsync(TContext context, IBodySerializer bodySerializer);
}