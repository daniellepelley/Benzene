using System.Diagnostics;
using Autofac;
using Benzene.Abstractions.DI;
using Benzene.Core.Exceptions;

namespace Benzene.Autofac;

public class AutofacServiceResolverAdapter : IServiceResolver
{
    private IComponentContext _container;
    private readonly IServiceResolverFactory _serviceResolverFactory;

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
            var str = RegistrationErrorHandler.CheckException(ex);
            Debug.WriteLine($"Unable to resolve type {typeof(T).FullName}, {str}, Exception: {ex}");
            throw new BenzeneException($"Unable to resolve type {typeof(T).FullName}, {str}, Exception: {ex}");
        }
    }

    public T? TryGetService<T>() where T : class
    {
        try
        {
            return GetService<T>();
        }
        catch
        {
            return default;
        }
    }

    public void Dispose()
    {
        _container = null;
    }
}