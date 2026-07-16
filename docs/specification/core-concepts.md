# Core Concepts

**Status: DRAFT v0.1**

This document defines Benzene's abstract model. Nothing here mentions a wire format (see
[wire-contracts.md](wire-contracts.md)) or a specific transport (see
[transport-bindings.md](transport-bindings.md)).

## 1. The model in one paragraph

A Benzene application is a set of **message handlers**, each identified by a **topic**, invoked
through a **middleware pipeline** that operates on a transport-specific **context**. **Transport
adapters** (ports, in hexagonal-architecture terms) convert a native transport event (an HTTP
request, a gRPC call, a queue message, a function invocation) into a context, run the pipeline
exactly once, and convert the handler's **result** back into a native response. Application code —
handlers and middleware — is transport-neutral and vendor-neutral; only the adapter at the edge
knows what the transport is.

That paragraph describes the *steer*, not the minimum: message handlers are Benzene's
recommended shape, but a pipeline with no handlers at all, or one invoked in process behind no
transport, is still a conforming Benzene application. Which capabilities each part unlocks —
and how the ones that need handlers degrade without them — is defined in
[design-principles.md](design-principles.md).

## 2. Topic

A topic identifies a message type and routes it to a handler.

- A topic has an **id** (a non-empty string) and an optional **version** (a string, empty by
  default).
- Topic id matching is exact and case-sensitive at the routing layer. (Transport adapters MAY
  apply their own normalization before routing — e.g. the gRPC adapter matches method paths
  case-insensitively — but the topic itself, once resolved, is a literal string.)
- A (topic id, version) pair maps to **at most one** handler. Registering two handlers for the
  same pair is a startup error, not a runtime dispatch ambiguity.
- Version selection: when a message arrives without a version, the unversioned handler (version =
  empty) handles it. Versioned handlers are selected only by an exact version match.

Topic ids SHOULD be lower-case with `:` or `_` separators (e.g. `order:create`, `say_hello`);
this is a convention, not a requirement.

## 3. Message handler

A handler is a function from a request to a result:

```
handle : TRequest -> Result<TResponse>
```

- `TRequest` and `TResponse` are application types. The handler never sees the transport.
- The handler returns a **result** (section 5), never throws for domain failures. An uncaught
  exception escaping a handler is converted by the framework into an `UnexpectedError` /
  `ServiceUnavailable`-class result — it MUST NOT crash the transport adapter.
- **Streaming**: a request or response type (or both) MAY be an **asynchronous stream** of items
  (`IAsyncEnumerable<T>` in .NET; the language's idiomatic async-stream type elsewhere — a channel
  in Go, an async generator in TypeScript/Python). The handler contract is otherwise unchanged,
  and the pipeline still runs exactly once per invocation, not per item (section 4). Per-item
  concerns belong inside the handler.
