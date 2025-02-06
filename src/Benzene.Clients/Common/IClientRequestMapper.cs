using Benzene.Abstractions.Messages.BenzeneClient;

namespace Benzene.Clients.Common;

public interface IClientRequestMapper<TRequest>
{
    TRequest CreateRequest<TRequestIn>(IBenzeneClientRequest<TRequestIn> request);
}