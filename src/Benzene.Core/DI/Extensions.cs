using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Core.Logging;

namespace Benzene.Core.DI;

public static class Extensions
{
    
    public static IBenzeneServiceContainer AddDefaultBenzeneLogging(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IBenzeneLogger, BenzeneLogger>();
        services.TryAddScoped<IBenzeneLogContext, NullBenzeneLogContext>();
        services.AddServiceResolver();
        return services;
    }
}
