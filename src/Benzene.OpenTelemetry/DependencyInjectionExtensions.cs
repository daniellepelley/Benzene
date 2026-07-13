using Benzene.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Benzene.OpenTelemetry
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Registers Benzene's <see cref="BenzeneDiagnostics.ActivitySource"/> as a source this
        /// <see cref="TracerProviderBuilder"/> exports, so every <c>Activity</c> span produced by
        /// <c>AddDiagnostics()</c> is exported to whatever backend this provider is configured with.
        /// </summary>
        public static TracerProviderBuilder AddBenzeneInstrumentation(this TracerProviderBuilder builder)
            => builder.AddSource(BenzeneDiagnostics.SourceName);

        /// <summary>
        /// Registers Benzene's <see cref="BenzeneDiagnostics.Meter"/> as a meter this
        /// <see cref="MeterProviderBuilder"/> exports, so instruments like
        /// <see cref="BenzeneDiagnostics.MessagesProcessed"/>/<see cref="BenzeneDiagnostics.MessageDuration"/>
        /// are exported to whatever backend this provider is configured with.
        /// </summary>
        public static MeterProviderBuilder AddBenzeneInstrumentation(this MeterProviderBuilder builder)
            => builder.AddMeter(BenzeneDiagnostics.SourceName);
    }
}
