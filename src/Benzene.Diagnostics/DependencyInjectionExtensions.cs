using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Diagnostics.Timers;

namespace Benzene.Diagnostics
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ActivityMiddlewareWrapper"/> so every middleware in every pipeline is
        /// wrapped in its own <see cref="System.Diagnostics.Activity"/> span (named after the middleware,
        /// tagged <c>benzene.transport</c>/<c>benzene.topic</c>/<c>benzene.version</c>/<c>benzene.handler</c>
        /// where resolvable). This is the focused, self-documenting opt-in for per-middleware tracing —
        /// use it when you want the span-per-middleware behaviour without the debug wrapper, timer
        /// factory, and correlation registrations that the broader <see cref="AddDiagnostics"/> also
        /// brings in. Export the spans with <c>Benzene.OpenTelemetry</c>'s <c>AddBenzeneInstrumentation</c>.
        /// Idempotent and composes safely with <see cref="AddDiagnostics"/> — the same registration
        /// guard means calling both never double-wraps a middleware.
        /// </summary>
        public static IBenzeneServiceContainer AddActivityPerMiddleware(this IBenzeneServiceContainer services)
        {
            if (!services.IsTypeRegistered<ActivityMiddlewareWrapper>())
            {
                services.AddSingleton<ActivityMiddlewareWrapper>();
                services.AddSingleton<IMiddlewareWrapper, ActivityMiddlewareWrapper>();
            }

            return services;
        }

        public static IBenzeneServiceContainer AddDiagnostics(this IBenzeneServiceContainer services)
        {
            if (!services.IsTypeRegistered<DebugMiddlewareWrapper>())
            {
                services.AddSingleton<DebugMiddlewareWrapper>();
                services.AddSingleton<IMiddlewareWrapper, DebugMiddlewareWrapper>();
            }

            services.AddActivityPerMiddleware();

            if (!services.IsTypeRegistered<ActivityProcessTimerFactory>())
            {
                services.AddScoped<IProcessTimerFactory, ActivityProcessTimerFactory>();
            }

            return services;
        }
    }
}
