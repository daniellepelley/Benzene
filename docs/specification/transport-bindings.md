# Transport Bindings

**Core**

A **transport binding** (a port adapter) connects one native transport to the Benzene model. This
document defines the contract every binding must satisfy, then catalogs the existing bindings as
worked examples. The catalog is deliberately multi-vendor: nothing in the binding contract is
AWS-, Azure-, or Google-specific, and a new vendor's transport is added by writing a binding, not
by changing the core.

## 1. The binding contract

Every binding MUST define:

1. **A context type** (core-concepts §6) carrying the native request/call and a result slot.
2. **Topic resolution** — how the native event yields a topic id (+ optional version). This is
   the binding's routing rule. A binding whose transport has its own routing key, or whose events
   carry the topic in their shape, MUST NOT require a `topic` attribute: EventBridge routes on
   `detail-type` and DynamoDB Streams derives `{tableName}:{eventName}`. Where each binding takes
   its topic from is pinned by `topicSources` in `conformance/transport-metadata-cases.json`.
3. **Header mapping** — native metadata → the flat header dictionary, and (where the transport is
   request/response) back again. Header conventions in wire-contracts §2 apply.
4. **Body mapping** — native payload → the handler's request type, per the request-mapping rules
   (core-concepts §6), including the binding's serialization rule.
5. **Result mapping** — the handler's result → the native response: payload conversion plus the
   status mapping (wire-contracts §4 for HTTP/gRPC; queue/event transports define their
   acknowledge/reject behavior instead).
6. **Scope rule** — exactly one pipeline invocation and one scope per unit of work (per
   request, per call, per message — *not* per batch).
7. **Failure rule** — an uncaught error inside the pipeline maps to the transport's native
   failure signal without crashing the host (and, for queue transports, without poisoning the
   whole batch).
8. **Settlement** — for a one-way binding (no reply channel: queues, topics, streams, event
   triggers), whether the transport may forget each message:
   - The binding MUST NOT acknowledge a message whose invocation ended with `isSuccessful: false`,
     with no result, or with an uncaught error; the transport redelivers it. A result with
     `isSuccessful: true` — `ignored` included — is acknowledged. Delivery is therefore
     at-least-once, and handlers on one-way bindings must be idempotent.
   - A message whose topic matches no handler is a `not-found` failure, and so is not
     acknowledged. A binding SHOULD offer an option that treats an unmatched topic as `ignored`,
     for subscribers on a shared bus that see other services' topics.
   - A middleware that ends the pipeline on a one-way binding MUST set a result; one that does not
     leaves no result, and the message is redelivered. A scheduled trigger is the exception: a tick
     carries no message to lose, so a binding MAY acknowledge a tick whose pipeline set no result.
   - A binding MUST offer an at-most-once override that acknowledges whatever the outcome.
   - A stream binding whose retention blocks the partition behind a failed message MAY default to
     skip-and-continue instead, and MUST document that it does.
   - A binding that checkpoints a batch or a position rather than single messages MUST define its
     unit of settlement: which messages one failure causes to be redelivered.
   - Dead-lettering is the transport's own redrive configuration (a maximum receive count, a
     dead-letter queue or topic). Benzene declines the acknowledgement; the transport decides when
     to give up.

   Each binding's shape, and the defaults each shape allows, are pinned by `settlementDefaults` in
   `conformance/transport-metadata-cases.json`. A request/response binding settles by responding.

A binding SHOULD also state its cancellation/deadline source (or that the transport has none) and
whether it supports response headers/trailers.

**One-way / fire-and-forget bindings.** A problem document (wire-contracts.md §1.3) is a
response-path artifact. A binding with no reply channel (SQS/SNS/EventBridge/Kafka/RabbitMQ
consumption, one-way sends) MUST NOT invent one just to carry a failure — that would fabricate a
response the caller never asked for and no receiver is listening on. Such failures surface where
they already do: the mesh issue feed (classification + exception type, mesh.md §4.1), traces and
logs, and the transport's own retry/dead-letter machinery. Where an application has wired an
explicit response-event channel on top of a one-way binding, a failure sent over that channel
carries the problem document like any other response body.

