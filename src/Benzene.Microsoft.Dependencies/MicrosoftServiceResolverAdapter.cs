using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public sealed class MicrosoftServiceResolverAdapter : IServiceResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope? _scope;
    private MicrosoftServiceResolverFactory? _serviceResolverFactory;

    // Return a stable factory instance for the adapter's lifetime rather than allocating a fresh one
    // on every IServiceResolverFactory resolution - matching the Autofac adapter, which returns its
    // stored factory. Both GetService and TryGetService go through here so they can't diverge.
    private MicrosoftServiceResolverFactory ResolverFactory
        => _serviceResolverFactory ??= new MicrosoftServiceResolverFactory(_serviceProvider);

    public MicrosoftServiceResolverAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Wraps a scope created via <see cref="MicrosoftServiceResolverFactory.CreateScope"/>. Unlike
    /// the <see cref="IServiceProvider"/> constructor - where the provider's lifetime is owned by
    /// whoever passed it in (e.g. Microsoft.Extensions.DependencyInjection's own scope management,
    /// or ASP.NET Core's per-request provider) - this adapter owns <paramref name="scope"/> and
    /// disposes it, so the scoped services resolved through it are actually released.
    /// </summary>
    public MicrosoftServiceResolverAdapter(IServiceScope scope)
    {
        _scope = scope;
        _serviceProvider = scope.ServiceProvider;
    }

    public T GetService<T>() where T : class
    {
        if (typeof(T) == typeof(IServiceResolver))
        {
            return this as T ?? throw new InvalidOperationException();
        }

        if (typeof(T) == typeof(IServiceResolverFactory))
        {
            return ResolverFactory as T ?? throw new InvalidOperationException();
        }

        try
        {
            return _serviceProvider.GetRequiredService<T>();
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
            return ResolverFactory as T;
        }

        try
        {
            // GetService (not GetRequiredService) returns null for an UNREGISTERED service without
            // throwing - so the common "optional feature is off" check (run per request/per event
            // across the framework) no longer raises and catches a first-chance exception every time.
            // The try/catch now only guards the rare registered-but-throws-on-construction case,
            // preserving the previous "TryGetService never propagates" behavior.
            return _serviceProvider.GetService<T>();
        }
        catch
        {
            return default;
        }
    }

    public IEnumerable<T> GetServices<T>() where T : class
    {
        return _serviceProvider.GetServices<T>();
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}