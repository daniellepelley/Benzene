using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public sealed class MicrosoftServiceResolverAdapter : IServiceResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope? _scope;

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
            return new MicrosoftServiceResolverFactory(_serviceProvider) as T ?? throw new InvalidOperationException();
        }

        try
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        catch (Exception ex)
        {
            var str = RegistrationErrorHandler.CheckException(ex);
            Debug.WriteLine($"Unable to resolve type {typeof(T).FullName}, {str}, Exception: {ex}");
            throw new BenzeneException($"Unable to resolve type {typeof(T).FullName}, {str}", ex);
        }
    }

    public T? TryGetService<T>() where T : class
    {
        if (typeof(T) == typeof(IServiceResolver))
        {
            return this as T ?? throw new InvalidOperationException();
        }

        try
        {
            return _serviceProvider.GetRequiredService<T>();
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