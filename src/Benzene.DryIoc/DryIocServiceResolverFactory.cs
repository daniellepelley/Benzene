using Benzene.Abstractions.DI;
using DryIoc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.DryIoc;

/// <summary>
/// Opens a DryIoc scope per Benzene scope. Seeds <see cref="NullLoggerFactory"/>/open-generic
/// <see cref="Logger{T}"/> fallbacks (only when the consumer hasn't registered their own) so
/// <c>ILogger&lt;T&gt;</c>/<c>ILoggerFactory</c> always resolve, matching the Autofac adapter.
/// </summary>
public class DryIocServiceResolverFactory : IServiceResolverFactory
{
    private readonly IContainer _container;

    public DryIocServiceResolverFactory(IContainer container)
    {
        // Register the logging fallbacks only if the consumer didn't (user registrations win). Done at
        // factory-creation time - after all registrations - so IsRegistered reflects the final set.
        if (!container.IsRegistered(typeof(ILoggerFactory)))
        {
            container.RegisterInstance<ILoggerFactory>(NullLoggerFactory.Instance);
        }

        if (!container.IsRegistered(typeof(ILogger<>)))
        {
            container.Register(typeof(ILogger<>), typeof(Logger<>), Reuse.Singleton);
        }

        _container = container;
    }

    public void Dispose()
    {
        // The container is supplied by the caller (via UsingBenzene), so its lifetime is theirs - don't
        // dispose it here. Per-scope disposal is owned by the resolver adapter CreateScope hands out.
    }

    public IServiceResolver CreateScope()
    {
        return new DryIocServiceResolverAdapter(_container.OpenScope(), this);
    }
}
