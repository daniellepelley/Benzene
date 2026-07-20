# Debuggability Assessment — finding errors in handlers & middleware

**Date:** 2026-07
**Question:** if something goes wrong in a message handler or in middleware, how easily can a
developer find and diagnose it — via logs, traces, and the debugger? Grounded in the source, not
the docs.

---

## Verdict

**Strong at the two ends, weak in the middle.** Local debugging is excellent (plain C# handlers +
in-memory test hosts), and the *raw material* for production diagnosis is all there (structured
enrichment, correlation, W3C trace context, metrics). But the **default, out-of-the-box** failure
signal is poor: a handler that returns a failure result is logged at `Information` (or not at all),
a thrown exception is only logged if the specific transport happens to catch it, distributed-trace
spans are never marked as failed, and none of the good logging middleware is scaffolded by the
templates or shown in the flagship examples. A developer who wires the observability stack
correctly gets a great experience; a developer who just `dotnet new`s and ships gets a thin one.

---

## What actually happens when something fails

The core pipeline is deliberately transparent: `MiddlewarePipeline.HandleAsync`
(`src/Benzene.Core.Middleware/MiddlewarePipeline.cs`) and `PipelineMessageHandler`
(`src/Benzene.Core.MessageHandlers/PipelineMessageHandler.cs`) **neither catch nor log** — an
exception propagates with its stack trace intact. `MessageRouter`
(`src/Benzene.Core.MessageHandlers/MessageRouter.cs`) logs only *routing* failures — missing topic
and no-handler-found, both at `Warning` (lines 93, 103) — and never logs a handler exception or a
handler failure result. So failure visibility comes entirely from **opt-in middleware** and the
**transport layer**. Three scenarios:

### A. Handler returns a failure result (no exception)
e.g. `BenzeneResult.UnexpectedError(...)` / `NotFound()` / `BadRequest(...)`.
- The framework does **not** log it anywhere.
- If `UseLogResult` is wired, it emits one line — but **always** `logger.LogInformation("BenzeneResult")`
  (`src/Benzene.Core.Middleware/LoggerExtensions.cs:46`), with the status buried in a log *scope*,
  not the message. A failed message is logged at **Info**, so grepping logs for `Error`/`Warning`
  never surfaces it; you must query the structured `status` field.
- Metrics do catch it: `UseBenzeneMetrics` tags `result=failure` for an unsuccessful result
  (`src/Benzene.Diagnostics/MetricsExtensions.cs`).

### B. Handler (or middleware) throws
- If `UseLogResult` is wired, `await next()` throws *before* its log line, so **`UseLogResult`
  logs nothing for that message**.
- `UseExceptionHandler` (opt-in) is the one framework component that logs an exception properly:
  `logger.LogError(ex, "Unhandled exception caught in middleware pipeline")`
  (`src/Benzene.Core.Middleware/ExceptionHandlerMiddleware.cs`) — full stack trace, then hands off
  to your `onException`. But it's opt-in, the message is generic, and it uses a bare `ILogger`.
- Otherwise it's down to the **transport** (matrix below). Many transports catch-and-log well;
  some propagate to the platform host; one catches without logging.

### C. Middleware throws (auth, validation, a custom step)
Same as B — there's nothing special. Because the whole pipeline is uniform middleware, a throw in
middleware is indistinguishable from a throw in a handler except by the stack trace and the
per-middleware span name.

---

## Strengths (genuine, keep these)

1. **Local debugging is first-class.** Handlers and middleware are plain C# — ordinary breakpoints,
   step-through, and exception-on-throw all work. There is no proprietary runtime to debug through.
2. **In-memory test hosts reproduce failures exactly.** `BenzeneTestHost` + the `*.TestHelpers`
   feed a transport-shaped event through the *real* pipeline in a unit test (no cloud, no emulator),
   so a production failure becomes a red test you can step through. This is the single biggest
   debugging asset.
