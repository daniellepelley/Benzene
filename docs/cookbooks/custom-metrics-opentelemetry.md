# Custom Metrics with OpenTelemetry

Emit Benzene's built-in message metrics — and your own business metrics — through OpenTelemetry to
Prometheus, an OTLP collector, or any metrics backend.

## Problem Statement

You want to:
- Track how many messages each topic processes and how long they take, without hand-instrumenting
  handlers
- Export those metrics to your monitoring backend
- Add your own domain metrics (orders placed, payments failed) alongside them

## Prerequisites

- A Benzene service (any transport)
- A metrics backend (Prometheus, an OTLP collector, …)
- `Benzene.Diagnostics` and `Benzene.OpenTelemetry`

```bash
dotnet add package Benzene.Diagnostics --prerelease
dotnet add package Benzene.OpenTelemetry --prerelease
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

## Benzene's built-in metrics

Adding [`UseBenzeneMetrics()`](../reference/middleware#usebenzenemetrics) to a pipeline records, for
the wrapped stage:

| Instrument | Type | Tags |
|---|---|---|
| `benzene.messages.processed` | counter | `topic`, `transport`, `result` |
| `benzene.message.duration` | histogram (ms) | `topic`, `transport`, `result` |

`result` is `success`/`failure` when the context carries a message result. These are once-per-message,
so add `UseBenzeneMetrics()` around the stage you want measured.

## Step-by-Step Implementation

### 1. Enable diagnostics and measure the pipeline

```csharp
services.UsingBenzene(x => x
    .AddDiagnostics()
    .AddMessageHandlers(typeof(MyHandler).Assembly));
```

```csharp
app.UseApiGateway(api => api
    .UseBenzeneMetrics()   // records processed count + duration for what follows
    .UseMessageHandlers(router => router.UseFluentValidation()));
```

### 2. Export via OpenTelemetry with Benzene instrumentation

`AddBenzeneInstrumentation()` on the `MeterProviderBuilder` registers Benzene's meter so its
instruments are exported:

```csharp
using Benzene.OpenTelemetry;
using OpenTelemetry.Metrics;

services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddBenzeneInstrumentation()   // export benzene.messages.processed / benzene.message.duration
        .AddOtlpExporter());
```

### 3. Add your own business metrics

Benzene's metrics are standard `System.Diagnostics.Metrics`, so add your own with a `Meter` and
export it the same way:

```csharp
public class OrderMetrics
{
    public static readonly Meter Meter = new("MyApp.Orders");
    public static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>("orders.placed");
}

// In a handler:
OrderMetrics.OrdersPlaced.Add(1, new KeyValuePair<string, object?>("channel", request.Channel));
```

Register your meter alongside Benzene's:

```csharp
services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddBenzeneInstrumentation()
        .AddMeter("MyApp.Orders")      // your custom meter
        .AddOtlpExporter());
```

## Testing

Attach an in-memory metric reader to a test `MeterProvider` (or scrape a local Prometheus/OTLP
collector) and assert the expected instruments and tag values are emitted after sending a message
through the [test host](../testing-benzene).

## Troubleshooting

### `benzene.*` metrics are missing

**Problem**: Your custom metrics show up but Benzene's don't.

**Solution**: `UseBenzeneMetrics()` must be in the pipeline (it's not automatic), `AddDiagnostics()`
must be registered, and `AddBenzeneInstrumentation()` must be on the `WithMetrics(...)` builder.

### Custom metrics not exported

**Problem**: Your `Meter` isn't collected.

**Solution**: Add its name via `.AddMeter("MyApp.Orders")` on the metrics builder — OpenTelemetry
only exports meters it's told about.

## Variations

### Prometheus

Swap `AddOtlpExporter()` for the Prometheus exporter to scrape metrics directly.

### Tracing too

Pair this with [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) — the
same `AddBenzeneInstrumentation()` exists for traces.

## Further Reading

- [Monitoring & Diagnostics](../monitoring#opentelemetry) - metrics in context
- [Middleware Reference](../reference/middleware#usebenzenemetrics) - `UseBenzeneMetrics`
- [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) - the tracing counterpart
- [.NET metrics](https://learn.microsoft.com/dotnet/core/diagnostics/metrics) - `System.Diagnostics.Metrics`
