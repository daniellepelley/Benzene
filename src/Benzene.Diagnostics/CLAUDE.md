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
  instance in an `Activity` span; registered by `AddDiagnostics()`. Tags `benzene.transport`/
  `benzene.topic`/`benzene.version`/`benzene.handler`, and (2026-07-23) **`benzene.status`** on the
  topic-bearing span only — the real Benzene wire status (`ok`/`not-found`/…) read from the completed
  context's `IHasMessageResult.MessageResult` after `next()`, or `exception` when the span throws. This
  is the trace-store analogue of the metric's `result` tag: it lets a trace-backed mesh reader
  (`Benzene.Mesh.Fleet.*`, `work/otel-fleet-adapter-scope.md` §6a) reconstruct `MeshTraceEvent.Status`
  from the span. On the topic-bearing span only, to avoid a duplicate tag on every wrapped stage.
  Also (2026-07-23) **`benzene.correlation-id`** on the same topic-bearing span, **only** when the
  message carried an `x-correlation-id` header (never the auto-generated `ICorrelationId` GUID — same
  source and null-when-absent rule as `MeshTraceEvent.CorrelationId`, so the mesh never fabricates one).
  This is the searchable span attribute `mesh:query:correlation` needs from a trace store
  (`work/otel-fleet-adapter-scope.md` §6b) — for X-Ray it must be indexed as an *annotation* to be
  filterable.
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
  `Meter`, tagged `topic`/`transport`/`result` (`"<missing>"` when a source isn't resolvable). The
  `result` tag reads the completed context's `IHasMessageResult.MessageResult` and **collapses success
  but itemizes failure** (2026-07-23): `success` for any successful outcome (from `IsSuccessful`, the
  bool — so a successful result carrying a failure-class status, e.g. a health check's
  `service-unavailable`, is still `success`), the failure's **`Status` string verbatim**
  (`not-found`/`unauthorized`/…) for an unsuccessful result, `exception` if the pipeline threw (distinct
  from a returned `unexpected-error`), and `<missing>` only when the context neither implements
  `IHasMessageResult` nor had a result recorded. Rationale: nobody wants `ok`-vs-`created`, but a
  failure mix that's mostly `not-found` reads very differently from mostly `unauthorized`. This is a
  pre-1.0 breaking change to the tag's *value set* (`docs/mesh-usage-feed.md` §1) — instrument
  names/tag keys unchanged. Every shipped transport now
  records that signal — event-style setters (SQS/SNS/EventBridge/Service Bus) set `MessageResult`
  directly, and the request/response setters (`ResponseMessageHandlerResultSetterBase`,
  `ResponseIfHandledMessageHandlerResultSetter`, `BenzeneMessageHandlerResultSetter` — via
  `Benzene.Core.MessageHandlers.Response.MessageResultRecorder`) now record it too, so
  `result=<missing>` no longer appears for a normally-handled HTTP/API-Gateway/BenzeneMessage
  message (it did before those setters recorded — the source of the mesh usage feed's `<missing>`
  status). The instruments:
  - `BenzeneDiagnostics.MessagesProcessed` - `Counter<long>` named `benzene.messages.processed`
  - `BenzeneDiagnostics.MessageDuration` - `Histogram<double>` named `benzene.message.duration` (ms)
  This is the whole metrics surface — there is no configurable/extensible instrument set, no other
  built-in meters, and nothing is recorded unless you add this middleware. Export the `Meter` via
  `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation(MeterProviderBuilder)`.
  These two instruments and their `topic`/`transport`/`result` tag set are also **the mesh usage
  feed's metric metadata standard** (`docs/mesh-usage-feed.md`): backend adapters
  (`Benzene.Mesh.Contracts.IMeshUsageSource`) read them out of whatever backend they were exported
  to and feed the mesh's `usage.json` — so treat the instrument names and tag keys as a published
  contract, not internals.

### Correlation
- `ICorrelationId`/`CorrelationId`, `AddCorrelationId()`, `WithCorrelationId()` - a per-invocation
  correlation value for log scopes. `ICorrelationId` self-generates a GUID; application middleware
  may `Set(...)` it (e.g. from a partner's proprietary header). There is no inbound
  correlation-header middleware - cross-service correlation is W3C `traceparent` propagation
  (below). The `GetHeader` extensions in `Correlation/Extensions.cs` remain and are used by the
  W3C middleware. `CorrelationId`'s self-generation/`Set` guard and `GetHeader`'s
  case-insensitive/multi-key-fallback lookup are unit-tested in
  `test/Benzene.Core.Test/Diagnostics/CorrelationIdTest.cs`/`CorrelationExtensionsTest.cs`.

### Ordering diagnostic (opt-in, advisory)
- `PipelineOrderingDiagnosticsExtensions` - `IServiceResolver.FindPipelineOrderingIssues(builder)` /
  `LogPipelineOrderingIssues(builder, logger?)` (→ `PipelineOrderingIssue[]`). Reads each
  middleware's `Name` by resolving it against the resolver (try/catch per item - a middleware that
  can't be resolved just for name inspection is skipped, never fails the check) and warns when
  `UseW3CTraceContext()` ("W3CTraceContext") is present but not at index 0 of the builder it was
  added to. **Advisory, never throws** - mirrors `Benzene.ResponseEvents`' F1 diagnostic and
  `Benzene.Clients`' `ValidateOutboundRouting` (call once after wiring). Deliberately checks only the
  W3C-first rule: the "enrichment needs `UseBenzeneInvocation` upstream" footgun can't be machine-
  checked here because the batch transports (SQS/SNS/Kafka/Event Hub) auto-wire `UseBenzeneInvocation`
  inside a per-message *sub*-pipeline (a different builder), so a check on the visible builder would
  false-positive on exactly the transports where `invocationId` is correct - that rule stays a
  documented footgun in `docs/diagnosing-failures.md`. Needs the concrete
  `MiddlewarePipelineBuilder<TContext>` (returns empty for any other `IMiddlewarePipelineBuilder`).
  Covered by `test/Benzene.Core.Test/Diagnostics/PipelineOrderingDiagnosticsTest.cs`.

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
- See the [Capability Matrix](../../docs/capability-matrix.md)'s *Distributed tracing* row for what
  Benzene traces out of the box (W3C context on HTTP **and** async transports) and what backend
  wiring is yours

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Abstractions.MessageHandlers** - `ICurrentTransport`/`IMessageGetter<TContext>`/
  `IMessageHandlerDefinitionLookUp`, used to resolve `ActivityMiddlewareDecorator`'s tags
- **Benzene.Core.Middleware** - Middleware implementations

## Important conventions
- `ActivitySource.StartActivity` is a documented no-op (returns `null`) when nothing is listening, and
  `ActivityMiddlewareDecorator` fast-paths that case: it returns the inner middleware's task **directly**
  (no tag work, no try/catch, and no per-stage async state machine allocated), so `AddDiagnostics()` is
  genuinely — not just "effectively" — near-free per stage with no OTel exporter wired. Measured
  (`benchmarks/…/TracingMiddlewareBenchmarks`, 8 stages): armed-but-not-listening dropped from
  ~4,600 ns / ~6 KB to ~600 ns / ~1.5 KB per message. When a listener **is** attached (an exporter is
  wired) the full span-per-stage cost is paid (~4,600 ns / ~6 KB for 8 stages) — inherent to
  per-middleware spans, and separate from the fast-path
- Every middleware gets its own `Activity`, forming a real trace tree via `Activity.Current`'s
  ambient parent-tracking — this is the *automatic* behavior, not something you opt into per call
- `DebugMiddlewareWrapper` is separate from tracing; it's `Debug.WriteLine`-only dev output
