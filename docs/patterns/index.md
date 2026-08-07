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
