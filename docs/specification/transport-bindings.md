# Transport Bindings

**Status: DRAFT v0.1**

A **transport binding** (a port adapter) connects one native transport to the Benzene model. This
document defines the contract every binding must satisfy, then catalogs the existing bindings as
worked examples. The catalog is deliberately multi-vendor: nothing in the binding contract is
AWS-, Azure-, or Google-specific, and a new vendor's transport is added by writing a binding, not
by changing the core.

## 1. The binding contract

Every binding MUST define:

1. **A context type** (core-concepts §6) carrying the native request/call and a result slot.
2. **Topic resolution** — how the native event yields a topic id (+ optional version). This is
   the binding's routing rule.
3. **Header mapping** — native metadata → the flat header dictionary, and (where the transport is
   request/response) back again. Header conventions in wire-contracts §2 apply.
4. **Body mapping** — native payload → the handler's request type, per the request-mapping rules
   (core-concepts §6), including the binding's serialization rule.
5. **Result mapping** — the handler's result → the native response: payload conversion plus the
   status mapping (wire-contracts §4 for HTTP/gRPC; queue/event transports define their
   acknowledge/reject behavior instead).
6. **Scope rule** — exactly one pipeline invocation and one DI scope per unit of work (per
   request, per call, per message — *not* per batch).
7. **Failure rule** — an uncaught error inside the pipeline maps to the transport's native
   failure signal without crashing the host (and, for queue transports, without poisoning the
   whole batch).

A binding SHOULD also state its cancellation/deadline source (or that the transport has none) and
whether it supports response headers/trailers.

Bindings are **hosted** by a platform entry point (an ASP.NET Core server, a Lambda runtime, an
Azure Functions worker, a background worker loop). Hosting is attached in the application's
`configure` phase via a `use<Transport>(...)` extension that no-ops on other platforms
(core-concepts §7).

## 2. Existing bindings *(informative)*

### HTTP (ASP.NET Core) — `Benzene.AspNet.Core` / `Benzene.Http`

- Topic: resolved from route/method conventions.
- Headers: HTTP headers, both directions.
- Status: wire-contracts §4.1 table.
- Scope: per request. Cancellation: the request abort token.

### gRPC — `Benzene.Grpc` (+ `.AspNet` hosting)

- Topic: full method path `/package.Service/Method` → topic, matched case-insensitively, built
  from explicit (route → topic) registrations (attribute sugar in .NET). Unmatched methods fall
  through to the native generated service — the binding claims routes, it doesn't own the server.
- All four RPC shapes; stream sides surface as async streams of items (core-concepts §3). One
  pipeline invocation per call regardless of shape or stream length.
- Headers: request metadata in (binary keys skipped); buffered response headers +
  pass-through trailers out, including the mandatory `benzene-status` trailer.
- Body: protobuf pass-through or proto3-JSON bridging, per side, per item.
- Status: wire-contracts §4.2, including cancellation → `DeadlineExceeded`/`Cancelled`.

### AWS Lambda — `Benzene.Aws.Lambda.*`

One host (the Lambda runtime), several inner bindings selected by event shape: API Gateway
(HTTP-like: topic from route, headers from HTTP headers), SQS / SNS / Kafka batches (topic from
the `topic` message attribute or envelope; one scope per record), S3 events, EventBridge (below),
and the raw `BenzeneMessage` envelope (wire-contracts §1) for direct invocation.

### EventBridge — `Benzene.Aws.Lambda.EventBridge`

- Topic: the event's `detail-type`, verbatim — EventBridge's native routing key, so this binding
  needs no bolted-on `topic` attribute. `source` is metadata, not part of the topic.
- One event per invocation (EventBridge does not batch Lambda targets) → one pipeline invocation,
  one scope; fire-and-forget (no response channel).
- Body: the raw JSON of `detail` (the domain payload).
- Headers: envelope metadata under `eventbridge-`-prefixed keys, plus Benzene wire headers lifted
  from the reserved `_benzeneHeaders` object inside `detail` (wire-contracts §2) — EventBridge has
  no native per-message attributes, so the outbound client embeds them there. Embedded headers win
  over prefixed envelope keys.
- Outbound: `PutEvents` with topic → `DetailType`; success requires an OK status **and** zero
  failed entries (a `PutEvents` request can partially fail).

### Azure Functions — `Benzene.Azure.Function.*`

Same shape as Lambda from the model's perspective: one isolated-worker host, inner bindings for
HTTP, Event Hub, Service Bus, and Kafka triggers.

### Kafka (self-hosted consumer) — `Benzene.Kafka.Core`

Topic from message metadata/envelope; headers from Kafka headers (UTF-8); no response channel —
result mapping is acknowledge/log only.

### Outbound clients (the reverse direction)

Every outbound client implements one interface — `sendMessage(topic, headers, message) → result`
— over a **send pipeline** with a transport middleware at the bottom: HTTP, Lambda invoke
(envelope of wire-contracts §1), SQS, SNS, Kafka produce, gRPC unary. Clients MUST forward the
header dictionary onto the native metadata channel (this is what makes correlation/trace
propagation work end-to-end) and map the native outcome back through the reverse status tables.
Cross-cutting client behaviors (correlation ID injection, trace context, retry) are decorators
over the same interface and therefore transport-agnostic.

## 3. Multi-vendor rules

1. **The core never references a vendor.** Vendor SDK types appear only inside a binding package.
2. **Vendor parity is defined by this spec, not by feature-matching another vendor's binding.**
   A binding is complete when it satisfies §1, not when it mirrors what the AWS binding does.
3. **Application code moves between vendors by swapping the `use<Transport>` calls** in
   `configure` (often just adding both — each no-ops where inapplicable). Handlers, middleware,
   topics, and clients' call sites do not change.
4. **Interop across vendors** rides on wire-contracts: a service on AWS and a service on Azure
   exchange the same envelope, headers, and status vocabulary.
