# Core Services

**Status: DRAFT v0.1 — part of the [two-tier pattern](two-tier-architecture.md).**

A **core service** owns a slice of the domain's *data*. It is the system of record for one or two
aggregate roots, and almost nothing else. Core services are the foundation the
[orchestrators](orchestrators.md) build processes on top of.

The defining trait is restraint: a core service is **light on process and heavy on data**. It
does CRUD — create, read, update, delete — with validation, over a database it alone owns. It does
not run business processes, does not call other core services, and does not know why it is being
called. That discipline is what makes the whole fleet composable.

---

## What a core service is

- **Owns one or two aggregate roots.** An aggregate root is the entry point to a cluster of data
  that changes together and is consistent together (a `Tenant`, an `Order` with its lines, a
  `User`). One service, one small handful of roots — the part of the business domain this service
  is the authority on.
- **Owns its own database — share-nothing.** The service is the *only* code that touches its
  store. No other service reads or writes those tables. This is the single most important rule:
  it is what lets a core service be deployed, scaled, migrated, and reasoned about on its own.
- **Backed by a database, always.** A core service is persistent by definition; it is where the
  data lives. (A stateless computation is not a core service — it is middleware, or an
  orchestrator step.)
- **Validates its own objects.** Every write topic validates its request before it touches the
  store, and returns a `validation` result on failure rather than throwing
  ([wire-contracts.md](../specification/wire-contracts.md) status vocabulary). The service is
  the last line of defence for its aggregate's invariants.
- **Holds minimal business logic and no cross-service process.** If logic spans two aggregates, it
  is not a core service's job — it belongs in an orchestrator.

Think of a core service as a well-guarded table (or small set of tables) with a typed, validated,
topic-addressed API in front of it.

---

## The CRUD surface

