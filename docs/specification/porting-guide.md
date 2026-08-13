# Porting Guide

**Status: DRAFT v0.1**

Notes for implementing Benzene in another language. The strategy is **spec-first**: a port
implements [core-concepts.md](core-concepts.md), [wire-contracts.md](wire-contracts.md), and
[transport-bindings.md](transport-bindings.md) idiomatically — it does not translate the C# API.
Interop with .NET Benzene services comes from the wire contracts, and is the first thing a port
should prove, not the last.

Once a port is under way, every shippable increment is held to
[port-quality-standards.md](port-quality-standards.md) — the Definition of Done that gates port work
(DX-champion-in-the-loop workflow; conformance; layered packaging; a runnable multi-transport example
per cloud provider tested through the port's own dogfooded test helpers; a CI gate; website-ready
docs). This guide is *how to translate*; that document is *when the translation is done*.

## 1. What is concept vs C# idiom

| .NET mechanism | The concept underneath | Idiomatic equivalent elsewhere |
|---|---|---|
| `[Message("topic")]` attribute + assembly scanning | Explicit handler registration records (core-concepts §9) | Explicit registration calls (Go, Rust); decorators (TS/Python); codegen |
| `IMessageHandler<TRequest,TResponse>` generic interface | `handle: TRequest → Result<TResponse>` | A function type / single-method interface; generics where available, per-registration types where not |
| MS DI + `IBenzeneServiceContainer` adapter | Registration + per-invocation scope + overridable defaults (core-concepts §8) | A context/registry object (Go); constructor injection frameworks where cultural (Java/TS) |
| `IMiddleware<TContext>` + `Func<Task> next` | Ordered onion pipeline with short-circuit (core-concepts §4) | The language's standard middleware shape (Go http-style wrappers, Express/Koa, Python ASGI-like) |
| `IAsyncEnumerable<T>` streaming handlers | Async stream of items, one pipeline run per call (core-concepts §3) | Channels (Go), async generators (TS/Python), `Stream` (Rust) |
| `GrpcServerCallAccessor` scoped accessor | Invocation-scoped facts available to handler code without transport coupling | Context values (Go `context.Context`), ALS (Node), contextvars (Python) |
| Reflection-cached protobuf `Descriptor`/`Parser` lookups | proto3-JSON bridging rule (wire-contracts §6) | Each language's protobuf library exposes the same JSON mapping natively |
| `BenzeneStartUp` abstract class | The three-phase lifecycle + platform no-op rule (core-concepts §7) | A builder or plain functions; the *ordering and no-op semantics* are what must survive |

Rule of thumb: if removing a mechanism would change what's on the wire or what a handler observes,
it's a concept and it's in the spec. Otherwise it's an idiom — do what's natural in the target
language.

Client generation (the `codegen` row above) has its own pinned contract: the `.spec.json` a
service derives and serves ([contract-document.md](contract-document.md)), the topic-scoping and
schema-closure rules a generator must implement identically, and the language-neutral
`contractHash` algorithm — all conformance-pinned by `contract-document-cases.json` and
`contract-hash-cases.json`. A port that ships a client generator implements that document; method
naming and file layout stay idiom, same rule of thumb as above.

## 2. Suggested porting order

1. **Wire contracts first**: envelope + status vocabulary + header conventions, verified against a
   running .NET Benzene service (send/receive the envelope, assert statuses round-trip).
   Cross-language interop is the product; prove it in week one.
2. Result type, topic, registry, pipeline (with short-circuit + scope semantics).
3. One inbound binding (HTTP is the cheapest) end-to-end, including status mapping and the
   correlation/trace middleware.
4. One outbound client + decorators.
5. Health checks (reserved topic + response format).
6. Further bindings by demand — each is additive.

## 3. Conformance testing

A language-neutral test suite that every implementation runs:

- **Fixture form** (exists — see [conformance/](conformance/README.md)): JSON fixtures for the
  status vocabulary, the HTTP/gRPC mapping tables in both directions, and end-to-end envelope
  cases run against a canonical handler set. The .NET reference runner is
  `test/Benzene.Conformance.Test/`; a port writes its own runner over the same files.
