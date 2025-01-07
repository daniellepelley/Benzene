using Benzene.Abstractions.DI;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Timers;

namespace Benzene.Datadog
{
    public static class DependencyInjectionExtensions
    {
        public static IBenzeneServiceContainer AddDatadog(this IBenzeneServiceContainer services)
        {
            services.AddDiagnostics();
            services.AddScoped<IProcessTimerFactory, DatadogProcessTimerFactory>();
            return services;
        }
    }
}