A core service exposes its aggregate as a small, predictable set of **topics**
([core-concepts.md](../specification/core-concepts.md#2-topic)), one per operation. The steer
is a consistent naming shape across the fleet — `aggregate:operation` — so that every service in
the estate reads the same way:

| Operation | Example topic | Request → Result |
|---|---|---|
| Create | `tenant:create` | `CreateTenant` → `TenantCreated` (or `validation`) |
| Read | `tenant:get` | `GetTenant` (by id) → `Tenant` (or `not-found`) |
| Update | `tenant:update` | `UpdateTenant` → `TenantUpdated` (or `validation` / `not-found`) |
| Delete | `tenant:delete` | `DeleteTenant` (by id) → `Deleted` (or `not-found`) |
| List / query | `tenant:list` | `ListTenants` (filter) → `TenantPage` |

Each topic is served by a message handler — `handle : TRequest -> Result<TResponse>`
([core-concepts.md](../specification/core-concepts.md#3-message-handler)). Because the topics
and their request/response types are registered in the handler registry, the service's **spec is
derived**, not hand-written ([Cloud Service Profile R5](../specification/cloud-service-profile.md)):
the CRUD surface documents itself, and clients can be generated from it.

*(informative, .NET)* A create handler is an ordinary handler with validation in front of it:

```csharp
[Message("tenant:create")]
public class CreateTenantHandler : IMessageHandler<CreateTenant, TenantCreated>
{
    private readonly ITenantStore _store;   // this service's own database, nobody else's
    public CreateTenantHandler(ITenantStore store) => _store = store;

    public async Task<IBenzeneResult<TenantCreated>> HandleAsync(CreateTenant message)
    {
        var tenant = Tenant.New(message.CompanyName);
        await _store.InsertAsync(tenant);
        return BenzeneResult.Ok(new TenantCreated { TenantId = tenant.Id });
    }
}
```

Validation is a middleware step in front of the handler (e.g. FluentValidation), so an invalid
`CreateTenant` short-circuits to a `validation` result and never reaches the store — see the
FluentValidation/DataAnnotations integrations in the language port. The handler itself stays a
clean function of request-to-result.

---

## Reference by id

When one aggregate needs to point at another, it stores the **id** of the other aggregate — never
an embedded copy, and never a foreign-key join across service boundaries (there is no shared
database to join in).

> A `User` belongs to a `Tenant`. The `User` aggregate carries a `tenantId: string`. It does
> **not** carry a `Tenant` object, and the user service does **not** join to a tenant table — it
> holds the id and nothing more.

Consequences that are features, not limitations:

- **No distributed joins.** If an orchestrator needs a user *and* its tenant, it asks the user
  service for the user (which yields a `tenantId`) and the tenant service for that tenant — two
  cheap topic calls, each hitting one owned database. Composition happens in the orchestrator, over
  the mesh, not in a database engine.
- **Referential integrity is eventual, and that's fine.** A `tenantId` on a user is a claim, not a
  database-enforced constraint. Enforcing that the tenant *exists* at write time, if you need it,
  is an orchestrator concern (a saga step that reads the tenant first) — not a core-service one.
- **Aggregates stay small and independent.** Each service's schema is about *its* aggregate only.

---

## Directional dependencies

References point **one way**, and the direction is always **child → parent**: the child knows the
parent; the parent does not know the child.

```
  Tenant  (parent — knows nothing about users)
    ▲
    │  User.tenantId  (child holds the parent's id)
    │
  User    (child — knows its tenant)
```

- The **user** service references the **tenant** by `tenantId`.
- The **tenant** service has no `userId`, no user table, no awareness that users exist.

This keeps the dependency graph **acyclic**. A service can only ever depend "downward" on
services that own the aggregates it references, and never the reverse. Acyclicity is what lets you
deploy the tenant service without touching the user service, delete the user service without the
tenant service caring, and reason about blast radius by following arrows in one direction.

If you find yourself wanting the parent to know about its children (the tenant needing to list its
users), that is a **read model / query concern** and it belongs *above* the core layer — an
orchestrator or a dedicated read service composes "a tenant and its users" by asking both services;
the tenant core service still does not grow a dependency on the user service. Cross-cutting queries
across aggregates are a fleet-level concern the [mesh](../specification/mesh.md) and read
models serve, not a reason to break the directional rule.

---

## A core service is a Benzene Cloud Service

Nothing about a core service is special Benzene — it is the ordinary, recommended shape at the top
of the [adoption ladder](../specification/design-principles.md#2-the-adoption-ladder). Aim
each core service at the [Cloud Service Profile](../specification/cloud-service-profile.md) so
the fleet tooling works on it with no negotiation:

- **CRUD topics served by handlers via the registry** (R2) — which is what makes the spec derivable.
- **Derived spec** at `/benzene/spec` (R5) — the CRUD surface documents itself; generate clients
  from it rather than hand-writing them.
- **Health checks** (R3) — a core service's health check naturally probes *its own* database
  connectivity, nothing else (share-nothing again).
- **Mesh feeds** (R6) and **trace-context propagation** (R8) — so the fleet view shows this
  service's real traffic, its schemas, and its edges.

A core service typically needs **no orchestration and no saga** of its own: each of its writes is a
single write to a single database, atomic on its own. Atomicity *across* services is the
orchestrator's problem, not the core service's — which is exactly why the core service can stay so
simple.

---

## Checklist

A service is a well-formed core service when:

- [ ] It owns **one or two aggregate roots** and is the authority on them.
- [ ] It owns **its own database** and no other service touches that store.
- [ ] Cross-aggregate references are **by id only**; no embedded copies, no cross-service joins.
- [ ] Its references point **child → parent** only; the dependency graph stays acyclic.
- [ ] Every write topic **validates** and returns a `validation` result on failure.
- [ ] It contains **no cross-service process** — any two-aggregate logic has been pushed up to an
      [orchestrator](orchestrators.md).
- [ ] Its topics are `aggregate:operation`, served by handlers, with a **derived spec**.
- [ ] It targets the **[Cloud Service Profile](../specification/cloud-service-profile.md)**.

Next: the layer that turns these building blocks into business processes — **[orchestrators](orchestrators.md)**.
