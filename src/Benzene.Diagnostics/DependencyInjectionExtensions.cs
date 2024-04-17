using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Diagnostics.Timers;

namespace Benzene.Diagnostics
{
    public static class DependencyInjectionExtensions
    {
        public static IBenzeneServiceContainer AddDiagnostics(this IBenzeneServiceContainer services)
        {
            services.AddScoped<IMiddlewareWrapper, DebugMiddlewareWrapper>();
            services.AddScoped<IMiddlewareWrapper, TimerMiddlewareWrapper>();
            services.AddScoped<IProcessTimerFactory, LoggingProcessTimerFactory>();

            return services;
        }
    }
}