**The envelope endpoint.** A binding that accepts the envelope (wire-contracts §1) over HTTP —
`POST /benzene/invoke` by default — answers **200** for every envelope it processed, whatever the
result's status, with the response envelope as the body: the envelope carries the outcome
(`statusCode`, `isSuccessful`), and a client reads it, never the HTTP status line. A `deleted`
result is 200 with its envelope, not 204. A non-2xx status means the carrier failed and no envelope
was processed: a malformed envelope (400), or authentication the HTTP host enforces before it reads
the envelope (401, 403). An `unauthorized` or `forbidden` result from the pipeline's own
authentication middleware is a processed envelope, and so 200. A problem
document inside the envelope omits `status` (wire-contracts §1.3). The same holds for the envelope
over any other request/response carrier.

An envelope delivered by an **asynchronous** function invocation (a Lambda `Event` invocation, for
example) has no caller to answer, so it is a one-way binding and settles by obligation 8: a failure
fails the invocation, and the platform retries it. Platforms do not reveal to the function which kind
of invocation it received, so the asynchronous envelope is a separately configured entry point that
the application selects, not a runtime detection.

The envelope endpoint runs the same pipeline as the service's other transports, and so the same
authentication and authorization middleware. Because it reaches every handler the service
registers, including topics with no HTTP route of their own, it MUST support restricting which
topics are reachable through it; a topic outside the restriction is answered `not-found`, exactly
as if no handler matched.

**Default HTTP paths.** Every framework-provided HTTP surface SHOULD default to a path under
**`/benzene/`**, so that a URL, a log line or a gateway rule shows at once that a path is framework
infrastructure, and an operator can expose or protect all of them with one path rule. Every path is
configurable per service. The Cloud Service Profile makes these defaults a MUST (its R7).

| Path | Surface |
|---|---|
| `POST /benzene/invoke` | The envelope endpoint (above) |
| `GET /benzene/spec` | The reserved `benzene:spec` topic (wire-contracts §7); the `type` and `format` query parameters are its request |
| `GET /benzene/health` | The reserved `benzene:healthcheck` topic |
| `/benzene/spec-ui` | Spec browser (optional UI); its default spec URL is `/benzene/spec?type=benzene` |
| `/benzene/mesh-ui` | Mesh artifact viewer (optional UI) |
| `/benzene/fleet-ui` | Live fleet view, served by a collector (optional UI) |
| `/benzene/mcp` | The MCP endpoint (Provisional; [mcp.md](mcp.md)) |

Routes for `benzene:liveness` and `benzene:readiness` are the port's choice. Any route mapped to a
health topic answers **200** when the payload's `isHealthy` is true and **503** when it is false,
with the health response (wire-contracts §5) as the body either way. Health routes are the one place
an HTTP binding maps a payload field rather than the result's status, so that load balancers and
orchestrators that read only the status line see the verdict. A health invocation that itself
failed maps through wire-contracts §4.1 as usual.

**Outbound clients** — the reverse direction — MUST forward the header dictionary onto the native
metadata channel (this is what makes trace propagation work end-to-end) and map the native outcome
back to a result through the reverse status tables of wire-contracts §4. An envelope-over-HTTP
client reads the envelope from the response body whatever the HTTP status, and falls back to the
reverse HTTP table only when the body is not an envelope.

Bindings are **hosted** by a platform entry point (an ASP.NET Core server, a Lambda runtime, an
Azure Functions worker, a background worker loop). The application **mounts** a binding as part
of its one definition, and a mount the current host cannot honour does nothing (core-concepts §7).

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

- Topic: the event's `detail-type`, verbatim — EventBridge's native routing key (§1, obligation
  2). `source` is metadata, not part of the topic.
- One event per invocation (EventBridge does not batch Lambda targets) → one pipeline invocation,
  one scope; fire-and-forget (no response channel).
- Body: the raw JSON of `detail` (the domain payload).
- Headers: envelope metadata under `eventbridge-`-prefixed keys, plus Benzene wire headers lifted
  from the reserved `benzene-headers` object inside `detail` (wire-contracts §2) — EventBridge has
  no native per-message attributes, so the outbound client embeds them there. Embedded headers win
  over prefixed envelope keys.
