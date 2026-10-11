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
6. **Scope rule** — exactly one pipeline invocation and one DI scope per unit of work (per
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
     leaves no result, and the message is redelivered.
   - A binding MUST offer an at-most-once override that acknowledges whatever the outcome.
   - A stream binding whose retention blocks the partition behind a failed message MAY default to
     skip-and-continue instead, and MUST document that it does.
   - A binding that checkpoints a batch or a position rather than single messages MUST define its
     unit of settlement: which messages one failure causes to be redelivered.
   - Dead-lettering is the transport's own redrive configuration (a maximum receive count, a
     dead-letter queue or topic). Benzene declines the acknowledgement; the transport decides when
     to give up.

   Each binding's defaults are pinned by `settlementDefaults` in
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
was processed: a malformed envelope (400), or authentication on the carrier (401, 403). A problem
document inside the envelope omits `status` (wire-contracts §1.3). The same holds for the envelope
over any other request/response carrier.

The envelope endpoint runs the same pipeline as the service's other transports, and so the same
authentication and authorization middleware. Because it reaches every handler the service
registers, including topics with no HTTP route of their own, it MUST support restricting which
topics are reachable through it; a topic outside the restriction is answered `not-found`, exactly
as if no handler matched.

**HTTP default mappings for the reserved topics.** An HTTP binding maps the reserved topics
(wire-contracts §7) to these routes by default, each configurable like any other route:

| Route | Topic |
|---|---|
| `GET /benzene/health` | `benzene:healthcheck` |
| `GET /benzene/health/live`, `GET /benzene/health/ready` | `benzene:liveness`, `benzene:readiness`, where served |
| `GET /benzene/spec` | `benzene:spec`; the `type` and `format` query parameters are its request |

The health routes answer **200** when the payload's `isHealthy` is true and **503** when it is
false, with the health response (wire-contracts §5) as the body either way. They are the one place
an HTTP binding maps a payload field rather than the result's status, so that load balancers and
orchestrators that read only the status line see the verdict. A health invocation that itself
failed maps through wire-contracts §4.1 as usual.

**Outbound clients** — the reverse direction — MUST forward the header dictionary onto the native
metadata channel (this is what makes trace propagation work end-to-end) and map the native outcome
back to a result through the reverse status tables of wire-contracts §4. An envelope-over-HTTP
client reads the envelope from the response body whatever the HTTP status, and falls back to the
reverse HTTP table only when the body is not an envelope.

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

### MCP (Model Context Protocol) — `Benzene.Mcp`

AI clients (assistants, IDEs, agents) call a service's handlers as MCP **tools**, over JSON-RPC 2.0
on Streamable HTTP (hosted by any HTTP binding above) or on stdio (hosted by the worker).

- **Tools, not topics.** The binding advertises a curated list of tools the application declares,
  never every topic (a hundred schemas in front of a model is a worse interface than none). A tool
  is either **topic-bound** (name → topic, the arguments object is the body) or **custom** (a body
  the application wrote, which asks the pipeline through the same dispatcher).
- Topic: the tool's declared topic (+ optional version, sent as `benzene-version`). Headers:
  `benzene-mcp-tool` (the tool name) and `benzene-mcp-session` (the session id, where the transport
  has one; wire-contracts §2); there is no native metadata channel for the caller to set headers on.
- Body: the `tools/call` `arguments` object, verbatim, as the request body. Arguments are read
  strictly; a value the model got wrong is refused with a sentence naming what was expected.
- Result: a success → the response body as the tool's text; a failure → a tool result marked
  `isError` whose text is `"{status}: {detail}"` (wire-contracts §1.3's problem `detail`), which the
  model reads. JSON-RPC errors are reserved for the protocol itself (parse, invalid request/params,
  unknown method) and for an unexpected exception, which carries a short reference and never the
  detail.
- Scope: one pipeline invocation and one DI scope per tool call (a custom tool that asks the
  pipeline twice makes two). Cancellation: the HTTP request's abort signal; stdio has none.
- Sessions: `Mcp-Session-Id` is issued on `initialize` and echoed on later requests. The binding
  keeps no session state; the id is passed to tools and is the application's to interpret.
- Annotations: `readOnlyHint`/`idempotentHint` are the inverse of the tool's declared `writes`,
  `destructiveHint` its declared `destructive`, so a client can gate the writers.
- Not offered: the server-to-client stream (`GET` → `405`), resources, prompts.
- A start-up check fails when a topic-bound tool names a topic no handler answers.

*Holding note — MCP is Provisional (one port, .NET) and gets its own document.* Until then, an MCP
binding MUST NOT advertise every topic as a tool; MUST return a handler failure as a tool result
marked `isError`, never as a JSON-RPC error; and MUST fail start-up when a topic-bound tool names a
topic no handler answers.

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
3. **Application code moves between vendors by swapping the `use<Transport>` calls** in
   `configure` (often just adding both — each no-ops where inapplicable). Handlers, middleware,
   topics, and clients' call sites do not change.
4. **Interop across vendors** rides on wire-contracts: a service on AWS and a service on Azure
   exchange the same envelope, headers, and status vocabulary.
