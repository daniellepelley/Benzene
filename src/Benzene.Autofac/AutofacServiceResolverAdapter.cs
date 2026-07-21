using System.Diagnostics;
using Autofac;
using Benzene.Abstractions.DI;
using Benzene.Core.Exceptions;

namespace Benzene.Autofac;

public class AutofacServiceResolverAdapter : IServiceResolver
{
    private readonly IComponentContext _container;
    private readonly ILifetimeScope? _scope;
    private readonly IServiceResolverFactory _serviceResolverFactory;

    /// <summary>
    /// Wraps a scope created via <see cref="AutofacServiceResolverFactory.CreateScope"/>. Unlike
    /// the <see cref="IComponentContext"/> constructors below - where the context's lifetime is
    /// owned by Autofac's own scope management (e.g. a registration lambda resolving its ambient
    /// <see cref="IComponentContext"/>) - this adapter owns <paramref name="scope"/> and disposes
    /// it, so the scoped services resolved through it are actually released.
    /// </summary>
    public AutofacServiceResolverAdapter(ILifetimeScope scope, IServiceResolverFactory serviceResolverFactory)
        : this((IComponentContext)scope, serviceResolverFactory)
    {
        _scope = scope;
    }

    public AutofacServiceResolverAdapter(IComponentContext container, IServiceResolverFactory serviceResolverFactory)
        :this(container)
    {
        _serviceResolverFactory = serviceResolverFactory;
    }

    public AutofacServiceResolverAdapter(IComponentContext container)
    {
        _container = container;
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
            return _container.Resolve<T>();
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
            // ResolveOptional returns null for an UNREGISTERED service without throwing, so the common
            // "optional feature is off" check no longer raises + catches a first-chance BenzeneException
            // every time (GetService<T> threw). The try/catch only guards the rare
            // registered-but-throws-on-construction case, preserving the previous never-propagate behavior.
            return _container.ResolveOptional<T>();
        }
        catch
        {
            return default;
        }
    }

    public IEnumerable<T> GetServices<T>() where T : class
    {
        return _container.Resolve<IEnumerable<T>>();
    }

    public void Dispose()
    {
        // Don't null out _container: the field is non-nullable (assigning null was a nullability lie),
        // and disposing the owned _scope already invalidates it - a post-dispose resolve then surfaces
        // Autofac's ObjectDisposedException rather than an NRE.
        _scope?.Dispose();
    }
}