- Outbound: `PutEvents` with topic → `DetailType`; success requires an OK status **and** zero
  failed entries (a `PutEvents` request can partially fail).

### DynamoDB Streams — `Benzene.Aws.Lambda.DynamoDb`

- Topic: `"{tableName}:{eventName}"` (e.g. `orders:INSERT`) — the table parsed from the stream
  ARN plus the change type (`INSERT`/`MODIFY`/`REMOVE`), the two things that identify a CDC event
  (§1, obligation 2).
- Body: the record's image unmarshalled from DynamoDB AttributeValue format into plain JSON
  (`NewImage`, else `OldImage`, else `Keys` — the most complete state available), so handlers
  deserialize ordinary POCOs.
- Headers: envelope metadata under `dynamodb-`-prefixed keys. **No** `benzene-headers`
  convention: these events originate from table writes, not a Benzene publisher, so there is no
  producer side to embed wire headers.
- Batching: records are ordered CDC within a shard → processed **sequentially, stopping at the
  first failure**, whose `SequenceNumber` is reported as the partial batch failure (Lambda
  checkpoints there and redelivers). Deliberately not the SQS binding's concurrent fan-out.
- Outbound: none — writing to the table is the publish operation; the stream is read-only.

### Azure Functions — `Benzene.Azure.Function.*`

Same shape as Lambda from the model's perspective: one isolated-worker host, inner bindings for
HTTP, Event Hub, Service Bus, and Kafka triggers.

### Kafka (self-hosted consumer) — `Benzene.Kafka.Core`

Topic from message metadata/envelope; headers from Kafka headers (UTF-8); no response channel —
result mapping is settlement (§1, obligation 8): a partition-ordered stream, so a failed message
blocks the ones behind it.

### RabbitMQ (self-hosted consumer) — `Benzene.RabbitMq`

- Topic: the delivery's `topic` header (matching the SQS/SNS/Service Bus convention above), falling
  back to the AMQP routing key so a non-Benzene producer's message still routes.
- Headers: `BasicProperties.Headers`, UTF-8 decoded (RabbitMQ carries header values as `byte[]` on
  the wire); the outbound client mirrors this so header-based decorators round-trip.
- One delivery per invocation, one scope; no response channel — result mapping is
  acknowledge/nack (§1, obligation 8).

### Cosmos DB Change Feed — `Benzene.Azure.CosmosDb` / `Benzene.Azure.Function.CosmosDb`

Not topic-routed: this binding is **streaming-shaped** (core-concepts §3), not handler-shaped —
each delivered change-feed batch is one `StreamContext<TDocument>` invocation, fanned in rather
than dispatched by topic. Checkpointing is batch-level (the change feed has no per-document resume
token); the self-hosted worker exposes a real checkpoint hook, the Azure Functions trigger
checkpoints on successful return only. Present as two bindings sharing one pipeline shape, matching
the Functions-trigger / self-hosted-worker split every other Azure transport has.

### MCP (Model Context Protocol)

AI clients call a service's handlers as MCP tools. MCP is a binding that satisfies §1, specified in
its own document: [mcp.md](mcp.md) (Provisional).

### Outbound clients (the reverse direction)

Every outbound client implements one interface — `sendMessage(topic, headers, message) → result`
— over a **send pipeline** with a transport middleware at the bottom: HTTP, Lambda invoke
(envelope of wire-contracts §1), SQS, SNS, Kafka produce, gRPC unary. Header forwarding and the
reverse status mapping are §1's rule for outbound clients. Cross-cutting client behaviors (correlation ID injection, trace context, retry) are decorators
over the same interface and therefore transport-agnostic.

## 3. Multi-vendor rules

1. **The core never references a vendor.** Vendor SDK types appear only inside a binding package.
2. **Vendor parity is defined by this spec, not by feature-matching another vendor's binding.**
   A binding is complete when it satisfies §1, not when it mirrors what the AWS binding does.
3. **Application code moves between vendors by swapping its mounts** (often just mounting both —
   each does nothing where inapplicable). Handlers, middleware, topics, and clients' call sites do
   not change.
4. **Interop across vendors** rides on wire-contracts: a service on AWS and a service on Azure
   exchange the same envelope, headers, and status vocabulary.
