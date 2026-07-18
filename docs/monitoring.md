# Monitoring & Diagnostics

Benzene includes built-in support for common monitoring and diagnostic patterns, ensuring your services are observable and easy to debug. Detailed information on core middleware can be found in the [Common Middleware](common-middleware.md) section.

## Correlation IDs

> Cross-service correlation is handled by automatic [W3C trace context](#w3c-trace-context)
> propagation.

Correlation IDs allow you to trace a single request as it moves through various components of your
system. In Benzene this rides on the W3C `traceparent`/`tracestate` headers via
`UseW3CTraceContext()` — see [W3C trace context](#w3c-trace-context) below.

A per-invocation `ICorrelationId` (self-generated GUID, settable from your own middleware) remains
available for log-scope enrichment — see [Correlation Ids](correlation-ids.md) and
`.UseLogResult(x => x.WithCorrelationId())` in [Common Middleware](common-middleware.md).

## Tracing

Every middleware in every pipeline is automatically wrapped in a [`System.Diagnostics.Activity`](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing)
span, tagged with `benzene.transport`, `benzene.topic`, `benzene.version`, and `benzene.handler`
where resolvable — no explicit call needed. This is enabled by `AddDiagnostics()`:

```csharp
services.UsingBenzene(x => x.AddDiagnostics());
```

`Activity.StartActivity` is a documented no-op when nothing is listening, so this has no real cost
until you attach a listener or exporter. To export spans to a real tracing backend, see the
[OpenTelemetry](#opentelemetry) section below.

### Named timers

For measuring a specific part of your pipeline by name, `UseTimer(name)` still works and now opens
an `Activity` span under that name:

```csharp
app.UseTimer("my-application");
```

`UseTimer(name)` is a thin wrapper around the `IProcessTimer`/`IProcessTimerFactory` abstraction:
`AddDiagnostics()` registers `ActivityProcessTimerFactory` as the default `IProcessTimerFactory`,
which is what makes `UseTimer(name)` open an `Activity` span rather than doing nothing. `IProcessTimer`
is kept mainly for source-compatibility with existing `UseTimer("name")` call sites that predate
`Activity`-based tracing — new code should prefer `Activity`/`ActivitySource` directly. If no
`IProcessTimerFactory` is registered at all (i.e. `AddDiagnostics()` was never called), `UseTimer(name)`
silently falls back to calling `next()` with no timing. Other `IProcessTimerFactory` implementations
ship in `Benzene.Diagnostics.Timers` if you want different behavior instead of (or alongside) `Activity`
spans — register one explicitly to replace the default:

- `LoggingProcessTimerFactory` - logs a start line and a `"{timer} took {ms}ms"` line (with any tags)
  via `ILogger`, at `Trace` level
- `DebugTimerFactory` - `Debug.WriteLine`-based timing, independent of `Activity`/logging
- `CompositeProcessTimerFactory` - fans a single timer out to multiple `IProcessTimerFactory`
  implementations at once (e.g. `Activity` spans *and* log lines)

For a lower-level hook that measures raw elapsed time without going through `IProcessTimer` at all,
there's also a callback-based overload:

```csharp
app.UseTimer((context, elapsedMilliseconds) =>
{
    // e.g. feed elapsedMilliseconds into your own metrics system
});
```

This wraps `next()` in a `Stopwatch` and invokes `onTimer` once the rest of the pipeline completes
(including on exceptions, since it runs in a `finally`) — unrelated to `Activity`/tracing and always
active regardless of what's registered in DI.

## Logging

Benzene logs through [`Microsoft.Extensions.Logging`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) (`ILogger<T>`). There is no Benzene-specific logger to configure: whatever logging providers your host sets up (console, Serilog, log4net, Application Insights, ...) automatically receive Benzene's framework logs and your handlers' logs alike.

`UsingBenzene(...)` calls `services.AddLogging()` for you, so `ILogger<T>` always resolves — with no providers configured, log calls are simply no-ops. Configure providers the standard .NET way:

```csharp
services.AddLogging(x => x.AddConsole());
// or with Serilog's Microsoft.Extensions.Logging provider:
services.AddLogging(x => x.AddSerilog());
```

Your message handlers just take a logger via constructor injection:

```csharp
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderMessage, OrderDto>
{
    private readonly ILogger<CreateOrderMessageHandler> _logger;

    public CreateOrderMessageHandler(ILogger<CreateOrderMessageHandler> logger)
    {
        _logger = logger;
    }
    // ...
}
```

### Structured log scopes

The pipeline middleware `.UseLogResult(...)` / `.UseLogContext(...)` attach structured properties (correlation ID, topic, transport, AWS request ID, ...) to the logging scope for the duration of the request, using `ILogger.BeginScope`:

```csharp
app.UseLogResult(x => x
    .WithCorrelationId()
    .WithTopic()
    .WithTransport());
```

Scope properties flow to any provider that supports scopes — for the console provider enable `IncludeScopes`; Serilog's provider maps scopes to its `LogContext` automatically.

For a single, portable call that covers `invocationId`/`traceId`/`spanId`/`topic`/`transport`/`handler` on every platform (rather than hand-composing the extensions above), see [`UseBenzeneEnrichment()`](common-middleware.md#usebenzeneenrichment).

### Autofac

The Autofac integration registers fallbacks so `ILogger<T>` always resolves. To enable real logging, register a logger factory — your registration always wins over the fallback:

```csharp
containerBuilder.RegisterInstance(LoggerFactory.Create(x => x.AddConsole()))
    .As<ILoggerFactory>();
```

> Note: if you construct Benzene from an already-built `IServiceProvider`, Benzene cannot add logging defaults — configure `AddLogging()` on the host yourself (ASP.NET Core and the generic host always do).

## Distributed Tracing

### W3C Trace Context

`Benzene.Diagnostics` can establish the correct `Activity` parent from an inbound
[W3C `traceparent`](https://www.w3.org/TR/trace-context/) header, so distributed traces continue
across services instead of each hop starting a new, disconnected trace. Add `UseW3CTraceContext()`
as the **first** middleware in the pipeline:

```csharp
app.UseW3CTraceContext();
```

Every automatically-wrapped middleware span added by `AddDiagnostics()` after this point nests under
the remote trace. When the header is missing or fails to parse, it falls back to a normal root span —
this is always safe to add.

To propagate the current trace to a downstream Benzene service, add `.UseW3CTraceContext()` to an
outbound route:

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:process", pipeline => pipeline.UseW3CTraceContext().UseSqs(queueUrl))));
```

This stamps `Activity.Current`'s `traceparent`/`tracestate` onto outgoing message headers. It works
today for HTTP, SQS, SNS, and Kafka (all forward headers onto the real request), and for AWS Lambda's
`AwsLambdaBenzeneMessageClient` (which embeds headers into its own message envelope) — but has no
effect on a client pipeline built via the lower-level `UseAwsLambda()`/`LambdaContextConverter` (a raw
`InvokeRequest` has no header-like concept). See [Clients — Header forwarding](clients.md#header-forwarding)
for the full per-transport breakdown.

> Inbound extraction (`UseW3CTraceContext()`) is currently wired for HTTP-based transports (ASP.NET
> Core, Azure Functions' ASP.NET-style trigger, API Gateway) — SQS/SNS/Kafka/Event Hub inbound
> extraction is not yet implemented, so a trace started by an HTTP request continues correctly through
> the outbound clients above, but a queue/stream consumer won't yet pick the parent back up on the
> receiving end.

### OpenTelemetry

`AddDiagnostics()` already produces `Activity` spans (one per middleware, see [Tracing](#tracing)
above) and, when you add [`UseBenzeneMetrics()`](common-middleware.md#usebenzenemetrics), the
`benzene.messages.processed`/`benzene.message.duration` metrics. Neither goes anywhere on its own —
the `Benzene.OpenTelemetry` package wires both into an OTel provider so they get exported to a real
backend:

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddBenzeneInstrumentation()
    .AddOtlpExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddBenzeneInstrumentation()
    .AddOtlpExporter()
    .Build();
```

`AddBenzeneInstrumentation()` is a plain extension on OTel's own `TracerProviderBuilder`/
`MeterProviderBuilder` — it registers no Benzene DI services, so it composes with any exporter or
hosting integration (`OpenTelemetry.Extensions.Hosting`'s `AddOpenTelemetry()`, the console exporter,
etc.) the same way any other `AddSource`/`AddMeter` call would.

For guidance on configuring a `Sampler` (recorded/exported trace volume), see
[Sampling Strategies](sampling-strategies.md). For what data ends up in your logs/traces and how to
avoid capturing PII, see [Privacy & Data Handling](privacy-and-data-handling.md).
