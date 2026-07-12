using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Exceptions;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Core;

/// <summary>
/// Default implementation of <see cref="IAzureFunctionApp"/>. Dispatches a request to whichever of its
/// constructed entry point applications matches the request (and response, where applicable) type.
/// </summary>
public class AzureFunctionApp : IAzureFunctionApp
{
    private readonly IEntryPointMiddlewareApplication[] _apps;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureFunctionApp"/> class, constructing every
    /// registered entry point application for the current invocation scope.
    /// </summary>
    /// <param name="appBuilders">The factories for each entry point application registered with the builder.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to construct each entry point application.</param>
    public AzureFunctionApp(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>[] appBuilders, IServiceResolverFactory serviceResolverFactory)
    {
        _apps = appBuilders.Select(x => x(serviceResolverFactory)).ToArray();
    }

    /// <summary>
    /// Handles a request that expects a response, dispatching to the registered entry point application
    /// whose request/response types match.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <returns>A task that resolves to the response produced by the matching entry point application.</returns>
    /// <exception cref="BenzeneException">Thrown when no registered entry point application matches <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>.</exception>
    public Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request)
    {
        foreach (var entryPointMiddleApplication in _apps)
        {
            if (entryPointMiddleApplication is EntryPointMiddlewareApplication<TRequest, TResponse> app)
            {
                return app.SendAsync(request);
            }
        }

        throw new BenzeneException("Cannot handle this kind of request");
    }

    /// <summary>
    /// Handles a fire-and-forget request, dispatching to the registered entry point application whose
    /// request type matches.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to handle.</param>
    /// <returns>A task that completes when the matching entry point application has finished handling the request.</returns>
    /// <exception cref="BenzeneException">Thrown when no registered entry point application matches <typeparamref name="TRequest"/>.</exception>
    public Task HandleAsync<TRequest>(TRequest request)
    {
        foreach (var entryPointMiddleApplication in _apps)
        {
            if (entryPointMiddleApplication is IEntryPointMiddlewareApplication<TRequest> app)
            {
                return app.SendAsync(request);
            }
        }

        throw new BenzeneException("Cannot handle this kind of request");
    }
}
