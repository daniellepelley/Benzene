# Core Concepts

**Core**

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
transport, is still a conforming Benzene application. A capability that depends on a steer
degrades when the steer is declined (section 11); the adoption ladder that explains why is
guidance, in [design-principles.md](../guides/design-principles.md).

## 2. Topic

A topic identifies a message type and routes it to a handler.

- A topic has an **id** (a non-empty string) and an optional **version** (a string, empty by
  default).
- Topic id matching is exact and case-sensitive at the routing layer. (Transport adapters MAY
  apply their own normalization before routing — e.g. the gRPC adapter matches method paths
  case-insensitively — but the topic itself, once resolved, is a literal string.)
- A (topic id, version) pair maps to **at most one** handler. Registering two handlers for the
  same pair is a startup error, not a runtime dispatch ambiguity.
- **The version signal** is the message's `benzene-version` header (wire-contracts §2) — one header,
  which drives both which handler runs (here) and which shape the payload arrives in (payload
  casting, [versioning.md](versioning.md)). On HTTP a route segment MAY carry it instead; the
  binding then supplies it exactly as if the header had been sent.
- **Version selection** for a message carrying version *v*, in order:
  1. the handler registered for exactly (topic id, *v*);
  2. otherwise, a handler of that topic the service can **cast** *v* to: a registered payload cast
     (versioning.md) takes a payload of version *v* to that handler's request type. An unversioned
     handler is such a target like any other. Where *v* can be cast to more than one handler, the
     topic's default handler (below) is chosen if it is among them; otherwise the choice is the
     port's, and MUST be deterministic and documented;
  3. otherwise, `not-found`, with an error naming the topic and *v*.

  There is no "nearest" or "highest available" fallback: serving another version's behaviour to a
  caller that named a version is the failure versioning exists to prevent. (A port MAY offer a
  different selection policy as an explicit, replaceable option; it is never the default.)
- **A message without a version** goes to the topic's **default handler**: the unversioned handler
  (version empty) if one is registered, otherwise the handler the application explicitly declared
  as the default. With neither, it is `not-found`. A topic whose handlers are all versioned and none
  declared default SHOULD be reported at start-up: it is valid, but every unversioned message to it
  will fail.

Topic ids SHOULD be lower-case with `:` or `_` separators (e.g. `order:create`, `say_hello`);
this is a convention, not a requirement.

A topic's version (this section) selects between **handler implementations** registered for the
same topic id. This is a distinct concept from a request/response **payload's schema version**
(transparently upcast/downcast without a second handler) — see
[versioning.md](versioning.md) for both, and how they relate.

## 3. Message handler

A handler is a function from a request to a result:

```
handle : TRequest -> Result<TResponse>
```

- `TRequest` and `TResponse` are application types. The handler never sees the transport.
- The handler returns a **result** (section 5), never throws for domain failures. An uncaught
  exception escaping a handler is converted by the framework into an `unexpected-error` /
  `service-unavailable`-class result — it MUST NOT crash the transport adapter.
- **Streaming**: a request or response type (or both) MAY be an **asynchronous stream** of items
  (`IAsyncEnumerable<T>` in .NET; the language's idiomatic async-stream type elsewhere — a channel
  in Go, an async generator in TypeScript/Python). The handler contract is otherwise unchanged,
  and the pipeline still runs exactly once per invocation, not per item (section 4). Per-item
  concerns belong inside the handler.
