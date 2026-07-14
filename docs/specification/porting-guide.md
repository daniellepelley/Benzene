# Porting Guide

**Status: DRAFT v0.1**

Notes for implementing Benzene in another language. The strategy is **spec-first**: a port
implements [core-concepts.md](core-concepts.md), [wire-contracts.md](wire-contracts.md), and
[transport-bindings.md](transport-bindings.md) idiomatically — it does not translate the C# API.
Interop with .NET Benzene services comes from the wire contracts, and is the first thing a port
should prove, not the last.

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

## 4. Known .NET-isms that must NOT leak into the spec

Recorded so they don't get accidentally specified:

- `Void` as a class standing in for "no response" — a port should use its language's unit type.
- PascalCase status strings are a wire contract (keep), but PascalCase keys inside health check
  `data` bags are incidental (each check writes verbatim keys — specified as "verbatim", not as
  "PascalCase").
- ~~The `message` vs `body` envelope field inconsistency~~ — resolved: both sides now use `body`
  (wire-contracts §1.1); the client also tolerates numeric HTTP status codes on read for
  compatibility with older services, which a fresh port need not replicate.
- Attribute-based gRPC route declaration — the spec form is an explicit (route → topic) record.
- The reopened-container / accessor-instance tricks in ASP.NET Core hosting — pure host plumbing;
  no equivalent should be required of a port whose platform doesn't have the same DI split.
