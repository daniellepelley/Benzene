# Response Events Plan — `UseResponseEvents`

> **Superseded note (post-implementation):** this plan built `UseResponseEvents` inside
> `Benzene.Extras`. That package was subsequently decommissioned; the response-events code now
> lives in its own **`Benzene.ResponseEvents`** package (references below to
> `Benzene.Extras/ResponseEvents` and `Benzene.Extras.ResponseEvents` are historical). See `CHANGELOG.md`.

## Context

`work/response-as-event-design.md` (2026-07-19) reviewed the transport pipelines' result
mapping (clean: fire-and-forget bindings never emit response bodies) and proposed first-class
support for the *response-as-event* pattern: a request/response handler on a fire-and-forget
transport (e.g. SQS `order:create`) whose response payload is republished as a follow-up event
(`order:created`). The prototype — `Benzene.Extras/Broadcast` — proved the seam but shipped
with no working sender, a hardwired CRUD-verb mapping, unenforced declarations, no header
propagation, and no tests. This plan implements the design's phase 3 (the core feature) with
three explicit qualities: **flexible** (every layer is a replaceable seam), **easy to
configure** (one fluent call per pipeline), **easy to introspect** (mappings are queryable
data, surfaced to DI and spec generation).

## Shape

New folder `src/Benzene.Extras/ResponseEvents/` (namespace `Benzene.Extras.ResponseEvents`).
`Benzene.Extras` gains a project reference to `Benzene.Clients` (internal project ref, no new
NuGet dependency) so the default publisher can ride `IBenzeneMessageSender`.

```csharp
.UseMessageHandlers(router => router
    .UseResponseEvents(events => events
        .Map("order:create", "order:created")                          // successful + payload
        .Map<InvoiceDto>("invoice:submit", "invoice:submitted",
             when: r => r.Status == BenzeneResultStatus.Created)       // typed, conditional
        .MapCrudConvention()                                            // Broadcast parity, opt-in
        .OnPublishFailure(PublishFailureMode.FailMessage)))             // default
```

### Layers (each one a seam)

1. **`IResponseEventMapping`** — one mapping rule: `Resolve(ITopic, IBenzeneResult) →
   ResponseEventPublication?` plus introspection metadata (`Description`, `SourceTopic`,
   `EventTopic`, `PayloadType` — nullable for convention rules). Implementations:
   - `ExplicitResponseEventMapping` — source topic → event topic; default predicate
     *successful and payload non-null*; optional `when` predicate and payload projector.
   - `CrudConventionResponseEventMapping` — the Broadcast rule as data:
     `X:create`/`update`/`delete` + `created`/`updated`/`deleted` → `X:{verb}d`.
   - Anything app-defined via `events.Add(mapping)`.
   Every matching mapping publishes (fan-out is allowed).
2. **`ResponseEventMappings`** — one pipeline's immutable mapping set + `PublishFailureMode`.
   Registered as a DI singleton instance per `UseResponseEvents` call.
3. **`ResponseEventsMiddleware<TRequest,TResponse>`** (via `IHandlerMiddlewareBuilder`) — after
   `next()`, resolves matches against `context.Response` and publishes. The publisher is
   resolved lazily — only when a mapping actually matched (Broadcast resolved eagerly, per
   message, for every message). Failure handling per mode:
   - `FailMessage` (default): overwrite `context.Response` with `unexpected-error` so the
     transport nacks/redelivers — honest at-least-once; stop publishing further matches.
   - `LogAndContinue`: log a warning, keep the handler's response, keep publishing.
4. **`IResponseEventPublisher`** — the outbound port (`PublishAsync(topic, payload, headers?)`).
   Default `BenzeneMessageSenderResponseEventPublisher` sends
   `IBenzeneMessageSender.SendAsync<object, Void>` — so event topics must have an
   `AddOutboundRouting` route (startup-validated), and correlation/W3C-trace outbound
   middleware stamp headers from the same message scope. Registered `TryAddScoped`, so apps
   can swap it (test fake, custom fan-out, future outbox relay).
5. **`ResponseEventCatalog`** (`IResponseEventCatalog`) — app-wide aggregation of every
   pipeline's mappings, for introspection. Also registered as
   `IMessageDefinitionFinder<IMessageDefinition>` (TryAdd), so typed mappings
   (`Map<TPayload>`) flow into AsyncAPI/EventService spec generation exactly where
   `AddBroadcastEvent` declarations did — one declaration drives runtime *and* spec.

### Per-pipeline correctness

`IHandlerPipelineBuilder` is scoped per message and seeded per `UseMessageHandlers` call, so
`UseResponseEvents` inside one pipeline's router configuration affects only that pipeline —
an HTTP pipeline sharing the same handlers is untouched. No context types change (context
purity preserved); no scoped holder is needed since publishing happens inline.

## Steps

1. Implement `src/Benzene.Extras/ResponseEvents/` per the shape above; add the
   `Benzene.Clients` project reference to `Benzene.Extras.csproj`.
2. Tests in `test/Benzene.Core.Test/Extras/ResponseEvents/`:
   - mapping unit tests (explicit/conditional/projector/convention/no-payload guards);
   - end-to-end via `BenzeneMessageApplication` + `UseMessageHandlers(types, router)` +
     `AddOutboundRouting` capture route: publish on match, silence on non-match, both failure
     modes, custom mapping, no-response handler (`accepted`, null payload) never publishes;
   - catalog/introspection + `FindDefinitions` tests.
3. Docs: fix the wrong `UseBroadcastEvent()` description in `docs/reference/middleware.md`
   (design review F4) and add `UseResponseEvents`; cookbook
   `docs/cookbooks/response-as-event.md` for the SQS `order:create` → `order:created`
   scenario; update `src/Benzene.Extras/CLAUDE.md`.
4. `dotnet build Benzene.sln` + full `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj`.

## Explicitly deferred (design doc phases 1/4/5/6)

- Retiring/reimplementing `Broadcast` — ~~left untouched and marked superseded in docs~~ **done
  in a follow-up: `Benzene.Extras/Broadcast` is deleted** (`MapCrudConvention()` +
  `AddResponseEventDeclarations(...)` replace it; see `CHANGELOG.md`).
- F1 startup diagnostic (payload-with-nowhere-to-go warning) — needs the mapping registry
  this plan introduces; follow-up.
- F2 (Event Hub envelope's discarded response serialization) — independent fix.
- Transactional outbox — deferred per `work/enterprise-adoption-gap-analysis.md` D.1; the
  `IResponseEventPublisher` seam is where a relay would slot in.

## Flags

- New project reference `Benzene.Extras` → `Benzene.Clients` (no new external dependency, no
  new project, no solution-structure change).
- Additive public API only; `Benzene.Extras/Broadcast` is not modified. *(Overtaken by the
  follow-up above: Broadcast was subsequently deleted on explicit request.)*
