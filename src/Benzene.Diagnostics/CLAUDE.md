# Benzene.Diagnostics

## What this package does
Tracing and timing utilities for Benzene, built on `System.Diagnostics.Activity`. `BenzeneDiagnostics`
exposes the shared `ActivitySource`/`Meter` ("Benzene") every pipeline stage reports through.
`AddDiagnostics()` wires `ActivityMiddlewareWrapper`, which automatically wraps *every* middleware in
*every* pipeline in an `Activity` span (tagged `benzene.transport`/`benzene.topic`/`benzene.version`/
`benzene.handler` where resolvable) — no explicit call needed per middleware. `AddDiagnostics()` also
registers `DebugMiddlewareWrapper` as an `IMiddlewareWrapper`, so it too wraps *every* middleware and
emits `Debug.WriteLine` start/complete lines per stage (unrelated to `Activity`/tracing; visible only
under a debugger/`DEBUG` build), and an `ActivityProcessTimerFactory` as the default
`IProcessTimerFactory`. If you want *only* the span-per-middleware behaviour without the debug wrapper,
timer factory, and correlation registrations, call the focused `AddActivityPerMiddleware()` instead —
it registers just `ActivityMiddlewareWrapper`. Both share the same `IsTypeRegistered` guard, so
calling both (or either twice) never double-wraps a middleware; `AddDiagnostics()` itself delegates to
`AddActivityPerMiddleware()` for that single registration. Correlation-ID registration lives in `DiagnosticsRegistrations` (auto-discovered
`RegistrationsBase`), not in `AddDiagnostics()` itself.

## Key types/interfaces

### Tracing
- `BenzeneDiagnostics` - the shared `ActivitySource`/`Meter`, named `"Benzene"`
- `ActivityMiddlewareWrapper`/`ActivityMiddlewareDecorator<TContext>` - auto-wraps every middleware
  instance in an `Activity` span; registered by `AddDiagnostics()`
- `DebugMiddlewareWrapper`/`DebugMiddlewareDecorator<TContext>` - `Debug.WriteLine` start/stop
  tracing, unrelated to `Activity`/tracing proper

### Timers (`IProcessTimer`/`IProcessTimerFactory`)
- `IProcessTimer`/`IProcessTimerFactory` - a scoped, named timer abstraction; `UseTimer(string)`
  resolves the registered `IProcessTimerFactory` and wraps `next()` in one
- `ActivityProcessTimer`/`ActivityProcessTimerFactory` - the default `IProcessTimerFactory`
  registered by `AddDiagnostics()`; opens a real `Activity` per timer, same source as
  `ActivityMiddlewareWrapper`. **`IProcessTimer` is kept for source-compat with existing
  `UseTimer("name")` call sites — new code should prefer `Activity`/`ActivitySource` directly.**
- `LoggingProcessTimer(Factory)`/`DebugProcessTimer`/`DebugTimerFactory`/`CompositeProcessTimer(Factory)` -
  generic `IProcessTimerFactory` implementations still available to register explicitly (e.g. if you
  want plain log-line timers instead of `Activity` spans); not vendor backends, not deprecated.
  `LoggingProcessTimer`'s start/complete log lines and tag-composition, and `CompositeProcessTimer`/
  `CompositeProcessTimerFactory`'s fan-out to every inner timer/factory, are unit-tested in
  `test/Benzene.Core.Test/Diagnostics/LoggingProcessTimerTest.cs` and
  `CompositeProcessTimerTest.cs`.

### Metrics
- `UseBenzeneMetrics<TContext>()` (`MetricsExtensions.cs`) - explicit-opt-in middleware that records
  once per message (not per-middleware). Emits exactly two fixed instruments on the shared `"Benzene"`
  `Meter`, tagged `topic`/`transport`/`result` (`"<missing>"` when a source isn't resolvable):
  - `BenzeneDiagnostics.MessagesProcessed` - `Counter<long>` named `benzene.messages.processed`
  - `BenzeneDiagnostics.MessageDuration` - `Histogram<double>` named `benzene.message.duration` (ms)
  This is the whole metrics surface — there is no configurable/extensible instrument set, no other
  built-in meters, and nothing is recorded unless you add this middleware. Export the `Meter` via
  `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation(MeterProviderBuilder)`.

### Correlation
- `ICorrelationId`/`CorrelationId`, `AddCorrelationId()`, `WithCorrelationId()` - a per-invocation
  correlation value for log scopes. `ICorrelationId` self-generates a GUID; application middleware
  may `Set(...)` it (e.g. from a partner's proprietary header). There is no inbound
  correlation-header middleware - cross-service correlation is W3C `traceparent` propagation
  (below). The `GetHeader` extensions in `Correlation/Extensions.cs` remain and are used by the
  W3C middleware. `CorrelationId`'s self-generation/`Set` guard and `GetHeader`'s
  case-insensitive/multi-key-fallback lookup are unit-tested in
  `test/Benzene.Core.Test/Diagnostics/CorrelationIdTest.cs`/`CorrelationExtensionsTest.cs`.

### Enrichment
- `UseBenzeneEnrichment<TContext>()` (`EnrichmentExtensions.cs`) - one portable, explicit-opt-in
  middleware that attaches `invocationId` (from `IBenzeneInvocation`), `traceId`/`spanId` (from
  `Activity.Current`), `topic`, `transport`, and `handler` to the logging scope, and tags the current
  `Activity` with `benzene.invocationId`. Each key degrades gracefully (simply omitted) when its
  backing service isn't registered for that pipeline scope. **Fixed (release plan Tier 3.5,
  2026-07-18):** `invocationId` used to be omitted inside a per-message SQS/SNS/Kafka/Event Hub
  sub-pipeline, because each of those transports dispatches every record/event through its own
  fresh DI scope (`serviceResolverFactory.CreateScope()`), disconnected from whatever
  `IBenzeneInvocation` an outer Lambda/Functions-invocation-level pipeline populated -
  `IBenzeneInvocation` is scoped, not ambient, so a fresh scope's accessor was always `null`. Every
  batch/per-message transport now auto-wires its own `UseBenzeneInvocation()` as the first
  middleware in its per-message pipeline (no application code changes required), deriving
  `InvocationId` from that message's own natural identifier: `SqsMessageContext`/
  `SqsConsumerMessageContext` → the SQS `MessageId`; `SnsRecordContext` → the SNS `MessageId`;
  `KafkaContext`/`KafkaRecordContext<TKey,TValue>` → `"{topic}-{partition}-{offset}"` (Kafka has no
  single message-id field); `EventHubConsumerContext` → the event's `SequenceNumber`. See each
  transport package's own `BenzeneInvocationExtensions.cs` (`Benzene.Aws.Lambda.Sqs`,
  `Benzene.Aws.Sqs.Consumer`, `Benzene.Aws.Lambda.Sns`, `Benzene.Aws.Lambda.Kafka`,
  `Benzene.Kafka.Core.KafkaMessage`, `Benzene.Azure.EventHub`, `Benzene.Azure.Function.EventHub`,
  `Benzene.Azure.Function.Kafka` — the Azure Functions Event Hub/Kafka triggers have the identical
  `MiddlewareMultiApplication`-per-record scope issue as the AWS Lambda batch transports, fixed the
  same way). Replaced the AWS-only `WithRequestId()`/`WithApplication()`, which have been removed.

