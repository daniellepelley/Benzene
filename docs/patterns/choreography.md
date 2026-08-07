# Event-Driven Choreography

**Status: DRAFT v0.1 — part of the [Patterns](index.md) "composing a system" set.**

The [two-tier architecture](two-tier-architecture.md) drives processes with **orchestrators**: a
central service issues commands to core services and runs a [saga](orchestrators.md#the-saga-pattern)
to keep the multi-service write atomic. **Choreography** is the other way to move a process across a
fleet: no central conductor — each service **reacts to events** and **emits its own**, and the
process emerges from the chain of reactions.

Both are first-class on Benzene, and a real system uses both. This document is about when to
choreograph, and exactly how you build it with Benzene's events, transports, and mesh.

---

## Orchestration vs. choreography

| | Orchestration ([orchestrators.md](orchestrators.md)) | Choreography (this doc) |
|---|---|---|
| Control | Central — one service directs the process | Distributed — each service decides its own reaction |
| Coupling | Orchestrator knows every step | Emitter knows nothing about who reacts |
| Atomicity | A saga: all-or-nothing across services | None built-in — each reaction succeeds/retries on its own |
| Adding a step | Edit the orchestrator | Add a new consumer; **no existing service changes** |
| Best for | An invariant that must hold across services (a signup that must not half-apply) | Fan-out reactions that are independently retryable (on signup: send a welcome email, warm a cache, start a trial clock) |

The rule of thumb: **orchestrate what must be atomic; choreograph what must merely happen.** A
process usually has both — a small atomic core (orchestrated as a saga) that emits an event, and a
spray of independent reactions (choreographed). Do not use choreography to hold a cross-service
invariant; there is no central rollback, and reconstructing one out of scattered compensations is
exactly the complexity the saga exists to remove.

---

## How you build it with Benzene

### An event is a topic

Choreography needs no new concept. A domain **event** — `tenant:created`, `order:shipped` — is a
[topic](../specification/core-concepts.md#2-topic) like any other, and a reaction to it is an
ordinary [message handler](../specification/core-concepts.md#3-message-handler). The only thing that
makes it an "event" rather than a "command" is intent: nobody is waiting for a result, and the
emitter does not know or care who handles it.

### Emitting an event

An emitter publishes fire-and-forget through the same outbound sender the
[two-tier calls](service-communication.md) use — a topic and a payload, with a `Void` response type
so the caller does not wait:

*(informative, .NET)*

```csharp
// Fire-and-forget: Void response type => no wait, no reply.
await sender.SendAsync<TenantCreated, Void>("tenant:created", new TenantCreated { TenantId = id });
```

Where `tenant:created` goes is a [routing-table](service-communication.md#the-routing-table) entry,
declared once at startup — SNS for fan-out, EventBridge for bus-routed events, SQS for a point-to-
point queue:

```csharp
services.AddOutboundRouting(routing => routing
    .Route("tenant:created", pipeline => pipeline.UseSns(tenantEventsTopicArn))
    // or onto an EventBridge bus, where the topic becomes the event's detail-type:
    .Route("order:shipped",  pipeline => pipeline.UseEventBridge(source: "orders", eventBusName: "domain")));
```

Benzene tags the message so a Benzene consumer can route on it with **no extra configuration**:

- **SNS / SQS** put the topic in the `topic` message attribute.
- **EventBridge** maps the topic to the event's **`detail-type`**, the source to `Source`, and
  embeds Benzene's wire headers (correlation id, `traceparent`) inside `detail` under a reserved
  key — because EventBridge has no native message attributes. The inbound side lifts them back out,
  so correlation and trace context survive the hop.

### Reacting to an event

A reacting service mounts the matching inbound transport and writes a handler for the event topic —
identical in shape to a command handler, because to Benzene it *is* one:

```csharp
app.UseAwsLambda(events => events
    .UseSns(sns => sns.UseMessageHandlers())          // topic from the "topic" attribute
    .UseEventBridge(bus => bus.UseMessageHandlers())); // topic from detail-type

[Message("tenant:created")]
public class SendWelcomeEmailOnTenantCreated : IMessageHandler<TenantCreated>
{
    public async Task<IBenzeneResult> HandleAsync(TenantCreated message) { /* react */ }
}
```

Adding a second reaction is adding a second handler in a second service that subscribes to the same
topic. The emitter is untouched, and never learns the reaction exists — that decoupling is the whole
point of choreographing.

### Events from non-Benzene producers

An event from an AWS service or a non-Benzene producer will not carry Benzene's `topic` attribute.
Two clean options:

- **A queue (or subscription) per event type**, and set the topic for everything on that pipeline
  with `UsePresetTopic("s3:object-created")` before `UseMessageHandlers()` — every message on that
  queue routes to a fixed topic. `UseTopicFrom(ctx => …)` is the dynamic version, computing the
  topic from the payload (a discriminator field, a routing key). *(Preset/derived topics are
  supported on the queue-shaped transports — SQS, Service Bus, Event Hub, Queue Storage, and the
  self-hosted workers; check your transport before relying on it.)*
- **EventBridge** already carries a `detail-type`, so a foreign bus event routes on that detail-type
  directly — declare a handler for it.

---

## Choreography is visible in the mesh — for free

Choreography's classic drawback is that the flow lives nowhere: no orchestrator to read, so "what
reacts to what" is folklore. On Benzene it is **not** folklore. A consumer stamps `traceparent` from
the inbound event's span, and the [mesh](../specification/mesh.md) **derives consumer edges from
that trace parentage** — an event whose parent span belongs to another service makes this service a
consumer of that topic, never declared. Keep [trace-context propagation](../specification/cloud-service-profile.md)
on (it is a Cloud Service Profile requirement) and the choreography graph draws itself in the fleet
view: every emitter, every reaction, observed from real traffic. The thing that makes choreography
hard to see everywhere else, Benzene gives you as a live diagram.

---

## Reliability: at-least-once, so make reactions idempotent

SNS, SQS, and EventBridge are **at-least-once** — a reaction can be delivered more than once (a
redelivery, a retry, a duplicate publish). Choreographed reactions must therefore be **idempotent**:
processing the same event twice must have the same effect as once.

Benzene ships the seam for this — `Benzene.Idempotency`. Add `UseIdempotency()` to a reaction's
pipeline and it derives a key (an `idempotency-key` header if present, else a hash of topic+body),
atomically claims it in a store, and runs the handler only on the first sighting:

```csharp
sns.UseIdempotency().UseMessageHandlers();
```

The in-memory store is single-process; for a fleet of Lambdas you supply an `IIdempotencyStore`
backed by an atomic conditional write (DynamoDB `attribute_not_exists`, Redis `SET NX`). Idempotent
reactions are what make at-least-once safe — and they are the same discipline the
[outbox](transactional-outbox.md) relies on downstream, and that a saga's compensations already
require.

There is no automatic cross-service rollback in choreography. If a reaction fails, it retries (and
eventually dead-letters) **on its own** — the emitter has already moved on. Choreograph reactions
that are safe to retry independently; keep anything that must be undone-as-a-unit inside an
orchestrated saga.

---

## Events are contracts

An emitted event is a published contract other services depend on — evolve it as carefully as a
request type. Version an event's payload with [payload-schema versioning](../specification/versioning.md)
so old consumers keep reading it, and give a breaking event a topic **version** rather than
redefining the old topic under everyone's feet.

---

## Checklist

Choreography is well-formed when:

- [ ] Events are **topics**, emitted fire-and-forget (`Void` response) via the routing table.
- [ ] Emitters know **nothing** about consumers; a new reaction adds a consumer and changes no
      existing service.
- [ ] Reactions are **idempotent** (`UseIdempotency` + a distributed store), because delivery is
      at-least-once.
- [ ] Cross-service **atomic** invariants stay in an [orchestrated saga](orchestrators.md), not in
      choreography.
- [ ] Trace context is propagated, so the **mesh** shows the choreography graph.
- [ ] Events are **versioned** as the contracts they are.

See also: [transactional outbox](transactional-outbox.md) (so the events you choreograph on are
never lost), and [CQRS & read models](cqrs-read-models.md) (a major consumer of domain events).
