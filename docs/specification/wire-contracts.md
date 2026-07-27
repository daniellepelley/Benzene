# Wire Contracts

**Status: DRAFT v0.1**

Everything in this document crosses a process boundary. These are the contracts that make two
Benzene implementations — in any two languages, on any two vendors — interoperable. From spec 1.0,
changes here are breaking changes.

All JSON field names below are camelCase unless stated otherwise. All header keys are
case-insensitive on read and SHOULD be written lower-case.

## 1. The Benzene message envelope

The transport-neutral message format, used whenever a Benzene client sends to a Benzene service
over a transport with no richer native contract (direct function invocation, queues without
attribute support, the generic `BenzeneMessage` entry point).

### 1.1 Request

```json
{
  "topic": "order:create",
  "headers": { "x-correlation-id": "…", "traceparent": "…" },
  "body": "{ …serialized message… }"
}
```

| Field | Type | Rules |
|---|---|---|
| `topic` | string | Required. The topic id (see core-concepts §2). Version, when used, travels as a header. |
| `headers` | object (string→string) | Required, may be empty. Flat string map — no nested values. |
| `body` | string | Required. The message payload, **pre-serialized as a string** (JSON by default), not an inline object. This keeps the envelope schema fixed regardless of payload schema. |

*(Informative: earlier .NET versions had the outbound Lambda client sending this field as
`message` while the inbound entry point read `body` — corrected to `body` on both sides; `body`
is normative.)*

### 1.2 Response

```json
{
  "statusCode": "ok",
  "headers": { },
  "body": "{ …serialized response… }"
}
```

| Field | Type | Rules |
|---|---|---|
| `statusCode` | string | A status vocabulary value (§3) — the *Benzene* status, not an HTTP code. Clients MAY additionally tolerate numeric HTTP codes here for interop with older or HTTP-shaped services, but MUST NOT write them. |
| `headers` | object (string→string) | Response headers, including `content-type` when set. |
| `body` | string | Pre-serialized response payload: on success, the handler's response payload; on failure, the error payload (§1.3). |

### 1.3 Error payload

When a result is unsuccessful, the response `body` is the serialized error payload — a
problem-details-shaped object:

```json
{
  "status": "not-found",
  "detail": "No handler found for topic order:create"
}
```

| Field | Type | Rules |
|---|---|---|
| `status` | string | The Benzene status, repeated from the envelope. |
| `detail` | string | The result's error messages, joined with `", "`. |
| `type`, `title`, `instance` | string? | Reserved (RFC 7807 alignment); writers MAY emit them as `null` or omit them. |

Clients recover `errors` from `detail`; a missing/empty `detail` yields an error-free failed
result.

## 2. Header conventions

Headers are the portable metadata channel. Every transport binding maps its native metadata
(HTTP headers, gRPC metadata, SQS/SNS message attributes, Kafka headers, the envelope's `headers`
field) to and from this flat string→string dictionary.

Every header below is labelled with the **tier** that says who must implement it. Without this, a
porting author cannot tell a mandatory wire contract from a convention of one optional middleware:

| Tier | Meaning |
|---|---|
| **A — core** | Part of the wire contract. An implementation that omits it cannot interoperate. |
| **B — profile** | Required to be a conformant Cloud Service ([cloud-service-profile.md](cloud-service-profile.md)), not to interoperate at the message level. |
| **C — add-on** | Only meaningful if the application wired the corresponding optional middleware. Benzene neither requires nor fabricates these. |
| **D — binding** | A detail of one transport binding, specified where that binding is ([transport-bindings.md](transport-bindings.md), or §4 here for the protocol mappings). |

