# Benzene.Diagnostics

## What this package does
Tracing and timing utilities for Benzene, built on `System.Diagnostics.Activity`. `BenzeneDiagnostics`
exposes the shared `ActivitySource`/`Meter` ("Benzene") every pipeline stage reports through.
`AddDiagnostics()` wires `ActivityMiddlewareWrapper`, which automatically wraps *every* middleware in
*every* pipeline in an `Activity` span (tagged `benzene.transport`/`benzene.topic`/`benzene.version`/
`benzene.handler` where resolvable) — no explicit call needed per middleware. Also provides
`DebugMiddlewareWrapper` (unrelated `Debug.WriteLine` dev tracing) and correlation-ID middleware.

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
  want plain log-line timers instead of `Activity` spans); not vendor backends, not deprecated

### Correlation
- `ICorrelationId`/`CorrelationId`, `WithCorrelationId()` - `correlationId`-header-based cross-service
  correlation; `UseCorrelationId()` is `[Obsolete]` (superseded by W3C `traceparent` propagation) but
  still supported as a legacy fallback and still emitted to log scopes via `WithCorrelationId()`.
  Default header lookup checks `x-correlation-id`, `correlation-id`, then legacy `correlationId`,
  case-insensitively, first match wins.

### Enrichment
- `UseBenzeneEnrichment<TContext>()` (`EnrichmentExtensions.cs`) - one portable, explicit-opt-in
  middleware that attaches `invocationId` (from `IBenzeneInvocation`), `traceId`/`spanId` (from
  `Activity.Current`), `topic`, `transport`, and `handler` to the logging scope, and tags the current
  `Activity` with `benzene.invocationId`. Each key degrades gracefully (simply omitted) when its
  backing service isn't registered for that pipeline scope — e.g. `invocationId` is omitted inside a
  per-message SQS/SNS/Kafka sub-pipeline, since `IBenzeneInvocation` isn't wired into those nested DI
  scopes today. Replaces the AWS-only `WithRequestId()`/`WithApplication()` (now `[Obsolete]`).

### W3C Trace Context
- `UseW3CTraceContext<TContext>()` (`W3CTraceContextExtensions.cs`) - reads `traceparent`/`tracestate`
  (case-insensitively) and starts the pipeline's root `Activity` with the parsed remote context as its
  parent, so traces continue across services instead of each hop starting a new one. Must be the FIRST
  middleware added — everything after it inherits the correct ambient `Activity.Current` parent. Falls
  back to a normal root span when the header is missing/invalid; never throws. Currently wired for
  HTTP-based transports only (ASP.NET Core, Azure Functions' ASP.NET-style trigger, API Gateway) —
  SQS/SNS/Kafka/Event Hub inbound extraction isn't implemented yet. Outbound injection is a separate
  `WithW3CTraceContext()` `ClientBuilder` decorator in `Benzene.Clients.TraceContext`.

## When to use this package
- Add `AddDiagnostics()` to get automatic `Activity` spans per middleware and `UseTimer("name")`
  support, on every platform (AWS, Azure, ASP.NET Core, Worker)
- Add `UseBenzeneEnrichment()` once per pipeline for portable log/trace enrichment instead of
  hand-composing platform-specific `WithXxx()` log-context extensions
- Add `UseW3CTraceContext()` as the first middleware on HTTP-based pipelines to continue a caller's
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
