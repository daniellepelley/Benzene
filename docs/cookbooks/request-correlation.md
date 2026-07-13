# Request Correlation Across Services

Track a single request as it moves through multiple Benzene services, whether you need a simple header-based ID for an existing system or full distributed tracing for a new one.

## Problem Statement

You're running multiple Benzene services and need to answer "what happened, across every service, for this one request?" — usually while debugging a production incident or building a dashboard that groups logs by request. Benzene has two different, non-overlapping ways to do this:

- **The legacy `correlationId`-style header** (`UseCorrelationId()`/`WithCorrelationId()`) — a single opaque string, read from and written to a plain HTTP-style header, that you look up by exact-match in your logs.
- **W3C trace context** (`UseW3CTraceContext()`/`WithW3CTraceContext()`) — the modern, standards-based approach, where each hop gets its own `Activity` span nested under the caller's, and a real tracing backend (Jaeger, an OTel Collector, ...) renders the whole thing as one connected trace tree, not just a shared ID to grep for.

This cookbook covers both, and — more importantly — how to decide which one you actually need.

## Which one do I use?

| | Legacy `UseCorrelationId()` | W3C trace context |
|---|---|---|
| Status | `[Obsolete]`, kept as a legacy fallback | Recommended for new work |
| What you get | One opaque string ID, propagated as a header | A real trace tree (parent/child spans) plus the same ID concept via `traceId` |
| Where it shows up | Log lines (`customDimensions`/log scope), manually grepped/joined | A tracing backend UI (Jaeger, etc.) — spans nest automatically |
| Inbound support | Any transport — it's just a header lookup | HTTP-based transports only today (see [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md#the-current-limitation-honestly-stated)) |
| Best for | Integrating with an existing system that already sends/expects a `correlationId`-style header (a legacy gateway, a partner API, an older service you don't control) | New services, or any service you're free to add real tracing to |

Reach for the legacy header when you don't have a choice — some upstream system already sends `X-Correlation-Id` (or `correlation-id`, or the historical `correlationId`) and expects it echoed back or forwarded unchanged, and you need Benzene to slot into that contract rather than replace it. Reach for W3C trace context for everything else, including when you're adding tracing to a brand-new service pair — it's the one covered in depth in [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md), which this cookbook cross-references rather than repeats.

The two are not mutually exclusive — nothing stops you from running both in the same pipeline (e.g. echoing a partner's `correlationId` header back to them while also tracing internally via W3C context) — but don't reach for the legacy one for new, all-Benzene service-to-service calls; it doesn't give you anything W3C trace context doesn't already do better.

## Prerequisites

- A Benzene service (AWS Lambda, Azure Functions, or ASP.NET Core)
- `Benzene.Diagnostics` (both approaches below live in this package)

## Installation

```bash
dotnet add package Benzene.Diagnostics --prerelease
```

## Option A: The legacy `correlationId`-style header

### 1. Add the middleware

`UseCorrelationId()` looks for a correlation ID in the incoming message headers, in order: `x-correlation-id`, then `correlation-id`, then the legacy `correlationId` (matched case-insensitively, first match wins). If none is found, it generates a new GUID.

```csharp
using Benzene.Diagnostics.Correlation;

app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
    .UseCorrelationId());
```

Pass an explicit header name to check only that one instead of the default fallback list:

```csharp
app.UseCorrelationId("x-partner-correlation-id");
```

This registers `ICorrelationId`, resolvable via dependency injection for the rest of the request — inject it anywhere you need the current value:

```csharp
public class MyHandler
{
    private readonly ICorrelationId _correlationId;

    public MyHandler(ICorrelationId correlationId)
    {
        _correlationId = correlationId;
    }
}
```

`UseCorrelationId()` is marked `[Obsolete]` — it still compiles and works exactly as described (this isn't a deprecation that changes behavior), the annotation is there to steer new code toward W3C trace context instead.

### 2. Add it to the logging scope

```csharp
app.UseLogResult(x => x.WithCorrelationId());
```

This attaches a `correlationId` property to the logging scope (via `ILogger.BeginScope`) for the duration of the request, so every log line from every provider that supports scopes (console with `IncludeScopes`, Serilog's `LogContext`, Application Insights, ...) carries it. See [Logging to Application Insights](logging-application-insights.md) for a full worked example querying by this exact property.

### 3. Propagate it downstream

To forward the same correlation ID when calling another Benzene service, add the matching client-side decorator from `Benzene.Clients.CorrelationId`:

```csharp
services.UsingBenzene(x => x.AddBenzeneMessageClients(c => c
    .CreateSqsBenzeneMessageClient("orders", queueUrl, resolver, client => client.WithCorrelationId())));
```

This stamps the current `ICorrelationId` onto the outgoing request's headers, so the receiving service's own `UseCorrelationId()` picks up the same value instead of minting a new one.

## Option B: W3C trace context (recommended for new work)

This is covered in full, worked-example depth in [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) — including exporting to Jaeger/an OTel Collector, propagating across an ASP.NET Core API and an SQS-backed worker, and the current limitation that inbound extraction only works for HTTP-based transports today. The short version:

```csharp
// First middleware in the pipeline — establishes the Activity parent from an inbound traceparent header
app.UseW3CTraceContext();
```

```csharp
// On an outbound client — stamps Activity.Current's traceparent/tracestate onto outgoing headers
services.UsingBenzene(x => x.AddBenzeneMessageClients(c => c
    .CreateSqsBenzeneMessageClient("orders", queueUrl, resolver, client => client.WithW3CTraceContext())));
```

Rather than a single opaque ID you grep for, you get a real `System.Diagnostics.Activity` per pipeline stage (automatic once you call `AddDiagnostics()`), correlated across services by the shared W3C `traceId`, and rendered as a connected trace tree once you export it via `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()`. `UseBenzeneEnrichment()` also surfaces the same `traceId`/`spanId` in your log lines (alongside `invocationId`/`topic`/`transport`/`handler`) without you hand-composing `WithCorrelationId()`-style extensions — see [Monitoring & Diagnostics — Structured log scopes](../monitoring.md#structured-log-scopes).

## Testing

### Legacy correlation ID

1. Send a request with an explicit header: `curl -H "x-correlation-id: test-123" ...`
2. Confirm the same value appears in your logs (via `WithCorrelationId()`'s log scope) and, if you're propagating it downstream, in the receiving service's logs too.
3. Send a request with no header at all and confirm a new GUID is generated and still flows through consistently within that single request.

### W3C trace context

See [Distributed Tracing with OpenTelemetry — Testing](distributed-tracing-opentelemetry.md#testing) for the full walkthrough with Jaeger.

## Troubleshooting

### Correlation ID is different on each service

- Confirm the outbound client has `WithCorrelationId()` (not just the inbound `UseCorrelationId()` on each service independently) — without it, each service generates its own GUID since nothing forwards the value.
- Check header casing/name if you passed an explicit header name to `UseCorrelationId(...)` on one service but relied on the default fallback list on another — they need to agree on which header is authoritative.

### `correlationId` doesn't appear in log output

- Make sure `.WithCorrelationId()` is chained onto `UseLogResult(...)`, not just `UseCorrelationId()` on its own — the middleware registers `ICorrelationId`, but nothing attaches it to the logging scope without the `With*()` call.
- Confirm your logging provider supports scopes and has them enabled (e.g. the console provider's `IncludeScopes` option).

### Mixing both approaches on the same pipeline

There's nothing stopping you from adding both `UseCorrelationId()` and `UseW3CTraceContext()` to the same pipeline (e.g. echoing a partner's correlation header while also tracing internally) — just add `UseW3CTraceContext()` first, since it establishes the root `Activity` that every other automatically-wrapped middleware span (including `UseCorrelationId()`'s own) nests under.

## Variations

### Explicit header name for a specific partner contract

```csharp
app.UseCorrelationId("x-partner-request-id");
```

Use this when integrating with a specific upstream system whose header name doesn't match Benzene's default fallback list (`x-correlation-id`, `correlation-id`, `correlationId`).

### Correlation ID plus full tracing

Run `UseW3CTraceContext()` for real distributed tracing internally, while still honoring/echoing a partner's `correlationId`-style header at the edge — add both, with `UseW3CTraceContext()` first:

```csharp
app.UseW3CTraceContext()
   .UseCorrelationId("x-partner-request-id");
```

## Further Reading

- [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) — the full worked example for W3C trace context, including the current SQS/SNS/Kafka/Event Hub inbound limitation
- [Correlation Ids](../correlation-ids.md) — reference documentation for `UseCorrelationId()`/`WithCorrelationId()`
- [Monitoring & Diagnostics](../monitoring.md) — the full picture of tracing, timers, logging, and OpenTelemetry
- [Logging to Application Insights](logging-application-insights.md) — querying logs by correlation ID
- [Common Middleware](../common-middleware.md) — `UseLogResult`/`UseBenzeneEnrichment` reference
