# Fire-and-Forget Responses & Response-as-Event — Transport Pipeline Design Review

**Date:** 2026-07-19
**Status:** Part 2's core (§2.3 step 3) is implemented as `UseResponseEvents`, per
`docs/plans/response-events-plan.md`, and the F4 docs bug is fixed. `Benzene.Extras/Broadcast`
has since been **deleted** (breaking; `MapCrudConvention()` + `AddResponseEventDeclarations(...)`
replace it), resolving F3. The whole `Benzene.Extras` grab-bag package was then decommissioned:
the response-events code moved to its own new **`Benzene.ResponseEvents`** package (namespaces in
this doc that say `Benzene.Extras.ResponseEvents` are historical) and the rest was abandoned — see
`docs/migration-alpha-to-1.0.md`. **The F1 diagnostic (D6) is now implemented** — opt-in
`IServiceResolver.FindUnmappedResponseHandlers()` / `LogUnmappedResponseHandlers(...)`, advisory
per the design. The F2 Event Hub fix and the outbox remain open as listed in §2.3.
**Scope:** (1) audit of every transport binding's result mapping, verifying that fire-and-forget
transports do not carry or emit response payloads; (2) a design proposal for first-class support
of the *response-as-event* pattern — a request/response message handler on a fire-and-forget
transport whose response payload is republished as a follow-up event (e.g. SQS `order:create`
returns an `OrderCreated` payload that middleware broadcasts as `order:created`).

Related docs: `docs/specification/transport-bindings.md` (binding contract §1.5),
`work/request-response-design-review.md` (response serialization machinery — different scope),
`work/enterprise-adoption-gap-analysis.md` D.1 (transactional outbox — deferred).

---

## Part 1 — Review: do fire-and-forget transports have responses?

### 1.1 The design as it stands

`MessageRouter<TContext>` (`src/Benzene.Core.MessageHandlers/MessageRouter.cs:118-119`) invokes
every handler as `IMessageHandler<TRequest, TResponse>` and hands the full
`IMessageHandlerResult` (topic + definition + `IBenzeneResult` with payload) to the transport's
registered `IMessageHandlerResultSetter<TContext>`. The setter is the **only** point where
"does this transport deliver a response?" is decided. Three tiers exist:

| Tier | Base / pattern | Payload handling | Transports |
|---|---|---|---|
| Response-writing | `ResponseMessageHandlerResultSetterBase` (→ `IResponseHandlerContainer` → renderers) | Serialized as response body | BenzeneMessage, AspNet.Core, Azure.Function.AspNet, Aws.Lambda.ApiGateway, SelfHost.Http; gRPC (custom: `ResponseAsObject` → protobuf) |
| Ack-only | `MessageHandlerResultSetterBase` → `IHasMessageResult.MessageResult = new MessageResult(IsSuccessful)`; or a custom bool flag (`SqsMessageContext.IsSuccessful`, DynamoDb) | **Dropped** — only success/failure drives ack/nack/partial-batch-failure | Aws.Lambda.{Sqs, Sns, S3, EventBridge, Kafka, DynamoDb}, Aws.Sqs (self-hosted), Azure.Function.{ServiceBus, EventGrid, QueueStorage, Kafka}, Azure.ServiceBus, Azure.EventHub, Kafka.Core, RabbitMq, GoogleCloud PubSub |
| No setter | Checkpoint/stream semantics | No payload path at all | Aws.Lambda.Kinesis (checkpointer + `KinesisBatchResponse`), Azure.Function.EventHub trigger |

