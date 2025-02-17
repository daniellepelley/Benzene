using Benzene.Abstractions.DI;

namespace Benzene.Core.Middleware;

public class NullBenzeneServiceContainer : IBenzeneServiceContainer
{
    public bool IsTypeRegistered<TService>()
    {
        return true;
    }

    public bool IsTypeRegistered(Type type)
    {
        return true;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>() where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddScoped(Type type)
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>() where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddTransient(Type type)
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddTransient(Type serviceType, Type implementationType)
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>() where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        throw new NotImplementedException();
    }

    public IServiceResolverFactory CreateServiceResolverFactory()
    {
        return new NullServiceResolverFactory();
    }

    public IBenzeneServiceContainer AddServiceResolver()
    {
        return this;
    }
}