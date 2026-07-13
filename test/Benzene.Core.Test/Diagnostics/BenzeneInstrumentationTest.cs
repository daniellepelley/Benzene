using Benzene.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class BenzeneInstrumentationTest
{
    [Fact]
    public void AddBenzeneInstrumentation_WiresTracerProviderWithoutThrowing()
    {
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddBenzeneInstrumentation()
            .Build();

        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void AddBenzeneInstrumentation_WiresMeterProviderWithoutThrowing()
    {
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddBenzeneInstrumentation()
            .Build();

        Assert.NotNull(meterProvider);
    }
}
