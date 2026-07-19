# Benzene.Extras

## What this package does
Grab-bag of optional, transport-agnostic message-handling extras layered on top of
`Benzene.Core.MessageHandlers`: response-event republishing, JSON-patch message support, inline
media formats, response building helpers, and raw/base64 result payload wrappers.

## Key types/interfaces

### ResponseEvents (`ResponseEvents/`) — republish a handler's response as a follow-up event
The supported implementation of the *response-as-event* pattern (design:
`work/response-as-event-design.md`; usage: `docs/cookbooks/response-as-event.md`): a
request/response handler on a fire-and-forget transport (e.g. SQS `order:create`) returns a
payload the transport can't deliver; a per-pipeline mapping publishes it as an event
(`order:created`) instead.

- `UseResponseEvents(events => ...)` (`ResponseEventsExtensions`) - the one registration call, on
  `IMessageRouterBuilder` inside a pipeline's `UseMessageHandlers(router => ...)`. Scoped to that
  pipeline only (handler middleware builders are per-`UseMessageHandlers` call; the
  `IHandlerPipelineBuilder` is scoped per message).
- `ResponseEventsBuilder` - fluent config: `Map(source, event, when?)`,
  `Map<TPayload>(source, event, when?, project?)` (declares the payload type for spec generation;
  projector may reshape or return `null` to skip), `MapCrudConvention()`, `Add(custom mapping)`,
  `OnPublishFailure(mode)`.
- `IResponseEventMapping` - one rule: `Resolve(ITopic, IBenzeneResult) → ResponseEventPublication?`
  plus introspection metadata (`Description`, `SourceTopic`/`EventTopic`/`PayloadType`, nullable
  for convention rules). Implementations: `ExplicitResponseEventMapping` (default predicate:
  successful + non-null payload), `CrudConventionResponseEventMapping`
  (`X:create`+`Created` → `X:created`, etc. - the old Broadcast behavior as an opt-in rule).
  Every matching mapping publishes (fan-out allowed).
- `ResponseEventsMiddleware<TRequest,TResponse>` - handler middleware; runs after `next()`,
  resolves the publisher **lazily** (only when a mapping matched). Failure per
  `PublishFailureMode`: `FailMessage` (default) replaces the response with `UnexpectedError` so
  queue transports nack/redeliver (at-least-once - handlers/consumers must be idempotent);
  `LogAndContinue` logs and keeps the handler's response.
- `IResponseEventPublisher` - outbound port; default
  `BenzeneMessageSenderResponseEventPublisher` sends `SendAsync<object, Void>` via
  `IBenzeneMessageSender`, so every event topic needs an `AddOutboundRouting` route and the
  route's middleware (correlation, W3C trace, retry) applies. Registered `TryAddScoped` - swap it
  for a test fake or an outbox relay.
- `IResponseEventCatalog` / `ResponseEventCatalog` - app-wide introspection: aggregates every
  pipeline's registered `ResponseEventMappings` singletons plus any
  `AddResponseEventDeclarations(...)` declaration-only definitions; also an
  `IMessageDefinitionFinder<IMessageDefinition>` so both appear in generated
  AsyncAPI/event-service specs.
- `AddResponseEventDeclarations(params IMessageDefinition[])` (container extension) -
  declaration-only published events: for topics handler code sends directly via
  `IBenzeneMessageSender`, so they still show up in specs and the catalog with no runtime
  republishing (used by the AwsMesh example for topology edges).
- History: the pre-1.0 `Broadcast/` folder (`UseBroadcastEvent()`/`IEventSender`, hardwired CRUD
  mapping, no shipped sender) was **removed**; `MapCrudConvention()` +
  `AddResponseEventDeclarations` cover its two use cases. Don't reintroduce it.

### Other
- `Patches/` - `IPatchMessage`/`PatchMessage` + `PatchExtensions`: partial-update (patch) message
  support
- `Request/InlineMediaFormat` - register an inline `IMediaFormat<TContext>` without a class
- `Response/ResponseBuilder` - fluent construction of response objects
- `Results/RawJsonMessage`, `Results/Base64JsonMessage` - result payload wrappers for raw JSON /
  base64-encoded JSON bodies
- `Constants.cs` - shared constants

## When to use this package
- To republish handler responses as events on fire-and-forget transports (`UseResponseEvents`)
- For patch-style message handling or raw/base64 payload results

## Dependencies on other Benzene packages
- **Benzene.Abstractions.MessageHandlers / .Messages / .Pipelines** - abstractions
- **Benzene.Core.MessageHandlers / Benzene.Core.Messages** - handler pipeline integration
- **Benzene.Clients** - `IBenzeneMessageSender`, used by the default response-event publisher

## Important conventions
- ResponseEvents mappings are immutable data built at configure time - keep them introspectable
  (populate `Description`/`SourceTopic`/`EventTopic`/`PayloadType` on new mapping types) so
  `IResponseEventCatalog` and spec generation stay truthful
- Publishing rides the outbound pipeline (`IBenzeneMessageSender`), never a bespoke send path -
  that's what keeps correlation/trace propagation and route validation working
- Context purity: the feature never touches transport contexts; per-message state lives in the
  handler-middleware scope (see `Benzene.Abstractions.Middleware/CLAUDE.md`)
