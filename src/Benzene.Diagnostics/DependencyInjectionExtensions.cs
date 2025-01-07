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

            if (!services.IsTypeRegistered<TimerMiddlewareWrapper>())
            {
                services.AddScoped<TimerMiddlewareWrapper>();
                services.AddScoped<IMiddlewareWrapper, TimerMiddlewareWrapper>();
            }

            services.AddScoped<IProcessTimerFactory, LoggingProcessTimerFactory>();

            return services;
        }
    }
}
