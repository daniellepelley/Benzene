using System.Diagnostics;
using Benzene.Abstractions.DI;
using Benzene.Core.DI;
using Benzene.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

public sealed class MicrosoftServiceResolverAdapter : IServiceResolver
{
    private readonly IServiceProvider _serviceProvider;

    public MicrosoftServiceResolverAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
            throw new BenzeneException($"Unable to resolve type {typeof(T).FullName}, {str}, Exception: {ex}");
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

    public void Dispose()
    {
    }
}
