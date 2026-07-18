# Correlation Ids

> Cross-service correlation is handled by automatic [W3C `traceparent` propagation](monitoring.md#w3c-trace-context)
> (`UseW3CTraceContext()`), which continues a distributed trace from the incoming
> `traceparent`/`tracestate` headers on every transport — see the
> [migration guide](migration-alpha-to-1.0#correlation-ids).

## What remains

`ICorrelationId` and its log-scope enrichment are still available. `WithCorrelationId()` attaches a
`correlationId` value to the logging scope (via `ILogger.BeginScope`):

```csharp
.UseLogResult(x => x.WithCorrelationId());
```

With nothing populating it, `ICorrelationId` self-generates a GUID per scope — useful as a
per-invocation marker in logs. To populate it from a custom source (e.g. a partner's proprietary
header), register `AddCorrelationId()` and call `ICorrelationId.Set(...)` from your own middleware:

```csharp
app.Use("PartnerCorrelation", resolver => async (context, next) =>
{
    var headers = resolver.GetService<IMessageHeadersGetter<MyContext>>();
    resolver.GetService<ICorrelationId>().Set(headers.GetHeader(context, "x-partner-request-id"));
    await next();
});
```

Outbound clients can still forward the value: `.UseCorrelationId()` on an outbound route pipeline
(see [Clients — Outbound middleware](clients.md#outbound-middleware)) stamps the current
`ICorrelationId` onto the outgoing request's `x-correlation-id` header.

## See Also

- [Monitoring & Diagnostics — W3C Trace Context](monitoring.md#w3c-trace-context)
- [Request Correlation cookbook](cookbooks/request-correlation.md)