3. **Exceptions are never swallowed by the core.** Full fidelity to the host; the stack trace that
   reaches your logs/host is the real one.
4. **The structured context is rich — when enabled.** `UseBenzeneEnrichment` attaches
   `invocationId`, `traceId`, `spanId`, `topic`, `transport`, and `handler` to the log scope on
   every platform, and W3C trace context ties one request into one trace across HTTP and queue hops.
   A correlated log line, once wired, tells you exactly which message on which transport failed.
5. **Metrics count failures** (thrown or unsuccessful), so dashboards/alerts can fire even when the
   log signal is weak.
6. **Every middleware is named** (`IMiddleware.Name`), so per-middleware Activity spans and timers
   are individually labelled — you can see *which stage* consumed the time.
7. **AWS batch transports log per-record failures with the message id** (see matrix), so partial
   failures are diagnosable without failing the whole batch.

---

## Gaps, ranked by debugging impact

### G1 — Distributed-trace spans are never marked failed
`ActivityMiddlewareDecorator` (`src/Benzene.Diagnostics/ActivityMiddlewareDecorator.cs`) and
`ActivityProcessTimer` (`src/Benzene.Diagnostics/Timers/ActivityProcessTimer.cs`) start an
`Activity`, and on the way out just dispose it. Neither calls `activity.AddException(ex)` /
`activity.SetStatus(ActivityStatusCode.Error)`. **Consequence:** in Jaeger/Tempo/App Insights a span
that threw looks identical to one that succeeded — no error flag, no exception event. Trace-first
debugging (the thing OpenTelemetry is *for*) can't point you at the failing stage. This is the
highest-value fix and it's a few lines in the decorator/timer.

### G2 — Failure results are logged at Info, not Warn/Error
`UseLogResult` logs `LogInformation("BenzeneResult")` regardless of outcome
(`LoggerExtensions.cs:46`). A handler returning `UnexpectedError`/`BadRequest`/`ServiceUnavailable`
produces an **Info** line. Standard triage ("show me the errors") misses every non-throwing
failure. The result status is known at that point — the level should reflect it.

### G3 — On a throw, the result-logging middleware goes silent
Because `UseLogResult` logs *after* `next()`, a throw skips its line entirely (scenario B). So the
one place most teams put "log every message" logs nothing for the messages that most need logging.
A `try/finally` (log on the way out even when `next()` threw, at Error) would close this.

### G4 — Nothing good is on by default
- **Templates** (`templates/content/**`) wire **no** logging or exception middleware.
- The **flagship AWS example** wires `UseSerilog` + `UseTimer` but **not** `UseLogResult`,
  `UseBenzeneEnrichment`, or `UseExceptionHandler`; the **ASP example** wires none of them.
So the default DX is: failure results invisible, exceptions visible only via whatever the transport
does, spans unmarked. The good stack exists but is undiscoverable unless you read `monitoring.md`
and assemble it yourself.

### G5 — One transport catches an exception without logging it
`BenzeneServiceBusWorker` (self-hosted Azure Service Bus,
`src/Benzene.Azure.ServiceBus/BenzeneServiceBusWorker.cs:131`) does
`catch (Exception) { await args.AbandonMessageAsync(...); throw; }` — abandons the message and
rethrows **without a `LogError`**. The rethrow means it isn't fully lost, but Benzene adds no
error log with topic/message context; you're relying on whatever hosts the worker loop.

