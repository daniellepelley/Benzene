# CQRS and Read Models

**Status: DRAFT v0.1 — part of the [Patterns](index.md) "composing a system" set.**

The [core services](core-services.md) pattern is deliberately strict: each service owns one or two
aggregate roots, owns its own database (share-nothing), and references other aggregates
[by id only](core-services.md#reference-by-id). That strictness is what makes each service
independent — and it leaves one thing conspicuously unanswered: **how do you query across
aggregates?** "A tenant and all its users", "orders with the customer's name and the product
titles" — no single core service owns that data, and the [directional rule](core-services.md#directional-dependencies)
says the parent must not even know its children exist. `core-services.md` punts that question
explicitly, "above the core layer." This is that layer.

---

## The idea: split reads from writes

**CQRS** — Command Query Responsibility Segregation — is the recognition that the model you write
through and the model you read through want to be *different*. The [core services](core-services.md)
are the **write model**: normalized, authoritative, one aggregate each, optimized for correct writes.
A **read model** is a **separate, derived, denormalized** store shaped for a specific query — a
materialized view assembled from the events the core services emit.

- The write side stays exactly as the two-tier pattern describes — untouched.
- The read side is a new service that **consumes domain events** and **maintains a query-optimized
  projection** it owns, then serves reads from it.

The read model holds no authority; it is a cache of a shape, rebuildable from the events that fed it.
That is what lets it break the core layer's rules safely — it can hold "tenant + its users" together
precisely because it is derived and disposable, not a source of truth.

---

## How you build it with Benzene

A read model is, pleasingly, just another Benzene service — with event handlers on its **write side**
(the projection) and query handlers on its **read side**. Both are ordinary
[message handlers](../specification/core-concepts.md#3-message-handler).

### 1. Consume the domain events and project

Subscribe to the events the core services emit — delivered by [choreography](choreography.md), made
reliable by the [outbox](transactional-outbox.md) — and fold each one into the projection:

*(informative, .NET)*

```csharp
app.UseAwsLambda(events => events
    .UseSqs(sqs => sqs
        .UseIdempotency()          // events are at-least-once; projecting twice must converge
        .UseMessageHandlers()));

[Message("tenant:created")]
public class ProjectTenant : IMessageHandler<TenantCreated>
{
    private readonly IReadStore _view;                        // this service's OWN denormalized store
    public ProjectTenant(IReadStore view) => _view = view;
    public async Task<IBenzeneResult> HandleAsync(TenantCreated e)
    {
        await _view.UpsertTenantAsync(e.TenantId, e.CompanyName);   // idempotent upsert
        return BenzeneResult.Ok();
    }
}

[Message("user:created")]
public class ProjectUserOntoTenant : IMessageHandler<UserCreated>
{
    private readonly IReadStore _view;
    public ProjectUserOntoTenant(IReadStore view) => _view = view;
    public async Task<IBenzeneResult> HandleAsync(UserCreated e)
    {
        await _view.AddUserToTenantAsync(e.TenantId, e.UserId, e.Email);  // the join the write side can't do
    }
}
```

The projection service is **still share-nothing** — it owns its read store, no one else touches it.
What it relaxes is the *shape*: it deliberately co-locates data from several aggregates (`tenant` and
`user`) that no core service may hold together. It earns that by being derived, not authoritative.

### 2. Serve the queries

The read side is ordinary query handlers over the projection, exposed on whatever transport the
readers use — HTTP via `UseApiGateway`, or the wire envelope:

```csharp
[Message("tenant:users:list")]
[HttpEndpoint("GET", "/tenants/{tenantId}/users")]
public class ListTenantUsers : IMessageHandler<ListTenantUsers, TenantUsersView>
{
    private readonly IReadStore _view;
    public ListTenantUsers(IReadStore view) => _view = view;
    public Task<IBenzeneResult<TenantUsersView>> HandleAsync(ListTenantUsers q)
        => _view.GetTenantWithUsersAsync(q.TenantId);   // one read, no fan-out
}
```

"A tenant and all its users" is now a single indexed read against a store shaped for exactly that
question — instead of the runtime fan-out (ask the tenant service, ask the user service, stitch) an
[orchestrator/BFF](service-communication.md) would otherwise do per request. You have moved the join
from **query time** to **event time**, and paid for it once, when the data changed, rather than on
every read.

### 3. Rebuild by replay

Because a read model is *derived*, it is **disposable and rebuildable**: replay the domain events
(re-consume from the source topic/stream, or from a snapshot) and the projection reconstructs itself.
This is why projections must be written as **idempotent** folds — `UpsertTenant`, `AddUserToTenant`,
not `IncrementCount` — so reprocessing an event, whether from an at-least-once redelivery or a full
rebuild, **converges to the same state**. `UseIdempotency()` guards against duplicates; idempotent
upserts make replay a routine operation (fix a projection bug, add a field, spin up a new view) rather
than a migration.

---

## Consistency: eventual, and that's the trade

A read model **lags** the write model by the event's propagation time — it is **eventually
consistent**. Right after `tenant:create` returns, the tenant may not yet be in the read view; a
moment later it is. That is the deal you accept for cheap cross-aggregate reads and independent
read-side scaling, and for most queries it is completely fine.

Where it is *not* fine — a screen that must show a user their just-committed write immediately —
**read that path from the core service directly** (it is the authority and is always current), and
use the read model for the cross-aggregate and high-volume queries where a second of lag doesn't
matter. Choosing per-query which side to read is the everyday CQRS decision; don't route *everything*
through the read model reflexively.

---

## How the trilogy composes

The three "composing a system" patterns are one pipeline:

```
  core service writes  ──►  transactional outbox  ──►  event (choreography)  ──►  read model projects
   (authoritative)          (reliable emission)        (SNS/EventBridge)          (denormalized view)
```

- The [outbox](transactional-outbox.md) guarantees the read model **never misses** an event.
- [Choreography](choreography.md) delivers events without the emitter knowing the read model exists —
  so you can add a new view without touching any core service.
- CQRS turns that event stream into **fast, cross-aggregate reads** the share-nothing write side
  cannot serve.

---

## When to use it

- **Use it** for cross-aggregate queries, denormalized views, expensive aggregations/reports, and
  when read load and write load want to scale differently.
- **Don't** build a read model for a query a single core service already answers from its own data —
  that is just a read topic on that service. Reach for CQRS when the query spans aggregates or the
  read shape genuinely diverges from the write shape; not before.

---

## Checklist

A read model is well-formed when:

- [ ] It is a **separate, derived** store — never a second writer to a core service's database.
- [ ] It **projects domain events** into a shape built for a specific query.
- [ ] Projections are **idempotent folds**, safe under at-least-once delivery and full **replay**.
- [ ] It relies on the [outbox](transactional-outbox.md) so it **never misses** an event.
- [ ] Queries choose **read-model (eventual) vs core-service (current)** per path, deliberately.
- [ ] It stays **share-nothing** — it owns its read store; nobody else touches it.

See also: [choreography](choreography.md) (how the events reach it) and
[transactional outbox](transactional-outbox.md) (why it can trust them).
