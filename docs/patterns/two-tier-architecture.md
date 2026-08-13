# The Two-Tier Microservice Architecture

**Status: DRAFT v0.1 — a reference architecture, not wire format.**

The [specification](../specification/index.md) defines what *one* good Benzene service is —
the middleware pipeline, message handlers, topics, results, and (at the top of the adoption
ladder) the [Cloud Service Profile](../specification/cloud-service-profile.md) that makes a
service a first-class fleet citizen. It is deliberately silent on how you compose *many* services
into a system.

This pattern fills that gap for one field-tested shape: a system split into a layer of small
domain-owning **core services** and a layer of process-owning **orchestrators** on top of them —
the shape of a real large-scale platform built on a Benzene-family framework, distilled into
something a team can follow or an agent can execute. Nothing here is normative; it builds *on top
of* the spec's vocabulary (topics, handlers, results, the mesh, the Cloud Service Profile) and
links to it rather than restating it. Where it names a concrete API it is marked
*(informative, .NET)* — the shape is language-neutral; the .NET names are illustrative, exactly as
in [design-principles.md](../specification/design-principles.md).

![Two-tier architecture: an Orchestrators tier receives an API call or event and calls four core services — Tenant, User, Order, Billing — over the mesh, one call per aggregate operation per the routing table; each core service owns its own database, share-nothing.](diagrams/two-tier-architecture.svg)

Two service types, one rule each:

- **[Core services](core-services.md)** own **data**. Each looks after one or two aggregate
  roots, owns its own database (share-nothing), validates its own objects, and holds almost no
  business process. They are the system's system-of-record. A core service is a textbook
  [Benzene Cloud Service](../specification/cloud-service-profile.md): CRUD topics served by
  handlers, a derived spec, health checks, mesh feeds.

- **[Orchestrators](orchestrators.md)** own **process**. They take a request (from an API) or an
  event, drive a business process across several core services, emit events on success or
  failure, and — crucially — run the multi-service write as a **[saga](orchestrators.md#the-saga-pattern)**:
  it either wholly succeeds or is wholly compensated, never left half-applied.

The two tiers talk over the mesh with ordinary Benzene request/response calls. On AWS the
reference realization is **[Lambda-to-Lambda](service-communication.md)** — fast, cheap, and a
natural fit for Benzene's envelope — with a **routing table** that turns a topic into a
destination. [Service communication](service-communication.md) covers that, including the
**central-routing-lambda** options and their latency/cost trade-offs.

### Why split this way

| Concern | Core services | Orchestrators |
|---|---|---|
| Owns | One/two aggregate roots + their database | A business process |
| Business logic | Minimal — validation and CRUD | Where all the process logic lives |
| State | The system of record (a database) | Ideally stateless between steps; saga state is transient |
| Fails how | A single write fails cleanly | A multi-service process fails **atomically** (saga) |
| Changes when | The shape of the domain data changes | The steps of a business process change |
| Scales on | Data volume / read-write load for its aggregate | Process throughput |

The payoff is that the two axes of change are separated. Redefining a business process touches an
orchestrator and no core service; reshaping an aggregate's data touches one core service and no
process. Each core service is independently deployable, independently scalable, and — because it
shares no database — independently reasoned about.

---

## When to use this pattern

**Use it when** you have a domain that decomposes into clear aggregate roots *and* business
processes that span several of them, you want independent deployability and share-nothing data
ownership, and you can tolerate eventual consistency between aggregates (bridged by sagas for the
writes that must be atomic).

**Don't reach for it when** a single service with a single database would do. This pattern buys
you fleet-scale separation of concerns at the cost of distributed-systems complexity (network
calls, partial failure, sagas, a routing table). A small system does not need two tiers; start
with one service and let it split when an aggregate boundary or a process boundary makes itself
obvious. The [adoption ladder](../specification/design-principles.md#2-the-adoption-ladder)
applies here too — climb it as the need appears, not before.

---

## Rules of the pattern (for a team or an agent to follow)

These are the invariants that make the architecture hold together. The per-layer docs expand each
one; this list is the checklist.

1. **One database per core service; share nothing.** No two services touch the same store. Cross-
   aggregate references are by **id only** — see [core-services.md](core-services.md#reference-by-id).
2. **Dependencies point one way: child knows parent, parent does not know child.** `User` holds a
   `tenantId`; the `Tenant` service has never heard of users. This keeps the dependency graph
   acyclic — see [core-services.md](core-services.md#directional-dependencies).
3. **Core services hold no cross-service process.** If a piece of logic needs to touch two
   aggregates, it belongs in an orchestrator, not in a core service.
4. **Every multi-service write is a saga.** An orchestrator that writes to more than one core
   service does it as an all-or-nothing saga with a compensation for every effect — see
   [orchestrators.md](orchestrators.md#the-saga-pattern).
5. **Address services by topic, not by transport.** Application code calls
   `send("tenant:create", req)`; the **routing table** maps the topic to a destination. Never
   hard-code a queue URL or a function name at a call site — see
   [service-communication.md](service-communication.md).
6. **Every service is a good mesh citizen.** Core services and orchestrators alike aim for the
   [Cloud Service Profile](../specification/cloud-service-profile.md) so the fleet is visible
   and operable end to end.

---

## The documents

- **[Core services](core-services.md)** — the data-owning CRUD layer: aggregate roots,
  share-nothing databases, validation, reference-by-id, directional dependencies.
- **[Orchestrators](orchestrators.md)** — the process-owning layer: request/response and
  event-driven triggers, event emission, and the saga pattern for atomic distributed writes.
- **[Service communication](service-communication.md)** — how the tiers talk: topic-based
  addressing, the routing table, the AWS Lambda-to-Lambda realization, and the central-routing-
  lambda options with their cost/latency analysis.

---

## Relationship to the specification

This pattern is a *consumer* of the spec, never a competitor to it:

- A core service and an orchestrator are both just **Benzene services** — same pipeline, same
  handlers, same result model ([core-concepts.md](../specification/core-concepts.md)).
- "A good fleet citizen" means the [Cloud Service Profile](../specification/cloud-service-profile.md);
  the pattern adds *how to organize a fleet of profiled services*, not new requirements on any one
  of them.
- Fleet visibility — which service consumes which topic, how often, over which transport — comes
  from the [mesh](../specification/mesh.md) for free; the pattern relies on it rather than
  redefining it.

If while following this pattern you find yourself wanting a new *observable contract* (a wire
shape, a status, a mesh field), that is a **spec change**, made in `docs/specification/**` — not a
pattern change. Patterns describe how to arrange the pieces; the spec defines the pieces.
