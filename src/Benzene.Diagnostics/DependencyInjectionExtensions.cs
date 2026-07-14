using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Diagnostics.Timers;

namespace Benzene.Diagnostics
{
    public static class DependencyInjectionExtensions
    {
        public static IBenzeneServiceContainer AddDiagnostics(this IBenzeneServiceContainer services)
        {
            if (!services.IsTypeRegistered<DebugMiddlewareWrapper>())
            {
                services.AddSingleton<DebugMiddlewareWrapper>();
                services.AddSingleton<IMiddlewareWrapper, DebugMiddlewareWrapper>();
            }

            if (!services.IsTypeRegistered<ActivityMiddlewareWrapper>())
            {
                services.AddSingleton<ActivityMiddlewareWrapper>();
                services.AddSingleton<IMiddlewareWrapper, ActivityMiddlewareWrapper>();
            }

            if (!services.IsTypeRegistered<ActivityProcessTimerFactory>())
            {
                services.AddScoped<IProcessTimerFactory, ActivityProcessTimerFactory>();
            }

            return services;
        }
    }
}
