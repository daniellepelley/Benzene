# Composing a service from the core

Almost every Benzene service — in any language — is built from the same handful of shapes. None of
them is a framework feature you switch on; each is a direct consequence of the
[core model](../specification/core-concepts.md): topics, message handlers, the middleware pipeline,
results, and per-invocation scopes. This guide names those shapes so you can recognise them, and
points at where each one is defined normatively.

Everything below is language-neutral. **How** you express a pattern — the attribute, the
registration call, the package name — is documented in each port's own docs; the *shape* is the same
everywhere.

## A handler per topic

The unit of application logic is a **message handler**: a function from a request to a
[result](../specification/core-concepts.md#5-result), identified by a
[topic](../specification/core-concepts.md#2-topic).

```
handle : TRequest -> Result<TResponse>
```

One topic maps to at most one handler, so a service is naturally a *set* of small, independent
handlers rather than one branching entry point. A handler never sees the transport — the same
`order:create` handler serves an HTTP request, a queue message, or a function invocation unchanged.
Keep handlers this way and the transport becomes a deployment decision, not a rewrite.

When you need a second implementation of the same message — a breaking change to how `order:create`
behaves — give it a topic **version** rather than a new topic id or an `if` inside the handler.
Routing selects the implementation; the caller's topic id is unchanged. (A payload's *schema*
version is a different concept — it is upcast/downcast transparently without a second handler. See
[versioning.md](../specification/versioning.md).)

## Cross-cutting concerns as middleware

Anything that wraps *every* handler — authentication, logging, tracing, validation, error mapping —
is a [middleware](../specification/core-concepts.md#4-middleware-pipeline) component, not code copied
into each handler. Middleware runs in registration order, outermost first, and each component
chooses whether to call `next`.

That single choice is the whole mechanism behind two everyday patterns:

- **Short-circuit.** A middleware that does *not* call `next` ends the pipeline before the handler
  runs. This is how health-check interception works (the reserved `benzene:healthcheck` topic never
  reaches a handler), and how an auth or validation gate rejects a bad request without any handler
  seeing it.
- **Wrap-around.** A middleware that calls `next` inside a `try`/timer/span observes the whole rest
  of the pipeline — the basis of error mapping, latency metrics, and trace spans.

Because the middleware signature carries no transport and no cancellation parameter (those ride on
the [context](../specification/core-concepts.md#6-context-and-request-mapping)), the *same*
middleware composes identically on every transport.

## Results, not exceptions

A handler returns a [result](../specification/core-concepts.md#5-result) — a value with a `status`,
an `isSuccessful` flag, an optional `payload`, and `errors` — it does not throw for domain failures.
"Order not found" is a returned `not-found` result, not an exception.

This keeps failure in the type you already return, so every caller and every middleware handles it
the same way, and the transport adapter maps the status to the transport's native failure signal
(an HTTP code, a gRPC status) by the tables in
[wire-contracts.md](../specification/wire-contracts.md). Exceptions are reserved for the genuinely
*unexpected*: an uncaught one is caught by the framework, turned into an `unexpected-error`-class
result, and MUST NOT crash the adapter.

## Per-invocation scope for request-scoped state

A fresh [DI scope](../specification/core-concepts.md#8-dependency-registration-and-resolution) is
created per pipeline invocation, and scoped services live and die with it. That gives you a clean
home for anything that belongs to *one* message — a unit of work, a correlation value, an accumulator
a middleware fills and the handler reads — without threading it through every call or reaching for a
static.

The scope boundary is the message, precisely: a batch delivery (say ten queue messages at once) is
[one invocation, and one scope, per message](../specification/core-concepts.md#4-middleware-pipeline),
so request-scoped state never leaks from one message to the next.

## A transport-neutral core behind a thin edge

The patterns above add up to one arrangement: handlers, middleware, and their dependencies know
nothing about the transport, and a **transport adapter** at the edge converts a native event into a
context, runs the pipeline exactly once, and converts the result back. The adapter is the *only*
part that knows whether it is HTTP, gRPC, a queue, or a function.

Two conventions keep this honest:

- Define the application **once** — configuration, service registration, pipeline — and attach
  transport entry points with `use<Transport>(...)` calls that
  [no-op when they don't match the host](../specification/core-concepts.md#7-application-lifecycle).
  One definition can then target several platforms, and each deployment activates only the entry
  points its host supports.
- Register the framework's defaults as *overridable* (`tryAdd`), so your own registration wins
  without editing the framework. Defaults you didn't override are still there.

The pay-off is the reason to hold the line on all of the above: a service composed this way moves
between transports and vendors by changing the adapter at the edge, not the logic in the middle.
