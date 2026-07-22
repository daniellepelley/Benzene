using Benzene.Aws.Lambda.Core;
using Benzene.Microsoft.Dependencies;
using Benzene.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Benzene.Examples.AwsMesh.Shared;

/// <summary>
/// Builds and owns the OpenTelemetry providers for a Lambda-hosted mesh service, and force-flushes them
/// at the end of every invocation (via <see cref="TracingLambdaHost{TStartUp}"/>).
/// <para>
/// This deliberately builds the providers itself instead of the usual
/// <c>services.AddOpenTelemetry()</c> (<c>OpenTelemetry.Extensions.Hosting</c>), because that path does
/// not work in a bare AWS Lambda host — for two distinct reasons this fixes:
/// </para>
/// <list type="number">
/// <item><description>
/// <b>The providers are never built.</b> <c>AddOpenTelemetry()</c> defers constructing the
/// <c>TracerProvider</c>/<c>MeterProvider</c> to a <c>TelemetryHostedService</c> that only runs under a
/// .NET Generic Host. <see cref="AwsLambdaHost{TStartUp}"/> has no <c>IHost</c>, so that hosted service
/// never starts, the providers are never constructed, the <c>"Benzene"</c> <c>ActivitySource</c> gets no
/// listener, and <c>ActivitySource.StartActivity</c> returns <c>null</c> — so <b>no per-middleware spans
/// are ever recorded</b>, no matter how the pipeline is wired. Building the providers eagerly here
/// (<c>Sdk.Create*ProviderBuilder().Build()</c>) attaches the listener at startup instead.
/// </description></item>
/// <item><description>
/// <b>Batched spans are lost on freeze.</b> The OTLP exporter buffers spans on a background thread that
/// stops when the Lambda execution environment freezes between invocations, so the just-finished
/// invocation's spans can arrive late (on the next invocation) or be dropped entirely on scale-in.
/// <see cref="ForceFlushAll"/> — called from <see cref="TracingLambdaHost{TStartUp}"/> after each
/// invocation — drains the batch synchronously while the process is still running.
/// </description></item>
/// </list>
/// <para>
/// The OTLP exporter is only attached when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set (e.g. pointing at
/// the CloudWatch Application Signals / ADOT collector); without it the providers still record spans but
/// export nowhere, so there are no connection-refused errors when running without a collector.
/// </para>
/// </summary>
public static class LambdaTelemetry
{
    private static TracerProvider? _tracerProvider;
    private static MeterProvider? _meterProvider;

    /// <summary>
    /// Builds the trace + metric providers for <paramref name="serviceName"/> (attaching the OTLP
    /// exporter when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set), and registers the built instances so the
    /// container disposes them on shutdown and anything resolving <c>TracerProvider</c>/<c>MeterProvider</c>
    /// gets the live one.
    /// </summary>
    public static void Configure(IServiceCollection services, string serviceName)
    {
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var resource = ResourceBuilder.CreateDefault().AddService(serviceName);

        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resource)
            .SetSampler(new AlwaysOnSampler())
            .AddBenzeneInstrumentation();
        if (!string.IsNullOrEmpty(otlpEndpoint)) tracerBuilder.AddOtlpExporter();
        _tracerProvider = tracerBuilder.Build();

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resource)
            .AddBenzeneInstrumentation();
        // Export metrics with DELTA temporality (each export is that interval's delta, not a running
        // cumulative total). The CloudWatch EMF exporter downstream turns each delta into a metric
        // datapoint, so a CloudWatch Sum over a window equals the request count the mesh usage feed reads;
        // cumulative temporality would make that Sum meaningless. Harmless when no collector is attached.
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            meterBuilder.AddOtlpExporter((_, metrics) =>
                metrics.TemporalityPreference = MetricReaderTemporalityPreference.Delta);
        }
        _meterProvider = meterBuilder.Build();

        services.AddSingleton(_tracerProvider);
        services.AddSingleton(_meterProvider);
    }

    /// <summary>
    /// Force-flushes the batched exporters so the just-finished invocation's spans/metrics are exported
    /// before the Lambda environment freezes. Bounded (2s) so a stuck collector can't hang the invocation.
    /// </summary>
    public static void ForceFlushAll()
    {
        _tracerProvider?.ForceFlush(2000);
        _meterProvider?.ForceFlush(2000);
    }
}

/// <summary>
/// An <see cref="AwsLambdaHost{TStartUp}"/> that force-flushes the OpenTelemetry providers (see
/// <see cref="LambdaTelemetry"/>) after every invocation, so batched spans/metrics are exported before
/// the Lambda execution environment freezes.
/// </summary>
public abstract class TracingLambdaHost<TStartUp> : AwsLambdaHost<TStartUp>
    where TStartUp : BenzeneStartUp, new()
{
    /// <inheritdoc />
    protected override Task OnInvocationCompleteAsync()
    {
        LambdaTelemetry.ForceFlushAll();
        return Task.CompletedTask;
    }
}
