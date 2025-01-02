using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.Common;

public interface IClientRequestMapper<TRequest>
{
    TRequest CreateRequest<TRequestIn>(IBenzeneClientRequest<TRequestIn> request);
}