**Verdict: the design is sound and the implementations conform.** No fire-and-forget binding
serializes a response body onto its native transport, matching the binding contract
(`transport-bindings.md` §1.5: "queue/event transports define their acknowledge/reject behavior
instead"). `MessageHandlerNoResultWrapper` (`src/Benzene.Core.MessageHandlers/
MessageHandlerNoResultWrapper.cs:36-40`) is also clean: a no-response `IMessageHandler<TRequest>`
is adapted by returning `BenzeneResult.Accepted<TResponse>()` with a default payload — nothing
fabricated. Docs (`docs/message-result.md`, per-package CLAUDE.md files) consistently describe
queue transports as fire-and-forget.

### 1.2 Findings

**F1 — A request/response handler on a fire-and-forget transport silently computes a payload
that is then dropped, with no guard or diagnostic anywhere.**
Nothing in discovery (`ReflectionMessageHandlersFinder`), creation
(`MessageHandlerFactory.CreateMessageHandlerByType`), or transport registration
(`AddSqs()`/`AddSns()`/…) cross-checks a handler's response-ness against the transport's
result-setter tier. Registering `IMessageHandler<CreateOrder, OrderCreated>` on SQS works, the
handler's payload reaches `SqsMessageHandlerResultSetter` — and evaporates
(`SqsMessageHandlerResultSetter.cs:21` reads only `IsSuccessful`). This is not a correctness bug
(the binding contract is honored) but it is a **silent semantic dead end**: the developer wrote a
response for a transport that can never deliver one, and the framework neither uses it, warns,
nor documents what happened. Part 2's proposal both legitimizes this pairing (the response
becomes an event) and adds the diagnostic for the unmapped case.

**F2 — The Azure Function Event Hub envelope path pays for a response body it then discards.**
`UseBenzeneMessage` on `EventHubContext` routes through a nested `BenzeneMessageApplication`,
whose pipeline uses the response-writing `BenzeneMessageHandlerResultSetter` — so headers,
status, and a **serialized body** are produced per message. But
`BenzeneMessageEventHubHandler.HandleFunction`
(`src/Benzene.Azure.Function.EventHub/Function/BenzeneMessageEventHubHandler.cs:54-57`) discards
the returned `IBenzeneMessageResponse` — Event Hub has no reply channel. Wasted serialization on
a hot batch path. Fix independently of Part 2: let the envelope application be composed with an
ack-only result setter when hosted on a one-way transport.

**F3 — The existing `Benzene.Extras/Broadcast` feature is the response-as-event prototype, and
it is underbaked.** `BroadcastEventMiddleware<TRequest, TResponse>`
(`src/Benzene.Extras/Broadcast/BroadcastEventMiddleware.cs`) is a handler-pipeline middleware
(`IHandlerMiddlewareBuilder` seam) that, after `next()`, publishes `context.Response.Payload` to
a derived topic. It proves the seam is right, but as shipped:

1. **Its outbound port has no implementation.** `IEventSender` (`SendAsync<T>(topic, payload)`)
   has no concrete implementation anywhere in the repo; the middleware resolves it eagerly —
   before `next()`, for every message on the pipeline — so adding `UseBroadcastEvent()` without
   hand-writing an `IEventSender` fails at runtime. The feature cannot work out of the box.
2. **The command→event mapping is hardwired**: topic verb (last `:`-segment) must be
   `create`/`update`/`delete`, result status must be exactly `Created`/`Updated`/`Deleted`, and
   the event topic is the string trick `$"{Topic.Id}d"` (`order:create` → `order:created`). Any
   other verb pair (`order:submit` → `order:submitted`) is inexpressible.
3. **Declared contracts are not enforced at runtime.** `AddBroadcastEvent(definitions)` registers
   `BroadcastEventChecker`, but the middleware never calls `Check(topic, payload)` — definitions
   feed only spec generation (`AsyncApiDocumentBuilder`/`EventServiceDocumentBuilder` via
   `IMessageDefinitionFinder`). Runtime mapping and declared topology can silently disagree.
4. **No headers flow.** `IEventSender.SendAsync(topic, payload)` has no headers parameter and
   bypasses the outbound pipeline, so `.UseCorrelationId()` / `.UseW3CTraceContext()` stamping
   never happens unless the app's own `IEventSender` routes through `IBenzeneMessageSender` —
   breaking end-to-end trace propagation by default, exactly the property
   `transport-bindings.md` §2 "Outbound clients" says clients MUST provide.
5. **No runtime tests** exercise the middleware (only spec-generation tests touch Broadcast
   types), and the reference docs describe a different feature entirely (F4).

**F4 — Documentation bug.** `docs/reference/middleware.md:275-283` describes
`UseBroadcastEvent()` as "broadcasts an event message to multiple matching handlers … in-process
fan-out". That is not what the code does (it republishes a handler's response outward). Needs
rewriting whichever way Part 2 lands.

---

## Part 2 — Proposal: first-class response-as-event

### 2.1 Target experience

```csharp
// SQS worker: order:create is a request/response handler; its response is the event payload.
[Message("order:create")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, OrderCreated>
{
    public async Task<IBenzeneResult<OrderCreated>> HandleAsync(CreateOrderRequest request)
    {
        var order = await _orders.CreateAsync(request);
        return BenzeneResult.Created(new OrderCreated(order.Id, order.Total));
    }
}

// Wiring: declarative mapping, published through the normal outbound pipeline.
app.UseSqs(sqs => sqs
    .UseMessageHandlers(router => router
        .UseResponseEvents(events => events
            .Map("order:create", "order:created")            // on success-with-payload
            .Map("invoice:submit", "invoice:submitted",
                 when: r => r.Status == BenzeneResultStatus.Created))));

services.AddOutboundRouting(routing => routing
    .Route("order:created", pipeline => pipeline
        .UseCorrelationId()
        .UseW3CTraceContext()
        .UseSns(snsConfig)));                                  // or EventBridge, Kafka, …
```

The handler stays a pure request/response handler — testable, reusable on HTTP where the same
response *is* the reply body. The transport pipeline decides what "response" means: reply body
on request/response transports, follow-up event on fire-and-forget ones. This is the same
philosophy as the result/status split (`docs/message-result.md`): the handler expresses intent
once; bindings translate it.

### 2.2 Design decisions

**D1 — Keep the handler-middleware seam (`IHandlerMiddlewareBuilder`).** Evaluated seams:

- *Typed handler middleware* (what Broadcast uses): runs after `next()` with the **typed**
  `IMessageHandlerContext<TRequest, TResponse>` — sees `Response.Payload` without boxing
  ceremony, has `Topic`, runs inside the message's DI scope, is registered per
  `UseMessageHandlers` call so it is naturally **per-pipeline** (only the queues that opt in
  publish events), and is fully transport-agnostic. **Chosen.**
- *`IMessageHandlerResultSetter` decorator*: untyped (`PayloadAsObject`), must be registered per
  context type app-wide (result setters are shared DI registrations, not per-pipeline — the same
  problem `PresetTopicHolder` exists to avoid), and runs on routing-failure paths too. Rejected.
- *Per-transport application changes* (SqsApplication et al.): N implementations of one concern.
  Rejected.
- *Handler calls `IBenzeneMessageSender` itself* (status quo, cf.
  `examples/Aws/…/PublishOrderCreatedMessageHandler.cs`): remains the right answer when the
  event isn't simply the response (multiple events, conditional payload shaping) — the proposal
  is additive, not a replacement.

**D2 — Publish through `IBenzeneMessageSender`, not a parallel port.** The republish is an
ordinary outbound send and must ride the outbound pipeline so that:
correlation/trace middleware stamps headers (Broadcast gap 4); the event topic's route is
declared in `AddOutboundRouting` and startup-validated (`ValidateOutboundRoutingExtensions`,
`UnroutedTopicException` semantics); and the transport choice (SNS, EventBridge, Kafka…) stays
where it already lives. `IEventSender` is retired — or kept as a thin shipped adapter over
`IBenzeneMessageSender` for back-compat during the pre-1.0 window.

**D3 — Mapping is explicit data, with the CRUD convention as opt-in sugar.** The registration
above builds an immutable map `(source topic, optional status/result predicate) → event topic
(+ optional payload projector)`. `MapCrudConvention()` reproduces today's
create→created/update→updated/delete→deleted behavior for those who want it. Default predicate:
result is successful **and** payload is non-null (an `Accepted` from a no-response handler never
publishes — preserving `MessageHandlerNoResultWrapper` semantics). The mapping registration also
implements `IMessageDefinitionFinder<IMessageDefinition>`, so **one declaration** feeds both the
runtime mapping and AsyncAPI/EventService spec generation — collapsing the Broadcast
declaration/runtime split (gap 3) instead of enforcing it with a runtime `Check`.

**D4 — Failure semantics: publish-inside-the-invocation, fail the message on publish failure.**
The middleware publishes after the handler succeeds, within the same pipeline invocation and DI
scope. If the publish throws or returns a failed result, the middleware overwrites
`context.Response` with a failure result → the transport's result setter reports failure → SQS
(et al.) redelivers → the handler re-runs. This is honest **at-least-once**: consumers of
`order:created` and the handler itself must be idempotent, and the doc for the feature must say
so. An opt-out `PublishFailure.LogAndContinue` mode covers fire-and-lose-tolerant cases.
A transactional outbox (exactly-once-ish handler-side dedup + relay) remains explicitly deferred
per `work/enterprise-adoption-gap-analysis.md` D.1 — this feature's seam (a mapping + an
`IBenzeneMessageSender` call) is exactly the surface an outbox relay would later slot behind, so
nothing here forecloses it.

**D5 — Context purity is preserved.** No context type changes. The middleware needs no
cross-step handoff at all in the common case (it publishes inline); if a later need arises
(e.g. a transport wanting to know "an event was published for this message"), the sanctioned
mechanism is a scoped DI holder per the `PresetTopicHolder` pattern
(`src/Benzene.Abstractions.Middleware/CLAUDE.md`, "Context purity"), not a context property.

**D6 — Close the F1 gap with a diagnostic, not a prohibition.** With response-as-event in
place, "request/response handler on a fire-and-forget transport" splits into two cases:
*mapped* (the payload has a destination — fine) and *unmapped* (the payload is dropped). Add a
startup-time check (alongside the existing `RegistrationCheck` diagnostics) or a log-once
warning: handler `X` on transport `Y` returns a response, `Y` cannot deliver responses, and no
response-event mapping covers topic `T` — the payload will be discarded. Warn, don't fail:
sharing one handler assembly across HTTP and SQS hosts is legitimate.

### 2.3 Sequencing

1. **Fix F4 + tests** (small): correct `docs/reference/middleware.md`; add runtime tests around
   the existing Broadcast middleware to pin current behavior before changing it.
2. **Ship the missing sender bridge**: default `IEventSender` adapter over
   `IBenzeneMessageSender`; resolve it lazily (only when a mapping matches). Unblocks Broadcast
   as-is without breaking changes.
3. **`UseResponseEvents` mapping registration** (the core of this proposal): explicit map +
   status predicates + CRUD-convention sugar; single declaration feeding runtime and spec
   generation; publish via `IBenzeneMessageSender`; `PublishFailure` modes. Supersedes
   `UseBroadcastEvent()` — acceptable pre-1.0 breaking change, with `UseBroadcastEvent()`
   reimplemented as `UseResponseEvents(e => e.MapCrudConvention())` during transition.
4. **F1 diagnostic** (D6) once mappings exist to check against.
5. **F2 fix** independently: ack-only result setter option for the nested
   `BenzeneMessageApplication` on one-way hosts.
6. **Outbox**: stays deferred (D.1); revisit after adoption feedback.

### 2.4 Worked example (the motivating scenario)

SQS message `order:create` → `SqsApplication` dispatches one record → `MessageRouter` invokes
`CreateOrderHandler` → returns `Created(OrderCreated)` → response-events middleware matches
`order:create` → `Map` → sends `OrderCreated` to topic `order:created` via
`IBenzeneMessageSender` → outbound routing's pipeline stamps `correlationId` + `traceparent`
and publishes to SNS/EventBridge → `SqsMessageHandlerResultSetter` records success → the record
is acked. On publish failure: the record is reported in `BatchItemFailures`, SQS redelivers,
the (idempotent) handler re-runs. On HTTP, the identical handler returns the payload as a `201`
response body and the mapping middleware — registered only on the SQS pipeline — never runs.

---

## Bottom line

The fire-and-forget half of the review is a clean bill of health: every queue/event binding maps
results to ack/nack/checkpoint semantics only, exactly as the binding contract requires — the
gaps are a silently-dropped payload with no diagnostic (F1), one wasted response serialization
(F2), and an aspirational-but-unfinished Broadcast feature (F3/F4). The response-as-event
pattern the framework should support already has the right seam proven in-tree; what it needs is
to be finished properly: explicit declarative topic mapping instead of a hardwired string trick,
publishing through the real outbound pipeline so routing validation and correlation/trace
propagation come for free, one declaration driving both runtime and AsyncAPI specs, honest
at-least-once failure semantics, and a diagnostic that tells developers when a response payload
has nowhere to go.
