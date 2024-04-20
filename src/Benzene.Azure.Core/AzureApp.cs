using Benzene.Abstractions.DI;
using Benzene.Core.Exceptions;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Core;

public class AzureApp : IAzureApp
{
    private readonly IEntryPointMiddlewareApplication[] _apps;

    public AzureApp(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication>[] appBuilders, IServiceResolverFactory serviceResolverFactory)
    {
        _apps = appBuilders.Select(x => x(serviceResolverFactory)).ToArray();
    }

    public Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request)
    {
        foreach (var entryPointMiddleApplication in _apps)
        {
            if (entryPointMiddleApplication is EntryPointMiddlewareApplication<TRequest, TResponse> app)
            {
                return app.HandleAsync(request);
            }
        }

        throw new BenzeneException("Cannot handle this kind of request");
    }
    
    public Task HandleAsync<TRequest>(TRequest request)
    {
        foreach (var entryPointMiddleApplication in _apps)
        {
            if (entryPointMiddleApplication is IEntryPointMiddlewareApplication<TRequest> app)
            {
                return app.HandleAsync(request);
            }
        }

        throw new BenzeneException("Cannot handle this kind of request");
    }
}
