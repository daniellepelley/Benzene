# Benzene.ResponseEvents

## What this package does
Republishes a request/response handler's response payload as a follow-up event on fire-and-forget
transports — the *response-as-event* pattern. A handler on a queue transport (e.g. SQS
`order:create`) returns a payload the transport can't deliver; a per-pipeline mapping publishes it
as an event (`order:created`) instead. Design: `work/response-as-event-design.md`; usage:
`docs/cookbooks/response-as-event.md`.

This was originally built inside the now-deleted `Benzene.Extras` grab-bag package and promoted to
its own package when Extras was decommissioned (it's a genuine framework capability, unlike the
specialized bits Extras also held).

## Key types/interfaces
- `UseResponseEvents(events => ...)` (`ResponseEventsExtensions`) - the one registration call, on
  `IMessageRouterBuilder` inside a pipeline's `UseMessageHandlers(router => ...)`. Scoped to that
  pipeline only (handler middleware builders are per-`UseMessageHandlers` call; the
  `IHandlerPipelineBuilder` is scoped per message), so an HTTP pipeline sharing the same handlers
  is unaffected.
- `ResponseEventsBuilder` - fluent config: `Map(source, event, when?)`,
  `Map<TPayload>(source, event, when?, project?)` (declares the payload type for spec generation;
  projector may reshape or return `null` to skip), `MapCrudConvention()`, `Add(custom mapping)`,
  `OnPublishFailure(mode)`.
- `IResponseEventMapping` - one rule: `Resolve(ITopic, IBenzeneResult) → ResponseEventPublication?`
  plus introspection metadata (`Description`, `SourceTopic`/`EventTopic`/`PayloadType`, nullable
  for convention rules). Implementations: `ExplicitResponseEventMapping` (default predicate:
  successful + non-null payload), `CrudConventionResponseEventMapping`
  (`X:create`+`Created` → `X:created`, etc.). Every matching mapping publishes (fan-out allowed).
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
  `IMessageDefinitionFinder<IMessageDefinition>` so both appear in generated AsyncAPI/event-service
  specs.
- `AddResponseEventDeclarations(params IMessageDefinition[])` (container extension) -
  declaration-only published events: for topics handler code sends directly via
  `IBenzeneMessageSender`, so they still show up in specs and the catalog with no runtime
  republishing (used by the AwsMesh example for topology edges). `ResponseEventDefinition` is the
  `IMessageDefinition` for an (event topic, payload type) pair; it has an `(ITopic, Type)` overload
  for versioned topics.
- **Unmapped-response diagnostic (F1).** `IResponseEventMapping.Covers(ITopic)` (default interface
  method; CRUD convention overrides) is the static coverage predicate;
  `IResponseEventCatalog.CoversTopic(ITopic)` aggregates it. `ResponseEventDiagnosticsExtensions`
  adds opt-in `IServiceResolver.FindUnmappedResponseHandlers()` (→ `ResponseEventGap[]`) and
  `LogUnmappedResponseHandlers(ILogger?)` - they enumerate `IMessageHandlersFinder` definitions,
  keep the response-returning ones (`ResponseType != Void`) whose topic no mapping covers, and
  report them. **Advisory, never throws** - handlers are transport-agnostic, so a response is
  correct on HTTP and dropped on SQS; the diagnostic can't know intent, so it lists candidates for
  the developer to triage. Mirrors `Benzene.Clients`' `ValidateOutboundRouting` (opt-in, call once
  after wiring).

## When to use this package
- To republish handler responses as events on fire-and-forget transports (`UseResponseEvents`)
- To declare published-event contracts for spec generation (`AddResponseEventDeclarations`)

## Dependencies on other Benzene packages
- **Benzene.Abstractions.MessageHandlers / .Messages** - handler + message-definition abstractions
- **Benzene.Core.MessageHandlers / Benzene.Core.Messages** - handler pipeline integration, `Topic`
- **Benzene.Clients** - `IBenzeneMessageSender`, used by the default publisher

## Important conventions
- Mappings are immutable data built at configure time - keep them introspectable (populate
  `Description`/`SourceTopic`/`EventTopic`/`PayloadType` on new mapping types) so
  `IResponseEventCatalog` and spec generation stay truthful.
- Publishing rides the outbound pipeline (`IBenzeneMessageSender`), never a bespoke send path -
  that's what keeps correlation/trace propagation and route validation working.
- Context purity: the feature never touches transport contexts; per-message state lives in the
  handler-middleware scope (see `Benzene.Abstractions.Middleware/CLAUDE.md`).
- History: the pre-1.0 `Benzene.Extras/Broadcast` mechanism (`UseBroadcastEvent()`/`IEventSender`,
  hardwired CRUD mapping, no shipped sender) was removed; `MapCrudConvention()` +
  `AddResponseEventDeclarations` cover its use cases. Don't reintroduce it.
