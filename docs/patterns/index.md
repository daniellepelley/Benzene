# Patterns

Recurring ways of composing Benzene's **core building blocks** — topics, message handlers, the
middleware pipeline, results, and per-invocation scopes ([core-concepts.md](../specification/core-concepts.md)) —
into real services, and whole systems of services. A pattern here is not part of the normative
[specification](../specification/index.md) and it is not a feature of any one language: it is a
*shape* that falls out of the core model and reads the same whether the service is written in .NET,
Go, TypeScript, or Python.

Each pattern explains the *idea* and when to reach for it. **How to express it** — the exact API,
the package, the attribute or the registration call — is language-specific and lives in that port's
own docs; where a pattern shows a concrete call it is marked *(informative, .NET)* and the shape,
not the syntax, is the point.

Two scales of pattern:

**Composing one service** — the shapes a single Benzene service is built from:

- [Composing a service from the core](composing-services.md) — the handful of shapes almost every
  Benzene service is built from: a handler per topic, cross-cutting concerns as middleware, results
  instead of exceptions, per-invocation scope for request-scoped state, and a transport-neutral core
  behind a thin adapter at the edge

**Composing a system** — how to arrange many services into an estate:

- [The modular monolith, and the road out of it](modular-monolith.md) — start as **one deliverable**
  whose modules talk by topic through in-process pipelines, so the module seams are message
  contracts from day one — and extraction to microservices, when the organization calls for it, is
  a routing-table change instead of a rewrite
- [The two-tier microservice architecture](two-tier-architecture.md) — a system split into a layer
  of data-owning **core services** and a layer of process-owning **orchestrators**, drawn from a
  large-scale platform built on a Benzene-family framework. Its parts:
  - [Core services](core-services.md) — the CRUD/data layer: one or two aggregate roots each,
    share-nothing databases, validation, reference-by-id, child-knows-parent dependencies
  - [Orchestrators](orchestrators.md) — the process layer: request/response and event-driven
    triggers, emitted events, and the saga pattern for all-or-nothing distributed writes
  - [Service communication](service-communication.md) — topic-based addressing, the routing table,
    the AWS Lambda-to-Lambda realization, and the central-routing-lambda options with their
    latency/cost trade-offs

- [Event-driven choreography](choreography.md) — the counterpart to the orchestrator tier: services
  react to events and emit their own, with no central conductor. When to orchestrate vs choreograph,
  how to emit and consume events on Benzene's transports, and why the mesh draws the choreography
  graph for you
- [The transactional outbox](transactional-outbox.md) — reliable event publishing: the dual-write
  problem, and the near-zero-code change-data-capture form on Benzene that makes an event a
  consequence of the committed write, so the events you choreograph on are never lost
- [CQRS & read models](cqrs-read-models.md) — the query side share-nothing core services can't serve:
  a derived, denormalized view projected from domain events, answering cross-aggregate queries with a
  single read

**At enterprise scale** — high-volume, real-time, and audit-heavy workloads, with worked examples of
the kind of systems large enterprises ask to have designed:

- [Real-time stream processing](streaming-processing.md) — the ordered streaming binding (Kinesis /
  Event Hubs), partition-by-key, windowing, backpressure and checkpointing, worked through a
  financial **market-data tick pipeline**
- [Map-reduce & high-volume compute](map-reduce.md) — scatter-gather for large partitionable
  calculations: bounded parallel fan-out and an app-owned reduce, worked through an **end-of-day
  portfolio-risk** run over a million positions
- [Event sourcing](event-sourcing.md) — an immutable, ordered event log as the source of truth,
  composed from command handlers + a change-captured log + projections + event versioning, worked
  through a **trade ledger** with full audit and replay
- [Reference solution: a real-time risk & trading platform](reference-real-time-risk.md) — a whole
  enterprise system that **assembles** the patterns above (stream → choreography → CQRS → map-reduce →
  event sourcing → gRPC) into one worked design
