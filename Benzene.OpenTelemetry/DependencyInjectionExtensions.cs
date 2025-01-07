using Benzene.Abstractions.DI;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Timers;

namespace Benzene.OpenTelemetry
{
    public static class DependencyInjectionExtensions
    {
        public static IBenzeneServiceContainer AddOpenTelemetry(this IBenzeneServiceContainer services)
        {
            services.AddDiagnostics();
            services.AddScoped<IProcessTimerFactory, OpenTelemetryProcessTimerFactory>();
            return services;
        }
    }
}
