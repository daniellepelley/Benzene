namespace Benzene.Clients.Aws.Sqs;

public interface IClientRequestMapper<TRequest>
{
    TRequest CreateRequest<TRequestIn>(IBenzeneClientRequest<TRequestIn> request);
}