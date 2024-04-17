namespace Benzene.Abstractions.DI;

public interface IBenzeneServiceContainer
{
    bool IsTypeRegistered<TService>();
    bool IsTypeRegistered(Type type);
    
    IBenzeneServiceContainer AddScoped<TImplementation>()
        where TImplementation : class;
    IBenzeneServiceContainer AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;
    IBenzeneServiceContainer AddScoped(Type type);
    IBenzeneServiceContainer AddScoped(Type serviceType, Type implementationType);
    IBenzeneServiceContainer AddScoped<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation: class;
    
    IBenzeneServiceContainer AddSingleton<TImplementation>()
        where TImplementation : class;
    IBenzeneServiceContainer AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;
    IBenzeneServiceContainer AddSingleton(Type type);
    IBenzeneServiceContainer AddSingleton(Type serviceType, Type implementationType);
    IBenzeneServiceContainer AddSingleton<TImplementation>(TImplementation implementation)
        where TImplementation : class;
    IBenzeneServiceContainer AddSingleton<TImplementation>(Func<IServiceResolver, TImplementation> func)
        where TImplementation : class;

    IBenzeneServiceContainer AddServiceResolver();
}