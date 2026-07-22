using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Core.Exceptions;
using DryIoc;

namespace Benzene.DryIoc;

/// <summary>
/// Resolves services from a DryIoc <see cref="IResolverContext"/> (a container or an opened scope).
/// </summary>
public class DryIocServiceResolverAdapter : IServiceResolver
{
    private readonly IResolverContext _resolver;
    private readonly IResolverContext? _ownedScope;
    private readonly IServiceResolverFactory? _serviceResolverFactory;

    /// <summary>
    /// Wraps a scope created via <see cref="DryIocServiceResolverFactory.CreateScope"/>. This adapter
    /// <b>owns</b> <paramref name="scope"/> and disposes it, so the scoped services resolved through it
    /// are actually released.
    /// </summary>
    public DryIocServiceResolverAdapter(IResolverContext scope, IServiceResolverFactory serviceResolverFactory)
    {
        _resolver = scope;
        _ownedScope = scope;
        _serviceResolverFactory = serviceResolverFactory;
    }

    /// <summary>
    /// Wraps an <see cref="IResolverContext"/> whose lifetime DryIoc owns (e.g. the context handed to a
    /// registration delegate) - this adapter does <b>not</b> dispose it.
    /// </summary>
    public DryIocServiceResolverAdapter(IResolverContext resolver)
    {
        _resolver = resolver;
    }

    public T GetService<T>() where T : class
    {
        if (typeof(T) == typeof(IServiceResolver))
        {
            return this as T ?? throw new InvalidOperationException();
        }

        if (typeof(T) == typeof(IServiceResolverFactory))
        {
            return _serviceResolverFactory as T ?? throw new InvalidOperationException();
        }

        try
        {
            return _resolver.Resolve<T>();
        }
        catch (Exception ex)
        {
            // Enrich from the requested type first (always known, so this works on any container) and
            // fall back to scanning the exception; Describe never throws, so it can't mask the real
            // failure, which is preserved as the InnerException either way.
            var hint = RegistrationErrorHandler.Describe(typeof(T), ex);
            Debug.WriteLine($"Unable to resolve type {typeof(T).FullName}{hint}, Exception: {ex}");
            throw new BenzeneException($"Unable to resolve type {typeof(T).FullName}{hint}", ex);
        }
    }

    public T? TryGetService<T>() where T : class
    {
        if (typeof(T) == typeof(IServiceResolver))
        {
            return this as T;
        }

        if (typeof(T) == typeof(IServiceResolverFactory))
        {
            return _serviceResolverFactory as T;
        }

        try
        {
            // IfUnresolved.ReturnDefault returns null for an UNREGISTERED service without throwing, so
            // the common "optional feature is off" check doesn't raise + catch an exception every time.
            // The try/catch only guards the rare registered-but-throws-on-construction case.
            return _resolver.Resolve<T>(IfUnresolved.ReturnDefault);
        }
        catch
        {
            return default;
        }
    }

    public IEnumerable<T> GetServices<T>() where T : class
    {
        return _resolver.Resolve<IEnumerable<T>>();
    }

    public void Dispose()
    {
        _ownedScope?.Dispose();
    }
}
