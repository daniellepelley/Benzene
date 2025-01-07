using Benzene.Abstractions.Middleware.BenzeneClient;

namespace Benzene.Clients.Common;

public interface IClientRequestMapper<TRequest>
{
    TRequest CreateRequest<TRequestIn>(IBenzeneClientRequest<TRequestIn> request);
}