- Handlers declare their request/response types in whatever form is natural to the language;
  the framework is responsible for mapping the transport's native payload to `TRequest`
  (see section 6 and each transport binding's serialization rules).
- The signature carries the request and nothing else. A handler that needs the message's
  headers or an invocation fact (cancellation, deadline, the resolved topic) reaches them through
  the invocation's scope — the context, or an accessor resolved from the scope — never through
  extra parameters a transport would have to supply.

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
| `isSuccessful` | boolean | Whether the invocation succeeded. Set by whoever sets the status; for a status in the vocabulary it MUST equal that status's class, and for an application-defined status it is the only signal of success — see [wire-contracts.md §1.2](wire-contracts.md#12-response) |
| `payload` | `TResponse?` | Present on success. On failure the wire carries the problem document in its place ([wire-contracts.md §1.3](wire-contracts.md#13-problem-details-payload)) |
| `errors` | structured, ordered list | Zero or more errors, populated on failure; each carries a human-readable message plus optional producer-owned `field`/`code`. A message-only projection remains available for readers that just want text. |

- The status vocabulary is **strings, not enums**, so applications can extend it; an unknown status
  maps to the transport's generic success or generic error code according to `isSuccessful` (each
  binding defines both).
- Results are values, not exceptions. Transport adapters translate a non-success status into the
  transport's native failure signal (HTTP status code, gRPC `RpcException`, ...) per the mapping
  tables in [wire-contracts.md](wire-contracts.md).
- `errors` is structured, not `string[]`, precisely so a caller can act on a failure rather than
  merely display it (render against the field that caused it, branch on a machine-readable code).
  The wire shape it serializes to is [wire-contracts.md §1.3](wire-contracts.md#13-problem-details-payload).

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

## 7. Application definition

- **One definition, several transports.** An application is defined once, platform-neutrally, and
  that one definition can mount several transports. A mount the current host cannot honour (an
  AWS Lambda mount on an Azure host, a gRPC mount in a worker) does nothing; it MUST NOT fail.
  This is what lets one definition move between vendors (transport-bindings §3).
- **Registration completes before the first message.** Handlers, middleware and mounts are
  registered, and the registration is validated, before the first invocation. A configuration
  error — two handlers for one (topic, version), a convention whose start-up verification fails
  (section 11) — fails start-up, never the first message.
- **No binding is required.** A pipeline can be invoked in process, with no transport at all, and
  is still a Benzene application.

## 8. Dependencies and scope

- **A scope per invocation.** Each pipeline invocation runs in its own scope; scoped dependencies
  live and die with it (section 4's batch rule makes that one scope per message).
- **Defaults are replaceable.** The framework registers its defaults so that an application's own
  registration wins over them.
- **Construction by declaration.** Handlers and middleware are constructed with their declared
  dependencies, per scope.

How a language provides this — a DI container, an explicit registry object, closures — is the
language's choice; the three properties above are what must survive.

## 9. Handler discovery

The **concept** is explicit registration: an application hands the framework a list of
(topic, version, handler, request type, response type) records.

Attribute/annotation scanning (`[Message("topic")]` in .NET) is an **idiom** — per-language sugar
that produces those records. Implementations MUST support explicit registration as a first-class
path; scanning is optional. Route-level attributes for specific transports (e.g.
`[GrpcMethod("/pkg.Service/Method")]`) are likewise sugar over an explicit
(route → topic) registration.

## 10. Optional capabilities

Some capabilities are optional, and a port need not offer them. One that does offers it in exactly
the shape the spec defines:

- **Versioning** — registering versioned handlers, and payload casting ([versioning.md](versioning.md)
  §5). Reading `benzene-version` and selecting by section 2 is not optional: every port does it.
- **W3C trace context** — continue a trace from inbound `traceparent`/`tracestate` headers and
  forward it on outbound calls. An implementation that propagates trace context MUST do so per the
  W3C Trace Context specification (wire-contracts §2); whether a service must propagate it at all is
  the [Cloud Service Profile](cloud-service-profile.md)'s R8, not a Core rule. (A per-invocation
  correlation value settable by application middleware and forwardable on outbound clients MAY
  additionally be offered, but inbound pickup of legacy correlation headers is not a framework
  contract.)
- **Health checks** — intercept the reserved health topics (wire-contracts §7), run the registered
  checks, and respond with the health response (wire-contracts §5).

## 11. Steers, shorthands and conventions

Benzene recommends a default for most things — the *steer* — and lets an application decline it.
Three rules make that safe:

- **A capability that depends on a steer degrades, and says how.** A capability that depends on a
  steer (message handlers, the default paths, a reserved topic) MUST state what it does when the
  steer is declined, and that behaviour MUST reduce the capability, never break the service or the
  tooling around it. The mesh's degradation rules ([mesh.md](mesh.md) §6) are the worked example;
  a derived contract document is the common one — without message handlers there is nothing to
  derive it from, so it is absent, not wrong.
- **A shorthand is composed from the explicit form.** Every capability has an explicit form in
  public API. A shorthand (a builder method, an attribute, scanning) MUST be built from that public
  form, so that an application could have written it itself and can drop one level down from it.
  Its documentation names the explicit form it composes.
- **Inference is verified before the first message.** A convention that infers something —
  scanning, discovery, convention over configuration — MUST be checked at start-up, and its failure
  names what was looked for, where, and what to add. A convention that can first fail on the
  message path is not permitted.

Every reserved header name is configurable in one place and applies in both directions
(wire-contracts §2).

## 12. Outbound

A service sends messages as well as receiving them.

- **The sender.** Every outbound client implements one operation —
  `send(topic, headers, message) → result` — whatever the transport underneath. Cross-cutting
  client behaviour (trace context, retry, correlation) is typically middleware on a send pipeline
  with the transport at the bottom, so it is the same for every transport. What a client must do on the wire is
  transport-bindings §1's rule for outbound clients.
- **Routing.** Which sender carries a topic is the application's choice. A port MAY offer a
  routing table from topic to sender, so that application code names only a topic and a message;
  it is not required.
- **Declared outbound.** A service MAY declare what it puts on the wire, as it registers handlers:
  *event declarations* for the topics it publishes and defines (each with its message type), and
  *send declarations* for messages to topics another party defines (each with its request type and,
  where a reply is expected, its response type) — the split is by who owns the contract
  ([contract-document.md](contract-document.md) §3). Declarations are what let a service describe
  what it produces without anyone reading its code; the mesh module requires them for a service
  that wants its outbound in the graph ([mesh.md](mesh.md) §2).
- **Error fidelity.** A client that receives a problem document — the body of a failure envelope,
  or an `application/problem+json` response — MUST rebuild the result's `errors` from it with
  `field` and `code` intact (wire-contracts §1.3). It MAY also attach the received document
  verbatim, which preserves an application's own problem `type` across the hop.
