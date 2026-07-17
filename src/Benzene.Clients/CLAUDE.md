# Benzene.Clients

## What this package does
Client abstractions for calling Benzene services. Provides interfaces and base implementations for building type-safe clients that communicate with Benzene HTTP endpoints and message handlers.

## Key types/interfaces

### Outbound routing (current, 2026-07-17)
The redesigned outbound mechanism from `work/benzene-clients-redesign-plan.md` - a single
topic-keyed pipeline table replacing the service-name/topic-key factory + decorator-chain shape
below. See that document for the full design and the 4-step migration plan. Steps 1-3 are done: the
mechanism itself, `Benzene.CodeGen.Client`'s generated clients target it, and
`Benzene.Clients.Aws`'s SQS/SNS transports (and the cross-cutting middleware below) are wired onto
it - `.UseAwsLambda(...)` is explicitly deferred (see that package's `CLAUDE.md`).

### Outbound middleware (current, 2026-07-17)
The middleware-ification of the old `ClientBuilder`/`IDependencyWrapper<T>` decorators, for use on
an `OutboundRoutingBuilder.Route(...)` pipeline:
- **Retry** - no new type needed. `Benzene.Resilience.RetryMiddleware<TContext>`/
  `.UseRetry<TContext>(...)` (already existed, fully generic) works on `OutboundContext` unmodified -
  pass `shouldRetryContext: ctx => ((IBenzeneResult)ctx.Response).IsServiceUnavailable() || ...`
  to match `RetryBenzeneMessageClient`'s old default retry predicate.
- `CorrelationIdMiddleware` (`Benzene.Clients.CorrelationId`) / `.UseCorrelationId(correlationKey = "correlationId")` -
  stamps the current `ICorrelationId.Get()` value onto `OutboundContext.Headers`. Converted from
  `CorrelationIdBenzeneMessageClient`.
- `W3CTraceContextMiddleware` (`Benzene.Clients.TraceContext`) / `.UseW3CTraceContext()` - stamps
  `Activity.Current`'s `traceparent`/`tracestate` onto `OutboundContext.Headers`. Converted from
  `TraceContextBenzeneMessageClient`. Same method name as `Benzene.Diagnostics.UseW3CTraceContext<TContext>()`
  (the *inbound* trace-context-extraction middleware) - no collision since one's generic and one's
  a concrete `OutboundContext` overload, but don't confuse the two; they run in opposite directions.
- No dedicated `HeadersMiddleware` - unnecessary. `IBenzeneMessageSender.SendAsync`'s per-call
  `headers` parameter (see above) already closes the "ambient mutable header state" concern
  structurally, without a decorator.
- `IBenzeneMessageSender` - the one interface business logic depends on:
  `SendAsync<TRequest,TResponse>(topic, request, headers = null)`. No service name, no client type,
  no factory resolution at the call site. `headers` is optional per-call metadata (e.g. a
  caller-supplied correlation/tenant value) - **note this is one deviation from the design doc's
  §2.1 two-arg snippet**, added while implementing Step 2 because the pre-existing generated
  client's public API already had a real per-call headers overload that migrating away from
  `IBenzeneMessageClientFactory` would otherwise have silently dropped.
- `OutboundContext` - the outbound pipeline context: `Topic`, `Request`, `Headers` (per-call, never
  null), and a settable `Response` slot. Deliberately non-generic, matching every other
  `IMiddleware<TContext>` in this codebase.
- `OutboundRoutingBuilder` / `AddOutboundRouting(...)` - builds one `IMiddlewarePipeline<OutboundContext>`
  per topic via `.Route(topic, pipeline => ...)`; `Build()` throws `DuplicateOutboundRouteException`
  for a repeated topic. `AddOutboundRouting(...)` registers the resulting `IBenzeneMessageSender`
  plus an `OutboundRoutingTopics` singleton (the registered topic set, for `ValidateOutboundRouting()` below).
- `DefaultBenzeneMessageSender` (internal) - resolves a topic to its pipeline and runs it; throws
  `UnroutedTopicException` for a topic with no registered route.
- `ValidateOutboundRouting()` (on `IServiceResolver`) - per design §2.5: reflects over every loaded
  assembly for a `*Routing` type with a public static `string[] RequiredTopics` field (what
  `Benzene.CodeGen.Client`'s generated `{Service}ServiceClientRouting` classes emit) and throws
  `MissingOutboundRoutesException` listing every required topic with no registered route. Opt-in -
  call once, typically right after resolving `IBenzeneMessageSender`. **Caveat for test authors**:
  the reflection scan is genuinely global (`AppDomain.CurrentDomain.GetAssemblies()`), so a
  test-local `*Routing` type with a `RequiredTopics` field stays loaded and visible to every other
  `ValidateOutboundRouting()` call for the rest of that test process, once JIT-touched - see
  `test/Benzene.Core.Test/Clients/ValidateOutboundRoutingTest.cs`'s own `OrderServiceClientRouting`
  for the one that currently exists.
- Transport-specific route-registration extensions (`.UseSqs(...)`/`.UseSns(...)`/etc. on the
  outbound pipeline builder) and the middleware-ified decorators (retry/headers/correlation/trace
  context) are **not yet implemented** - still Step 3 of the migration plan; for now, `.Route(...)`
  pipelines are built with whatever `IMiddleware<OutboundContext>` exists at the time.

### Benzene.CodeGen.Client generated clients (current, 2026-07-17)
`MessageClientSdkBuilder` (in `Benzene.CodeGen.Client`, not this package, but documented here since
it's the primary consumer of the outbound routing mechanism above) now generates against
`IBenzeneMessageSender` instead of `IBenzeneMessageClientFactory`: the generated
`{Service}ServiceClient`'s constructor takes `IBenzeneMessageSender sender`, and every generated
`XAsync(message, headers)` method body is `return _sender.SendAsync<TRequest,TResponse>("topic", message, headers);`
- no more per-call `using (var client = _clientFactory.Create(...))`. Each generated client also
now emits a sibling `{Service}ServiceClientRouting` static class with a `RequiredTopics` array
(every request topic plus `"healthcheck"`), for `ValidateOutboundRouting()` above. The generated
*interface* (`I{Service}ServiceClient`) is unchanged - this is purely an implementation-detail
change in the generated class body.

### Legacy outbound mechanism (obsolete, superseded by the above)
- Client abstractions
- Request/response mapping for clients
- Client builder patterns
- Type-safe client interfaces
- `ClientsBuilder`/`SingleClientsBuilder`/`IBenzeneMessageClientFactory`/`IClientMessageRouter` -
  **`[Obsolete]`** - service-name/topic-key client resolution, split by cardinality. Not yet
  deleted (Step 4 of the migration plan) - still fully functional, just superseded.
- `ClientBuilder`/`IDependencyWrapper<IBenzeneMessageClient>`/`DependencyWrapperFactory<T>` -
  **`[Obsolete]`** - decorator-chain pattern for `IBenzeneMessageClient`; existing decorators:
  `CorrelationId/` (`WithCorrelationId()`), `TraceContext/` (`WithW3CTraceContext()` - stamps
  `Activity.Current`'s `traceparent`/`tracestate` onto outgoing headers),
  `HeaderBenzeneMessageClient`/`HeadersBenzeneMessageClient`, `RetryBenzeneMessageClient`

## When to use this package
- When building clients for Benzene services
- For type-safe service communication
- When generating API clients
- Foundation for specific client implementations

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Core.Middleware** - `MiddlewarePipelineBuilder<TContext>`, for `OutboundRoutingBuilder`'s
  per-topic pipelines (added 2026-07-17 for the outbound routing redesign above)

## Important conventions
- Type-safe request/response
- Async client methods
- Transport-agnostic interface
- Used by HTTP and AWS clients

## Tests
- `test/Benzene.Core.Test/Clients/OutboundRoutingBuilderTest.cs` - one pipeline per topic,
  duplicate-topic throws `DuplicateOutboundRouteException`, no routes -> empty table.
- `test/Benzene.Core.Test/Clients/DefaultBenzeneMessageSenderTest.cs` - routes to the right topic's
  pipeline and returns its response, two topics don't cross-contaminate, unrouted topic throws
  `UnroutedTopicException`. Exercises `DefaultBenzeneMessageSender` (internal, no
  `InternalsVisibleTo` wiring in this repo) through `AddOutboundRouting` + the resolved
  `IBenzeneMessageSender` instead of a direct unit test.
- `test/Benzene.Core.Test/Clients/ValidateOutboundRoutingTest.cs` - all required topics routed
  doesn't throw; a missing required topic throws `MissingOutboundRoutesException` naming exactly
  the missing one.
- `test/Benzene.Core.Test/Autogen/CodeGen/Client/MessageClientSdkBuilderTest.cs` - golden-file
  coverage for the generated `{Service}ServiceClient`/`{Service}ServiceClientRouting` output
  (`test/Benzene.Core.Test/Autogen/CodeGen/Client/Examples/*.txt`).
- `test/Benzene.Core.Test/Clients/W3CTraceContextMiddlewareTest.cs` /
  `CorrelationIdMiddlewareTest.cs` - the two outbound cross-cutting middleware directly (an ambient
  `Activity`/`ICorrelationId` value gets stamped onto `OutboundContext.Headers`; no ambient value
  leaves headers unchanged).
- `test/Benzene.Core.Test/Clients/Aws/Sqs/OutboundSqsContextConverterTest.cs` /
  `Aws/Sns/OutboundSnsContextConverterTest.cs` - `.UseSqs(...)`/`.UseSns(...)` routed end to end
  through `AddOutboundRouting` + the resolved `IBenzeneMessageSender`: the mocked AWS client
  receives the right queue/topic, serialized body, `topic` message attribute, and forwarded
  per-call headers; the result maps `HttpStatusCode.OK` to `BenzeneResultStatus.Ok`.
