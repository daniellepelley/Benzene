using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Response;


public interface IResponseHandler<TContext> where TContext : class, IHasMessageResult
{ }