| Header | Tier | Direction | Meaning |
|---|---|---|---|
| `topic` | **A** | inbound (queue/stream transports) | On transports where the envelope isn't used but native metadata exists (SQS/SNS message attributes, Service Bus/Event Hub properties, Kafka/RabbitMQ headers, Pub/Sub attributes), the topic travels as an attribute of this name — the same spelling as the envelope field, so one concept has one name wherever it appears. The routing key: without it such a transport cannot dispatch. **Configurable** — see *Reserved names are defaults* below. |
| `content-type` | **A** | outbound | Response content type where the transport has no native slot for it. A borrowed name (HTTP/MIME), used verbatim. |
| `benzene-version` | **C** | both | The payload's schema version, for topics using payload versioning. Read from an ordered, configurable fallback list — default `benzene-version`, then `version`, then `x-version` — and written as `benzene-version`. Only meaningful for a service that opted into versioning; see [versioning.md](versioning.md) for the fallback-list rules and why the list must be configurable. |
| `traceparent`, `tracestate` | **C** | both | W3C Trace Context. Benzene does not define these and does not require them. **If** an implementation propagates trace context, it MUST do so verbatim per the W3C specification — that verbatim-ness is what makes traces from different languages join up, which the mesh depends on. Benzene never fabricates a trace context that wasn't there. |
| `x-correlation-id` | **C** | outbound | A business correlation value, written by the outbound correlation client decorator when the application populates one. Implementations are NOT required to read it inbound; honouring a partner's correlation header is application middleware, not a framework contract. One convention among several — Benzene does not own this name. |
| `_benzeneHeaders` | **D** | both (EventBridge) | On transports with no native per-message metadata (EventBridge), wire headers travel as a reserved string→string object named `_benzeneHeaders` at the top level of the payload (`detail`), embedded by the sender only when headers exist and the payload is a JSON object, and lifted back out by the receiver. Its form differs deliberately: it is a **JSON field**, so it follows the camelCase JSON convention rather than the kebab-case header one, with the leading underscore marking it reserved inside a payload the application owns. |

