# Patterns

Recurring ways of composing Benzene's **core building blocks** — topics, message handlers, the
middleware pipeline, results, and per-invocation scopes ([core-concepts.md](../specification/core-concepts.md)) —
into real services. A pattern here is not part of the normative
[specification](../specification/index.md) and it is not a feature of any one language: it is a
*shape* that falls out of the core model and reads the same whether the service is written in .NET,
Go, TypeScript, or Python.

Each pattern explains the *idea* and when to reach for it. **How to express it** — the exact API,
the package, the attribute or the registration call — is language-specific and lives in that port's
own docs.

- [Composing a service from the core](composing-services.md) — the handful of shapes almost every
  Benzene service is built from: a handler per topic, cross-cutting concerns as middleware, results
  instead of exceptions, per-invocation scope for request-scoped state, and a transport-neutral core
  behind a thin adapter at the edge