### W3C Trace Context
- `UseW3CTraceContext<TContext>()` (`W3CTraceContextExtensions.cs`) - reads `traceparent`/`tracestate`
  (case-insensitively) and starts the pipeline's root `Activity` with the parsed remote context as its
  parent, so traces continue across services instead of each hop starting a new one. Must be the FIRST
  middleware added — everything after it inherits the correct ambient `Activity.Current` parent. Falls
  back to a normal root span when the header is missing/invalid; never throws.
  - **It is, and always was, fully generic and transport-agnostic** - the only requirement is that
    `IMessageHeadersGetter<TContext>` be registered in DI for that context type (the same seam the
    message-handler dispatch pipeline already uses for headers). "HTTP-only" (as this doc used to
    claim) described the fact that nobody had wired it into an async transport's pipeline anywhere
    in the codebase yet, not a limitation of the middleware itself.
  - **Fixed for real (release plan Tier 3.5, 2026-07-18):** now proven to work end to end on SQS,
    SNS, Kafka (AWS Lambda, the self-hosted `Benzene.Kafka.Core` worker, and the Azure Functions
    Kafka trigger), and Event Hub (the self-hosted `Benzene.Azure.EventHub` worker and the Azure
    Functions Event Hub trigger) - see each transport's `CLAUDE.md`. Two real gaps were found and
    closed along the way (not just documentation): `Benzene.Azure.Function.Kafka`'s
    `KafkaMessageHeadersGetter` was a stub that always returned `{}` regardless of what was on the
    record (confirmed via reflection against the installed
    `Microsoft.Azure.Functions.Worker.Extensions.Kafka` 4.3.0 that the trigger's bound type
    genuinely does expose headers - this was a Benzene oversight, not a platform limitation), and
    `Benzene.Azure.Function.EventHub`'s `EventHubContext` had **no**
    `IMessageHeadersGetter<EventHubContext>` registered at all (the package had no first-class
    mappers of its own, only the envelope-routing path). Every other transport's getter was already
    correct; adding W3C trace context there needed no code change, only the same DI-registration
    check plus tests proving it.
  - Outbound injection is a separate `WithW3CTraceContext()`-equivalent - see
    `Benzene.Clients.TraceContext`'s `W3CTraceContextMiddleware`/`.UseW3CTraceContext()` (the
    `OutboundContext` overload, opposite direction, no naming collision).

## When to use this package
- Add `AddDiagnostics()` to get automatic `Activity` spans per middleware and `UseTimer("name")`
  support, on every platform (AWS, Azure, ASP.NET Core, Worker)
- Add `AddActivityPerMiddleware()` when you want *only* the automatic span-per-middleware behaviour
  and none of the other diagnostics registrations (debug wrapper, timer factory, correlation)
- Add `UseBenzeneEnrichment()` once per pipeline for portable log/trace enrichment instead of
  hand-composing platform-specific `WithXxx()` log-context extensions
- Add `UseW3CTraceContext()` as the first middleware on any pipeline (HTTP, SQS, SNS, Kafka, Event
  Hub - anything with a registered `IMessageHeadersGetter<TContext>`) to continue a caller's
  distributed trace instead of starting a new one per hop
- Wire `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` against an OTel `TracerProviderBuilder`/
  `MeterProviderBuilder` to actually export what this package produces to a real backend

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Abstractions.MessageHandlers** - `ICurrentTransport`/`IMessageGetter<TContext>`/
  `IMessageHandlerDefinitionLookUp`, used to resolve `ActivityMiddlewareDecorator`'s tags
- **Benzene.Core.Middleware** - Middleware implementations

## Important conventions
- `ActivitySource.StartActivity` is a documented no-op (returns `null`) when nothing is listening —
  `AddDiagnostics()` has effectively zero cost with no OTel exporter wired up, same as before
- Every middleware gets its own `Activity`, forming a real trace tree via `Activity.Current`'s
  ambient parent-tracking — this is the *automatic* behavior, not something you opt into per call
- `DebugMiddlewareWrapper` is separate from tracing; it's `Debug.WriteLine`-only dev output
