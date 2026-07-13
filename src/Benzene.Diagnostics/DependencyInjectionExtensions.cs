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
                services.AddScoped<DebugMiddlewareWrapper>();
                services.AddScoped<IMiddlewareWrapper, DebugMiddlewareWrapper>();
            }

            if (!services.IsTypeRegistered<ActivityMiddlewareWrapper>())
            {
                services.AddScoped<ActivityMiddlewareWrapper>();
                services.AddScoped<IMiddlewareWrapper, ActivityMiddlewareWrapper>();
            }

            if (!services.IsTypeRegistered<ActivityProcessTimerFactory>())
            {
                services.AddScoped<IProcessTimerFactory, ActivityProcessTimerFactory>();
            }

            return services;
        }
    }
}
