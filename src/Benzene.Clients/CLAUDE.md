# Benzene.Clients

## What this package does
Client abstractions for calling Benzene services. Provides interfaces and base implementations for building type-safe clients that communicate with Benzene HTTP endpoints and message handlers.

## Key types/interfaces

### Outbound routing (current, 2026-07-17)
The redesigned outbound mechanism from `work/benzene-clients-redesign-plan.md` - a single
topic-keyed pipeline table replacing the service-name/topic-key factory + decorator-chain shape
below. See that document for the full design and the 4-step migration plan (this is Step 1: add
the new mechanism alongside the old, non-breaking).
- `IBenzeneMessageSender` - the one interface business logic depends on:
  `SendAsync<TRequest,TResponse>(topic, request)`. No service name, no client type, no factory
  resolution at the call site.
- `OutboundContext` - the outbound pipeline context: `Topic`, `Request`, and a settable `Response`
  slot. Deliberately non-generic, matching every other `IMiddleware<TContext>` in this codebase.
- `OutboundRoutingBuilder` / `AddOutboundRouting(...)` - builds one `IMiddlewarePipeline<OutboundContext>`
  per topic via `.Route(topic, pipeline => ...)`; `Build()` throws `DuplicateOutboundRouteException`
  for a repeated topic. `AddOutboundRouting(...)` registers the resulting `IBenzeneMessageSender`.
- `DefaultBenzeneMessageSender` (internal) - resolves a topic to its pipeline and runs it; throws
  `UnroutedTopicException` for a topic with no registered route.
- Transport-specific route-registration extensions (`.UseSqs(...)`/`.UseSns(...)`/etc. on the
  outbound pipeline builder) and the middleware-ified decorators (retry/headers/correlation/trace
  context) are **not yet implemented** - still Step 3 of the migration plan; for now, `.Route(...)`
  pipelines are built with whatever `IMiddleware<OutboundContext>` exists at the time.

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
