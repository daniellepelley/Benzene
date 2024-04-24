namespace Benzene.Azure.Core;

public interface IAzureFunctionApp
{
    Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request);
    Task HandleAsync<TRequest>(TRequest request);
}
