# Request Correlation Across Services

Track a single request as it moves through multiple Benzene services.

> W3C trace context is Benzene's cross-service correlation mechanism. If you're migrating from an
> older header-pickup approach, see [the migration guide](../migration-alpha-to-1.0.md) and the
> [partner-header variation](#variation-honoring-a-partners-correlation-header) below for the one
> use case that needs replacing by hand.

## Problem Statement

You're running multiple Benzene services and need to answer "what happened, across every service,
for this one request?" — usually while debugging a production incident or building a dashboard that
groups logs by request.

## The approach: W3C trace context

This is covered in full, worked-example depth in
[Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) — including exporting
to Jaeger/an OTel Collector, propagating across an ASP.NET Core API and an SQS-backed worker, and
the current limitation that inbound extraction only works for HTTP-based transports today. The
short version:

```csharp
// First middleware in the pipeline — establishes the Activity parent from an inbound traceparent header
app.UseW3CTraceContext();
```

```csharp
// On an outbound client — stamps Activity.Current's traceparent/tracestate onto outgoing headers
services.UsingBenzene(x => x.AddBenzeneMessageClients(c => c
    .CreateSqsBenzeneMessageClient("orders", queueUrl, resolver, client => client.WithW3CTraceContext())));
```

Rather than a single opaque ID you grep for, you get a real `System.Diagnostics.Activity` per
pipeline stage (automatic once you call `AddDiagnostics()`), correlated across services by the
shared W3C `traceId`, and rendered as a connected trace tree once you export it via
`Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()`. `UseBenzeneEnrichment()` surfaces the same
`traceId`/`spanId` in your log lines (alongside `invocationId`/`topic`/`transport`/`handler`) — see
[Monitoring & Diagnostics — Structured log scopes](../monitoring.md#structured-log-scopes).

## Variation: honoring a partner's correlation header

Some upstream system already sends a proprietary correlation header (`x-partner-request-id`, a
legacy gateway's `correlationId`, ...) and expects it echoed or forwarded unchanged. `ICorrelationId`
still exists for exactly this: populate it from your own small middleware, attach it to the logging
scope, and forward it on outbound clients.

```csharp
// 1. Populate ICorrelationId from the partner's header
app.Use("PartnerCorrelation", resolver => async (context, next) =>
{
    var headers = resolver.GetService<IMessageHeadersGetter<MyContext>>();
    var value = headers.GetHeader(context, "x-partner-request-id");
    if (!string.IsNullOrEmpty(value))
    {
        resolver.GetService<ICorrelationId>().Set(value);
    }
    await next();
});

// 2. Attach it to the logging scope
app.UseLogResult(x => x.WithCorrelationId());
```

```csharp
// 3. Forward it downstream (stamps the current ICorrelationId onto the outgoing x-correlation-id header)
services.UsingBenzene(x => x.AddBenzeneMessageClients(c => c
    .CreateSqsBenzeneMessageClient("orders", queueUrl, resolver, client => client.WithCorrelationId())));
```

With nothing calling `Set(...)`, `ICorrelationId` self-generates a GUID per invocation — so
`WithCorrelationId()` on its own still gives you a per-invocation marker in logs.

Run this alongside `UseW3CTraceContext()` freely (echo the partner's header at the edge, trace
internally via W3C) — just add `UseW3CTraceContext()` first, since it establishes the root
`Activity` that the automatically-wrapped middleware spans nest under.

## Testing

See [Distributed Tracing with OpenTelemetry — Testing](distributed-tracing-opentelemetry.md#testing)
for the full walkthrough with Jaeger. For the partner-header variation: send a request with the
header set, confirm the same value appears in your logs via `WithCorrelationId()`'s scope, and in
the receiving service's logs if forwarded.

## Troubleshooting

### `correlationId` doesn't appear in log output

- Make sure `.WithCorrelationId()` is chained onto `UseLogResult(...)` — nothing attaches
  `ICorrelationId` to the logging scope without the `With*()` call.
- Confirm your logging provider supports scopes and has them enabled (e.g. the console provider's
  `IncludeScopes` option).

### Correlation ID is different on each service

- Confirm the outbound client has `WithCorrelationId()` — without it, nothing forwards the value
  and each service's `ICorrelationId` self-generates its own GUID.
- Confirm the receiving service's populating middleware reads the same header the sender writes
  (`x-correlation-id` for the built-in client decorator).

## Further Reading

- [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) — the full worked
  example for W3C trace context, including the current SQS/SNS/Kafka/Event Hub inbound limitation
- [Correlation Ids](../correlation-ids.md) — reference for `ICorrelationId`/`WithCorrelationId()`
- [Monitoring & Diagnostics](../monitoring.md) — the full picture of tracing, timers, logging, and OpenTelemetry
- [Logging to Application Insights](logging-application-insights.md) — querying logs by correlation ID
- [Common Middleware](../common-middleware.md) — `UseLogResult`/`UseBenzeneEnrichment` reference