- **Interop form** (future): a docker-composed pair — reference .NET service + candidate
  implementation — exercising envelope round-trips, correlation/trace propagation, and the
  `benzene-status` trailer over real transports.
- A port is "Benzene" when it passes both; API shape is explicitly not part of conformance.

Conformance comes in two levels ([cloud-service-profile.md](cloud-service-profile.md)): the
fixtures above establish **Benzene Core**. A port that also wants its services to claim the
**Cloud Service Profile** additionally implements the service-side mesh feeds and passes
`mesh-descriptor-cases.json` and `mesh-trace-cases.json`. Plan for the profile from the start
if the port's services should appear in a mesh — it is the difference between "interoperates"
and "fully tool-operable".

## 4. Recommended: an in-process transport

Not required for conformance (it carries nothing over the wire, so there is nothing for the
fixtures to check), but strongly recommended once the outbound client + decorators step (§2.4) is
done: a `MessageSender`/outbound-client implementation that dispatches straight to a handler
pipeline built in the *same* runtime, with no wire hop at all — not even loopback.

It exists for the [modular monolith pattern](../../docs/patterns/modular-monolith.md): a service
built as named, topic-addressed pipelines in one process, extracted into microservices later by
swapping a routing-table entry from "in-process" to a real transport. A port without this
transport still works for that pattern — a hand-rolled direct function call stands in — but it
loses the point of the pattern: that a module boundary is a *message*, indistinguishable from a
call that will one day cross a process, from the first commit.

Two things vary by port, and both are architecture, not oversight:

- **Whether "two named pipelines can share a topic" needs a workaround.** If handler registration
  is a process-wide singleton (as in .NET's `MessageHandlerDefinitionIndex` or a decorator/module
  scanner shared process-wide), two named in-process pipelines that both declare a handler for the
  same topic collide — fan-out to several pipelines then needs a per-target topic to disambiguate
  (see .NET's `InProcessFanOutTarget`/`DuplicateInProcessFanOutTargetException`). If the registry
  is per-instance (constructed fresh per pipeline, as in Go and Python), there is no collision and
  no workaround is needed — fan-out just dispatches the caller's one topic to each pipeline's own
  registry.
- **Whether a separate boot-time validation pass is worth adding.** A port whose wiring is
  imperative and runs once at startup (Go, Python) already fails loudly on a typo'd pipeline name
  at construction — nothing lazy is left to validate. A port with a declarative, possibly-deferred
  routing table (.NET's `OutboundRoutingBuilder`) benefits from an explicit startup check
  (.NET's `IStartUpCheck`) that cross-references every route reference against the registered
  pipeline names before the service is considered healthy, rather than surfacing the mistake on
  first send.

Match the target language's existing idiom for outbound clients (a constructed object used
directly, not a new routing concept invented for this one transport) rather than translating any
one port's shape literally — see the .NET, Go, TypeScript, and Python implementations for four
idiomatic answers to the same two questions above.

## 5. Known .NET-isms that must NOT leak into the spec

Recorded so they don't get accidentally specified:

- `Void` as a class standing in for "no response" — a port should use its language's unit type.
- Status **wire values** are a contract (keep) — they are the lowercase-kebab-case strings of
  wire-contracts §3 (e.g. `not-found`), *not* the .NET enum's PascalCase member name, which is a
  .NET-ism that must not leak. PascalCase keys inside health check `data` bags are incidental (each
  check writes verbatim keys — specified as "verbatim", not as "PascalCase").
- ~~The `message` vs `body` envelope field inconsistency~~ — resolved: both sides now use `body`
  (wire-contracts §1.1); the client also tolerates numeric HTTP status codes on read for
  compatibility with older services, which a fresh port need not replicate.
- Attribute-based gRPC route declaration — the spec form is an explicit (route → topic) record.
- The reopened-container / accessor-instance tricks in ASP.NET Core hosting — pure host plumbing;
  no equivalent should be required of a port whose platform doesn't have the same DI split.
