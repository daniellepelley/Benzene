using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public class MicrosoftServiceResolverFactory : IServiceResolverFactory, IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _ownsServiceProvider;

    public MicrosoftServiceResolverFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // An externally-supplied provider belongs to whoever built it - do not dispose it here.
        _ownsServiceProvider = false;
    }

    public MicrosoftServiceResolverFactory(IServiceCollection container)
    {
        _serviceProvider = container.BuildServiceProvider();
        // We built this provider, so we own its disposal.
        _ownsServiceProvider = true;
    }

    public void Dispose()
    {
        // Only dispose a provider we built ourselves. Disposing runs the container's IDisposable
        // singletons' cleanup (e.g. MeshAnnouncer's announce loop, HttpMeshTraceExporter's tail-batch
        // flush), which previously leaked until process exit on the Lambda / self-host-from-
        // IServiceCollection paths - there this Dispose() was a no-op and nothing else owned the provider.
        if (_ownsServiceProvider)
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_ownsServiceProvider)
        {
            return;
        }

        // Prefer async disposal: a singleton registered only for IAsyncDisposable (not IDisposable)
        // would throw if disposed synchronously. Microsoft's ServiceProvider implements both.
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public IServiceResolver CreateScope()
    {
        return new MicrosoftServiceResolverAdapter(_serviceProvider.CreateScope());
    }
}