- Handlers declare their request/response types in whatever form is natural to the language;
  the framework is responsible for mapping the transport's native payload to `TRequest`
  (see section 6 and each transport binding's serialization rules).

## 4. Middleware pipeline

A pipeline is an ordered list of middleware components executed around handler dispatch:

```
middleware : (Context, next: () -> Promise<void>) -> Promise<void>
```

Required semantics:

- **Order**: middleware runs in registration order; the first registered is the outermost.
- **Short-circuit**: a middleware that does not call `next` terminates the pipeline; everything
  after it (including the handler) does not run. This is the mechanism behind features like
  health check interception.
- **One invocation per transport event.** An HTTP request, a gRPC call (of any streaming shape),
  a single queue message — each is exactly one pipeline invocation. A *batch* delivery (e.g. an
  SQS batch of 10 messages) is one pipeline invocation **per message**, each in its own scope.
- **The pipeline carries no cancellation parameter.** Cancellation, deadlines, and other
  invocation-scoped facts ride on the context (or an accessor resolved from the invocation's
  scope), not on the middleware signature. This keeps the middleware shape identical across
  transports that have no cancellation concept.
- **Terminal middleware**: the message router (topic → handler dispatch) is itself an ordinary
  middleware, conventionally registered last.

## 5. Result

Every handler invocation produces a result:

| Field | Type | Meaning |
|---|---|---|
| `status` | string | One of the status vocabulary values (see [wire-contracts.md §3](wire-contracts.md#3-status-vocabulary)), or an application-defined extension |
| `isSuccessful` | boolean | Derived from status class |
| `payload` | `TResponse?` | Present on success (and optionally on failure) |
| `errors` | string[] | Zero or more human-readable error messages; populated on failure |

- The status vocabulary is **strings, not enums**, so applications can extend it; unknown statuses
  map to the transport's generic-error code (each binding defines its default).
- Results are values, not exceptions. Transport adapters translate a non-success status into the
  transport's native failure signal (HTTP status code, gRPC `RpcException`, ...) per the mapping
  tables in [wire-contracts.md](wire-contracts.md).

## 6. Context and request mapping

Each transport defines a **context** type carrying, at minimum:

- the resolved **topic**,
- the native request (or a stream of them),
- **headers** as a string→string dictionary (mapped from the transport's native metadata — see
  each binding),
- a slot for the handler's result,
- invocation-scoped facts the transport has (cancellation token, deadline, native call object).

**Request mapping** converts the context's native payload into the handler's declared `TRequest`:

1. If the native payload already *is* `TRequest` (same type / directly assignable), pass it
   through untouched — zero-copy.
2. If both sides are asynchronous streams, wrap lazily, converting per item by rules 1/3.
3. Otherwise convert via the binding's serialization rule (JSON by default; protobuf-JSON for
   gRPC — see the bindings).

The same rules apply in reverse for the response. This is what lets a handler choose, per side,
between the transport's native type (zero-copy) and its own plain type (converted).

## 7. Application lifecycle

An application is defined once, platform-neutrally, in three phases run in order, exactly once,
at startup:

1. **`getConfiguration()`** — produce the configuration object. Runs before any registration; no
   service resolution available.
2. **`configureServices(container, configuration)`** — register handlers, middleware
   dependencies, and adapters with the DI container (section 8).
3. **`configure(appBuilder, configuration)`** — build the pipeline(s) against a
   platform-neutral application builder. Transport-specific entry points are attached via
   `use<Transport>(...)` extensions that **pattern-match the concrete platform and no-op
   otherwise**, so one application definition can target several platforms and each deployment
   activates only the entry points its host supports.

The no-op rule is load-bearing for vendor neutrality: `useAwsLambda(...)` on an Azure host, or
`useGrpc(...)` on a worker host, MUST silently do nothing rather than fail.

## 8. Dependency registration and resolution

Benzene defines a minimal container abstraction rather than mandating a DI framework:

- **Registration**: `addSingleton` / `addScoped` / `addTransient`, by type, instance, or factory,
  plus `tryAdd*` variants (register only if absent — this is how defaults are made overridable:
  the framework `tryAdd`s its defaults, the application's own registration wins).
- **Resolution**: a **scope** is created per pipeline invocation; scoped services live and die
  with it. `getService` (required, throws if missing) and `tryGetService` (optional, returns
  absent) are the only resolution operations.
- Languages without a DI culture (Go, Rust) MAY implement this as an explicit registry/context
  object; the *semantics* that must survive are: per-invocation scoping, overridable defaults,
  and construction of handlers/middleware with their declared dependencies.

## 9. Handler discovery

The **concept** is explicit registration: an application hands the framework a list of
(topic, version, handler, request type, response type) records.

Attribute/annotation scanning (`[Message("topic")]` in .NET) is an **idiom** — per-language sugar
that produces those records. Implementations MUST support explicit registration as a first-class
path; scanning is optional. Route-level attributes for specific transports (e.g.
`[GrpcMethod("/pkg.Service/Method")]`) are likewise sugar over an explicit
(route → topic) registration.

## 10. Cross-cutting middleware guaranteed to be portable

These features are defined purely in terms of sections 2–8 and the header conventions in
[wire-contracts.md §2](wire-contracts.md#2-header-conventions), so they MUST behave identically on
every transport and every vendor:

- **W3C trace context** — continue a trace from `traceparent`/`tracestate` headers. This is the
  cross-service correlation mechanism. (A per-invocation correlation value settable by application
  middleware and forwardable on outbound clients MAY additionally be offered, but inbound pickup of
  legacy correlation headers is not a framework contract.)
- **Health checks** — intercept the reserved `healthcheck` topic (plus an app-chosen alias), run
  registered checks, respond with the standard response format.
