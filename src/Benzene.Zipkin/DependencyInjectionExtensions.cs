using Benzene.Abstractions.DI;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Timers;

namespace Benzene.Zipkin
{
    public static class DependencyInjectionExtensions
    {
        public static IBenzeneServiceContainer AddZipkin(this IBenzeneServiceContainer services)
        {
            services.AddDiagnostics();
            services.AddScoped<IProcessTimerFactory, ZipkinProcessTimerFactory>();
            return services;
        }
    }
}
