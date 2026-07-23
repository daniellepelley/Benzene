# Diagnosing Failures

A message failed in production. How do you find out *why*?

This page ties together the signals Benzene emits when something goes wrong in a message handler or
in the middleware pipeline: what you get out of the box with nothing configured, the recommended
middleware stack that makes failures easy to trace, the log fields each layer adds, and how the same
failure shows up across logs, traces, and metrics. For *setting up* observability backends see
[Monitoring & Diagnostics](monitoring.md); for catching exceptions and mapping them to responses see
[Global Error Handling](cookbooks/global-error-handling.md).

## Two kinds of failure

Benzene distinguishes between two failure modes, and they surface differently:

- **An unsuccessful result** — a handler returns `BenzeneResult.NotFound(...)`,
  `BenzeneResult.BadRequest(...)`, `BenzeneResult.UnexpectedError(...)`, etc. This is a *normal*
  return value: the response pipeline serializes an `ErrorPayload` and maps the status to the
  transport (an HTTP 404/400/500, an SQS batch-item-failure, ...). Nothing threw.
- **A thrown exception** — a handler (or middleware, or a mapper) throws. This bypasses the response
  pipeline entirely; how it settles depends on the transport (see the [catch matrix](#what-reaches-your-logs-per-transport)).

The distinction matters when you're reading logs: an unsuccessful result and an exception are
*different events* with different log lines, and a stack trace only exists for the second kind.

## What you get with nothing wired

Even with no logging or diagnostics middleware added, Benzene emits a baseline error signal so
failures aren't completely silent:

| Situation | Signal | Source |
|---|---|---|
| Topic missing from the message | `Warning` "Topic is missing" | `MessageRouter` |
| No handler registered for the topic | `Warning` "No handler found for topic {topic}" | `MessageRouter` |
| Handler returns an unsuccessful result | `Warning` "Handler {handler} for topic {topic} returned unsuccessful status {status}{errors}" | `MessageRouter` |
| Handler/middleware throws | Transport-dependent — see the [catch matrix](#what-reaches-your-logs-per-transport) | the transport application |

These are enough to answer "did my handler run, and did it fail?" from a plain log tail — but they
carry no correlation id, no trace id, and (for the baseline) no per-message processing time. Wiring
the recommended stack below gives you all of that.

> These log lines only reach a sink if a logging provider is configured. `UsingBenzene(...)` calls
> `AddLogging()` so `ILogger<T>` resolves, but with **no provider** every log call is a no-op. Add a
> provider (`AddLogging(x => x.AddConsole())`, Serilog, Application Insights, ...) or you'll see
> nothing regardless of what middleware you add. See [Monitoring — Logging](monitoring.md#logging).

## The recommended stack

Add these to each transport pipeline, in this order, before `.UseMessageHandlers()`:

```csharp
app.UseSqs(sqsApp => sqsApp
    .UseW3CTraceContext()      // 1. FIRST — continue the caller's distributed trace
    .UseBenzeneEnrichment()    // 2. attach invocationId/traceId/topic/transport/handler to every log
    .UseExceptionHandler((SqsMessageContext ctx, Exception ex) => ctx.IsSuccessful = false) // 3. catch + log thrown exceptions
    .UseLogResult(_ => { })    // 4. one structured log line per message (Info on success, Error on throw)
    .UseMessageHandlers());
```

and, once, in `ConfigureServices`:

```csharp
services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
    .AddDiagnostics());        // Activity span per middleware — marks failing spans with Error status
```

What each layer contributes when something fails:

| Layer | On failure it gives you | Notes |
|---|---|---|
| `AddDiagnostics()` | The failing **span** is marked `Error` with an exception event and stack, tagged `benzene.transport`/`benzene.topic`/`benzene.version`/`benzene.handler` | Spans are a no-op until an OTel listener is attached; near-zero cost otherwise |
| `UseW3CTraceContext()` | The trace **continues from the caller** instead of starting fresh, so the failure is on the same trace as whatever triggered it | Must be first; safe when the header is absent |
| `UseBenzeneEnrichment()` | `invocationId`, `traceId`, `spanId`, `topic`, `transport`, `handler` on **every** log line in the pipeline (each omitted gracefully if unavailable) | Portable across all transports; replaces hand-composed `WithXxx()` calls |
| `UseExceptionHandler(...)` | An `Error` log — "Unhandled exception caught in middleware pipeline" — plus your callback decides how the transport settles | The callback is transport-specific; see [Global Error Handling](cookbooks/global-error-handling.md) |
| `UseLogResult(...)` | One line per message: `Info` "BenzeneResult" on success, `Error` "BenzeneResult faulted" (with the exception) on a throw, both with `processTime` | The Error line is what stops a throw from silently skipping the "log every message" line |

Add [`UseBenzeneMetrics()`](common-middleware.md#usebenzenemetrics) too if you want the failure to
also move a counter/histogram — see [below](#how-a-failure-shows-up-in-metrics).

## How a failure shows up across the three signals

The same failing message produces a coordinated picture once the stack is wired:

### In logs

A thrown `InvalidOperationException("boom")` handling `order:create` on SQS, with the stack above:

```
[Error] Benzene: BenzeneResult faulted
    System.InvalidOperationException: boom
    { processTime=42, invocationId=..., traceId=4bf92f..., topic=order:create, transport=sqs, handler=CreateOrderHandler }

[Error] Benzene: Unhandled exception caught in middleware pipeline
    System.InvalidOperationException: boom
       at MyApp.CreateOrderHandler.HandleAsync(...)
    { invocationId=..., traceId=4bf92f..., topic=order:create, transport=sqs, handler=CreateOrderHandler }
```

`UseLogResult` sits *inside* `UseExceptionHandler`, so the innermost catch logs first ("faulted",
with `processTime`), then the exception handler's net logs and settles the message. Both lines
describe the same throw from different layers — the `traceId` ties them together.

An *unsuccessful result* (no throw) instead produces a single `Warning` from the router:

```
[Warning] Benzene: Handler CreateOrderHandler for topic order:create returned unsuccessful status not-found
    { invocationId=..., topic=order:create, transport=sqs, handler=CreateOrderHandler }
```

Grep tips: `traceId` ties every line of one message together (and across services); `topic` +
`status` narrows to a failure kind; `transport` separates which adapter handled it.

### In traces

With `AddDiagnostics()` and an OTel exporter ([Monitoring — OpenTelemetry](monitoring.md#opentelemetry)),
the failing middleware's span carries `status = Error` and an `exception` event with the stack, so a
trace viewer (Jaeger, Tempo, Application Insights) points straight at the stage that threw — the
`MessageRouter` span for a handler throw. Every span is tagged with the topic/handler, and
`UseW3CTraceContext()` keeps it on the caller's trace.

### In metrics

With [`UseBenzeneMetrics()`](common-middleware.md#usebenzenemetrics), every message moves two
instruments on the `"Benzene"` meter, tagged `topic`/`transport`/`result`:

- `benzene.messages.processed` (counter) — the `result` tag lets you alert on a rising rate of a
  specific failure status for a topic
- `benzene.message.duration` (histogram, ms) — a failure that's actually a timeout shows up here as
  a latency spike, not just an error count

## Ordering footguns

A few middleware silently do nothing if wired in the wrong order — no error, just missing data:

- **`UseW3CTraceContext()` must be the *first* middleware.** Everything after it inherits the
  correct ambient `Activity.Current` parent; put it later and the spans before it start a new,
  disconnected trace and inbound `traceparent` continuation is lost.
- **Enrichment's `invocationId` needs `UseBenzeneInvocation()` upstream.** On the batch/per-message
  transports (SQS, SNS, Kafka, Event Hub) this is wired automatically per record; on a hand-built
  pipeline, if you want `invocationId` you need the invocation populated before `UseBenzeneEnrichment()`
  runs, or that one field is simply omitted.
- **`UseExceptionHandler(...)` only wraps what comes *after* it.** Register it before
  `.UseMessageHandlers()` (and before anything else whose throws you want caught). Anything earlier
  in the chain is unprotected.
- **`UseLogResult(...)` should sit outside the handlers** so its `processTime` covers the whole
  pipeline and it logs the result the handlers produced.

For the first of these, there's an opt-in startup check: `IServiceResolver.LogPipelineOrderingIssues(builder)`
(`Benzene.Diagnostics`) warns if `UseW3CTraceContext()` isn't the first middleware in a pipeline.
Call it once after building a pipeline; it's advisory and never throws. (The enrichment/invocation
rule isn't machine-checkable — the batch transports auto-wire `UseBenzeneInvocation` inside a
per-message sub-pipeline the check can't see — so that one stays a documented footgun.)

## What reaches your logs per transport

When a handler throws, what Benzene logs (before any middleware you add) depends on the transport
application that owns the invocation:

| Transport | Exception handling | Benzene-logged with context? |
|---|---|---|
| AWS Lambda SQS (`SqsApplication`) | catch per record → batch-item-failure | **Yes** — `Error` "Processing SQS message {id} failed" |
| AWS Lambda SNS (`SnsApplication`) | catch when `CatchExceptions` | **Yes** — `Error` (when catching) |
| AWS Lambda DynamoDb / Kinesis | catch → checkpoint/stop | **Yes** — `Error` per record |
| AWS Lambda S3 / EventBridge / Kafka | **no catch** — propagates to the Lambda host | Only the Lambda runtime's raw error (no Benzene context; whole invocation fails) |
| Self-hosted SQS / Kafka / RabbitMQ / Event Hub / Cosmos / HTTP workers | catch per message → ack/nack | **Yes** — `Error` per message |
| Self-hosted Azure Service Bus (`BenzeneServiceBusWorker`) | catch → abandon + rethrow | **Yes** — `Error` per message with the message id, plus the receive-side error handler |
| Azure Functions triggers | `RaiseOnFailureStatus` escalation → Functions host | Host logs; Benzene adds structured logs where the app catches |
| HTTP (ASP.NET Core / API Gateway / SelfHost) | maps to a status code | Exception → the app's error path / `UseExceptionHandler` if wired |

Two takeaways: the **AWS batch family and the self-hosted workers already attribute a throw to a
specific message**; the **single-event AWS Lambda sources (S3/EventBridge/Kafka) lean on the platform
host**, so wiring `UseExceptionHandler`/`UseLogResult` there is what gets you a Benzene-context log
line instead of a bare stack trace in CloudWatch.

## Checklist

When a message failed and you're trying to find out why:

1. **Was it an unsuccessful result or a throw?** Look for the router `Warning` (result) vs. an
   `Error` with a stack (throw).
2. **Grep by `traceId`** (or `invocationId`) to pull every line of that one message together.
3. **Check the `topic`/`handler` tags** to confirm it routed to the handler you expect — a
   "No handler found for topic" warning means a routing/registration problem, not a handler bug.
4. **Open the trace** if you export spans — the `Error`-status span names the failing stage.
5. **Nothing in the logs at all?** Confirm a logging **provider** is configured (not just
   `AddLogging()`), and that `UseLogResult`/`UseExceptionHandler` sit before `.UseMessageHandlers()`.

## Further reading

- [Monitoring & Diagnostics](monitoring.md) — configuring logging providers, tracing, W3C context, and OpenTelemetry export
- [Global Error Handling](cookbooks/global-error-handling.md) — `UseExceptionHandler` behavior and per-transport response mapping
- [Common Middleware](common-middleware.md) — reference for `UseBenzeneEnrichment`, `UseLogResult`, `UseBenzeneMetrics`
- [Correlation Ids](correlation-ids.md) / [Request Correlation cookbook](cookbooks/request-correlation.md) — tracking a request across services
- [Privacy & Data Handling](privacy-and-data-handling.md) — keeping PII out of the logs/traces above
