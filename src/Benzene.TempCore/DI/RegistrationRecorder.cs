using Benzene.Abstractions.DI;

namespace Benzene.Core.DI;

public class RegistrationRecorder : IBenzeneServiceContainer
{
    private readonly List<Type> _types = new();

    public Type[] GetTypes()
    {
        return _types.ToArray();
    }

    public bool IsTypeRegistered<TService>()
    {
        return false;
    }

    public bool IsTypeRegistered(Type type)
    {
        return false;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>() where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _types.Add(typeof(TService));
        return this;
    }

    public IBenzeneServiceContainer AddScoped(Type type)
    {
        _types.Add(type);
        return this;
    }

    public IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType)
    {
        _types.Add(serviceType);
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>() where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _types.Add(typeof(TService));
        return this;
    }

    public IBenzeneServiceContainer AddTransient(Type type)
    {
        _types.Add(type);
        return this;
    }

    public IBenzeneServiceContainer AddTransient(Type serviceType, Type implementationType)
    {
        _types.Add(serviceType);
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddTransient<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>() where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        _types.Add(typeof(TService));
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type type)
    {
        _types.Add(type);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType)
    {
        _types.Add(serviceType);
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func) where TImplementation : class
    {
        _types.Add(typeof(TImplementation));
        return this;
    }

    public IServiceResolverFactory CreateServiceResolverFactory()
    {
        return null;
    }

    public IBenzeneServiceContainer AddServiceResolver()
    {
        return this;
    }
}