`benzene-status` is **not** listed here: it is a gRPC-only trailer, specified with the gRPC mapping
it serves in [§4.2](#42-grpc). It appeared in this table historically, which made it read as a
universal header.

**Naming.** Names Benzene invents carry the `benzene-` marker, because a header sits in a namespace
shared with the application and the transport. Names Benzene *borrows* — `content-type`,
`traceparent`, `tracestate`, `x-correlation-id` — are never renamed: interoperating with the
standard is the entire reason for using them. (The full rule is in
`work/benzene-naming-principle.md`.)

`topic` is the deliberate exception: it keeps the envelope field's spelling so that one concept has
one name wherever it travels. The collision the marker guards against — an application that already
puts its own `topic` attribute on a message — is handled instead by making the name configurable.

**Reserved names are defaults.** Every name in this table is a *default*, not a literal an
implementation may hard-code. An implementation MUST expose them as a single injectable value, so a
service can replace one in one place rather than at each binding. Two consequences follow, and both
are normative:

- **The defaults carry interop.** Two Benzene services that have not changed anything must
  interoperate. A service that overrides a name is opting out of that, and is responsible for
  agreeing the change with whatever it talks to.
- **An override applies to both directions.** The same value MUST be used by the service's inbound
  bindings and its outbound clients. A service that overrode only one side would send messages it
  cannot itself receive, and the symptom — a message that arrives and never routes — looks
  identical to a missing handler. Implementations SHOULD name the configured key in the
  unresolved-topic error for exactly this reason.

Binary metadata (e.g. gRPC `-bin` keys) is excluded from the dictionary in both directions.
Duplicate keys: last value wins.

## 3. Status vocabulary

The closed set of framework-defined statuses. The strings below are the wire values — they are
**case-sensitive**. The vocabulary is lowercase-kebab-case (e.g. `not-found`, `validation-error`).

| Status | Success? | Meaning |
|---|---|---|
| `ok` | yes | Handled successfully |
| `created` | yes | Resource created |
| `accepted` | yes | Accepted for asynchronous processing |
| `updated` | yes | Resource updated |
| `deleted` | yes | Resource deleted |
| `ignored` | yes | Deliberately not processed (e.g. filtered); not an error |
| `bad-request` | no | Malformed or invalid request |
| `validation-error` | no | Semantically invalid request (validation rules failed) |
| `unauthorized` | no | Caller not authenticated |
| `forbidden` | no | Caller authenticated but not permitted |
| `not-found` | no | Target not found (including: no handler registered for the topic) |
| `conflict` | no | State conflict |
| `too-many-requests` | no | Throttled / rate limited; transient — back off and retry |
| `timeout` | no | A downstream deadline elapsed; transient, but the operation may or may not have been applied, so blind retries are only safe for idempotent operations |
| `not-implemented` | no | Recognized but unsupported operation |
| `service-unavailable` | no | Transient infrastructure failure; retryable. Also the mapping for uncaught handler exceptions and client-side send failures. |
| `unexpected-error` | no | Unclassified failure |

Applications MAY use additional status strings; every mapping table below routes unknown statuses
to its generic-error row.

## 4. Per-protocol status mappings

### 4.1 HTTP

| Benzene status | HTTP |
|---|---|
| `ok`, `ignored` | 200 |
| `created` | 201 |
| `accepted` | 202 |
| `updated`, `deleted` | 204 |
| `bad-request` | 400 |
| `unauthorized` | 401 |
| `forbidden` | 403 |
| `not-found` | 404 |
| `conflict` | 409 |
| `validation-error` | 422 |
| `too-many-requests` | 429 |
| `unexpected-error`, unknown, missing | 500 |
| `not-implemented` | 501 |
| `service-unavailable` | 503 |
| `timeout` | 504 |

Reverse (HTTP → Benzene, used by HTTP clients): 200→`ok`, 201→`created`, 202→`accepted`,
204→`deleted`, 400→`bad-request`, 401→`unauthorized`, 403→`forbidden`, 404→`not-found`,
408→`timeout`, 409→`conflict`, 422→`validation-error`, 429→`too-many-requests`,
501→`not-implemented`, 502→`service-unavailable`, 503→`service-unavailable`, 504→`timeout`,
anything else→`unexpected-error`.

### 4.2 gRPC

Forward (server):

| Benzene status | gRPC `StatusCode` |
|---|---|
| `ok`, `ignored`, `created`, `accepted`, `updated`, `deleted` | `OK` |
| `bad-request`, `validation-error` | `InvalidArgument` |
| `unauthorized` | `Unauthenticated` |
| `forbidden` | `PermissionDenied` |
| `not-found` | `NotFound` |
| `conflict` | `AlreadyExists` |
| `not-implemented` | `Unimplemented` |
| `service-unavailable` | `Unavailable` |
| `too-many-requests` | `ResourceExhausted` |
| `timeout` | `DeadlineExceeded` |
| `unexpected-error`, unknown, missing | `Internal` |

**The `benzene-status` trailer**: because several Benzene statuses collapse to one gRPC code, a
Benzene gRPC server MUST attach a response trailer `benzene-status` carrying the raw status string
verbatim, on success and failure alike. A missing result maps the trailer value to `Unknown`.
Non-`OK` outcomes are surfaced as a gRPC error with the mapped code and a detail string of the
joined `errors` (or the raw status if `errors` is empty).

Reverse (client): a `benzene-status` trailer, when present, wins verbatim. Otherwise: `OK`→`ok`,
`InvalidArgument`→`bad-request`, `Unauthenticated`→`unauthorized`, `PermissionDenied`→`forbidden`,
`NotFound`→`not-found`, `AlreadyExists`→`conflict`, `Unimplemented`→`not-implemented`,
`Unavailable`/`Cancelled`→`service-unavailable`, `ResourceExhausted`→`too-many-requests`,
`DeadlineExceeded`→`timeout`, anything else→`unexpected-error`.

**Cancellation**: a cancelled invocation maps to gRPC `DeadlineExceeded` if the call's deadline
has passed, else `Cancelled`.

## 5. Health check response

Returned for the reserved topic `benzene:healthcheck` (and any app-configured alias):

```json
{
  "isHealthy": true,
  "healthChecks": {
    "Database": {
      "status": "ok",
      "type": "Database",
      "data": { "CanConnect": true }
    }
  }
}
```

- `status` per check is one of `"ok"`, `"warning"`, `"failed"` (lower-case — note this is a
  *different* vocabulary from §3).
- `isHealthy` is true iff no check reports `"failed"`; `"warning"` does not flip it.
- `healthChecks` keys are check names, deduplicated with `-2`/`-3` suffixes on collision.
- `data` is a free-form diagnostic bag; its keys are written verbatim (no naming policy applied).

gRPC hosts additionally expose the same aggregate over standard
[grpc.health.v1](https://github.com/grpc/grpc/blob/master/doc/health-checking.md): `SERVING` iff
no check failed (a warning maps to a degraded-but-serving state).

## 6. Serialization defaults

- Default payload encoding is JSON, UTF-8.
- Writing: camelCase property names. Writers MAY omit null-valued properties or emit them as
  `null`; readers MUST accept both.
- Reading: property-name matching is case-insensitive.
- gRPC payload bridging uses **protobuf's own JSON mapping**
  ([proto3 JSON](https://protobuf.dev/programming-guides/proto3/#json)) between protobuf messages
  and plain types — not a naive reflection round-trip — so enums, well-known types, and oneofs
  convert per protobuf rules. Property matching is against the protobuf JSON names.
