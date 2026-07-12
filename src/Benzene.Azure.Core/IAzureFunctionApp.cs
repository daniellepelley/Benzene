namespace Benzene.Azure.Core;

/// <summary>
/// Represents a built Azure Function app that dispatches requests to the matching registered entry
/// point application, based on the request type.
/// </summary>
public interface IAzureFunctionApp
{
    /// <summary>
    /// Handles a request that expects a response, dispatching to the registered entry point application
    /// whose request/response types match.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <returns>A task that resolves to the response produced by the matching entry point application.</returns>
    Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request);

    /// <summary>
    /// Handles a fire-and-forget request, dispatching to the registered entry point application whose
    /// request type matches.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <returns>A task that completes when the matching entry point application has finished handling the request.</returns>
    Task HandleAsync<TRequest>(TRequest request);
}