### G6 — Ordering footguns fail silently
`UseW3CTraceContext()` must be the first middleware, and enrichment's `invocationId` needs
`UseBenzeneInvocation()` upstream. Get the order wrong and enrichment/trace context silently produce
nothing — no warning that your correlation is broken. (`monitoring.md` documents the trace-context
ordering rule, but it's easy to miss.)

### G7 — No troubleshooting / "why did my handler fail" guide
Docs cover observability *setup* (`monitoring.md`) and `global-error-handling.md` (teaches
`UseExceptionHandler`), but there's no single "a message failed in production — here's how to find
out why" page tying the recommended middleware stack, log fields, and trace/metric signals together.

### G8 — The default failure log line is opaque
`"BenzeneResult"` as a message, with everything in scopes, isn't greppable without structured log
querying. A message that named the topic/status (e.g. `"order:create -> UnexpectedError"`) would be
far more useful in a plain log tail.

---

## Transport catch/log matrix (what reaches your logs in production)

| Transport | Exception handling | Error logged with stack trace? |
|---|---|---|
| AWS Lambda SQS (`SqsApplication`) | catch per record → batch-item-failure | **Yes** — `LogError(ex, "Processing SQS message {id} failed")` |
| AWS Lambda SNS (`SnsApplication`) | catch `when (_options.CatchExceptions)` | **Yes** — `LogError(ex, …)` (when catching) |
| AWS Lambda DynamoDb / Kinesis | catch → checkpoint/stop | **Yes** — `LogError(ex, …)` |
| AWS Lambda S3 / EventBridge / Kafka | **no catch** — propagates to Lambda host | Only the Lambda runtime's raw CloudWatch error (no Benzene context; whole invocation fails) |
| Self-hosted SQS / Kafka / RabbitMQ / Event Hub / Cosmos / HTTP workers | catch per message → ack/nack | **Yes** — `LogError(ex, …)` |
| Self-hosted Azure Service Bus (`BenzeneServiceBusWorker`) | catch → abandon + **rethrow** | **No** — abandons and rethrows without logging (G5) |
| Azure Functions triggers | `RaiseOnFailureStatus` escalation → Functions host | Host logs; Benzene adds structured log only where the app catches |
| HTTP (ASP.NET Core / API Gateway / SelfHost) | maps to status code | Exception → the app's error path / `UseExceptionHandler` if wired |

Net: the **AWS batch family and the self-hosted workers are good**; the **single-event AWS Lambda
sources (S3/EventBridge/Kafka) lean on the platform host**, and **self-hosted Service Bus is the one
outright gap**.

---

## Recommendations (quick wins first)

1. **Mark failed spans (G1).** In `ActivityMiddlewareDecorator` and `ActivityProcessTimer`, wrap
   `next()`/the scope in try/catch, call `activity?.AddException(ex)` +
   `activity?.SetStatus(ActivityStatusCode.Error)`, and rethrow. ~10 lines; turns traces into a
   real debugging tool. Highest ROI.
2. **Make result logging level-aware and throw-safe (G2, G3).** In `UseLogResult`, log in a
   `finally`; choose the level from the outcome — Info on success, Warning on a failure result,
   Error on a thrown exception — and put the topic + status in the message text (G8).
3. **Log failure results/exceptions once at the router (G2/B).** A single `LogWarning`/`LogError`
   in `MessageRouter` when the handler result is unsuccessful (topic + status + errors) would give a
   baseline error signal even with no logging middleware wired.
4. **Fix G5:** add a `LogError(ex, …)` in `BenzeneServiceBusWorker`'s catch before abandon/rethrow,
   matching the other workers.
5. **Scaffold sensible defaults (G4).** Have the templates (and the flagship examples) wire
   `UseBenzeneEnrichment` + `UseLogResult` + `UseExceptionHandler` so a new project can *see* its
   failures on day one.
6. **Write a "Diagnosing failures" doc (G7).** One page: the recommended middleware stack and order,
   the log fields you get, and how a failure shows up in logs vs. traces vs. metrics — with the
   ordering footguns (G6) called out.
7. **Warn on broken ordering (G6), longer-term.** A startup check (like the existing registration
   checks) that flags `UseW3CTraceContext`/enrichment wired in an order that will no-op.

None of these change the architecture; they raise the *default* floor to meet the ceiling the
framework already reaches when fully wired.
