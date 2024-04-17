namespace Benzene.Azure.Core;

public interface IAzureApp
{
    Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request);
    Task HandleAsync<TRequest>(TRequest request);
}
