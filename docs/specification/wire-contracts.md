# Wire Contracts

**Core**

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

### 1.2 Response

```json
{
  "statusCode": "ok",
  "isSuccessful": true,
  "headers": { },
  "body": "{ …serialized response… }"
}
```

| Field | Type | Rules |
|---|---|---|
| `statusCode` | string | A status vocabulary value (§3) — the *Benzene* status, not an HTTP code. Clients MAY additionally tolerate numeric HTTP codes here for interop with older or HTTP-shaped services, but MUST NOT write them. |
| `isSuccessful` | boolean | Required. **The** success/failure signal, set by whoever sets the status. For a status in §3's vocabulary it MUST equal that status's Success? column — there are no exceptions; a result that wants to carry a payload with a failure status is a failure. For an application-defined status it is the only signal there is. **A receiver MUST take this field as authoritative and MUST NOT reclassify from `statusCode` text**, which means nothing to a receiver for a status outside the shared vocabulary. An envelope without it is malformed, and a receiver treats it as unsuccessful. |
| `headers` | object (string→string) | Response headers, including `content-type` when set. |
| `body` | string | Pre-serialized response payload: on success, the handler's response payload; on failure, the error payload (§1.3). |

### 1.3 Problem details payload

When a result is unsuccessful (`isSuccessful: false`, §1.2), the response `body` **is** the
serialized problem document — **replace, not wrap**: the failure payload takes the place a success
payload would otherwise occupy, and a response never carries both.

The payload is a **valid [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) problem document** on
every transport — not "problem-details-shaped", the genuine standard, adopted as a
transport-neutral profile:

```json
{
  "type": "https://benzene.app/problems/validation-error",
  "title": "Validation failed",
  "status": 422,
  "detail": "Name must not be empty, Age must be greater than 0",
  "benzeneStatus": "validation-error",
  "errors": [
    { "message": "Name must not be empty",     "field": "/name", "code": "NotEmptyValidator" },
    { "message": "Age must be greater than 0", "field": "/age",  "code": "GreaterThanValidator" }
  ]
}
```

| Field | Type | Rules |
|---|---|---|
| `type` | string (URI ref) | Framework-produced failures MUST use the registry URI for the status (§3.1). Application-authored problems SHOULD use their own absolute URI; absent or `about:blank` is tolerated on read. Readers treat it as an **opaque identifier** — comparison is string equality, never dereference (the registry URIs are not live pages). |
| `title` | string | Short human summary of the *type*, fixed per type; the wording is free. Never asserted by conformance fixtures — wording is free. |
| `status` | integer | **Only where the problem document is itself an HTTP response body** (a route-mapped HTTP response, §4.1), and then it MUST equal that response's status code. Everywhere else it MUST be **omitted** — not emitted as `null`: inside an envelope on any carrier, HTTP included (the envelope endpoint answers 200, transport-bindings §1), and in a queue reply. Benzene clients MUST NOT classify a result from this member; classification is envelope-first (§1.2). |
| `detail` | string | Human-readable occurrence detail — the result's error messages joined with `", "`, unchanged from every prior version of this document. The compatibility member: every existing reader can keep using only this one. |
| `instance` | string (URI ref) | Optional, application-owned. The framework never fabricates it. |
| `benzeneStatus` | string | **Required.** The §3 status string, mirroring the envelope's `statusCode` (§1.2). The transport-neutral discriminator — present on every transport regardless of whether `status` is. It carries the `benzene` marker because this member namespace is shared with RFC 9457 itself and with applications (the naming rule of §2's "Naming" paragraph); `errors`, below, is Benzene's own extension and stays unmarked. |
| `errors` | array | Optional. When present, **authoritative and ordered** — supersedes the "recover `errors` from `detail`" rule this document carried previously (withdrawn, see below). Each item: `message` (string, required), `field` (string, optional — see **The `field` pointer** below), `code` (string, optional — a machine-readable, producer-owned rule identifier, emitted verbatim, never normalized or reworded by the framework). |
| *(extensions)* | any | Applications MAY add further members (RFC 9457 §3.2). Readers MUST ignore unknown members. Neither `code` nor `type` participates in the mesh issue fingerprint ([mesh.md](mesh.md) §4.1). |

**The `field` pointer.** `errors[].field` is a [JSON Pointer](https://www.rfc-editor.org/rfc/rfc6901)
to the offending value in the handler's request, addressed in the request type's **JSON form** —
property names as the service's serializer writes them, its naming policy and any rename
attributes applied: `/name`, `/items/0/sku`. Without a payload cast a caller can resolve it against
the body it sent; after one (versioning.md) it addresses the handler's version of the type. A rule that cannot be resolved to a single location (a cross-field rule, a rule on the whole
request) omits `field` rather than inventing one. Every validation integration emits pointers; one
whose library reports host-language property paths converts them.

**Unknown-member tolerance.** A reader MUST ignore any problem-document member it does not
recognize, framework-defined or application-added alike — this is what lets applications extend
the document (RFC 9457 §3.2) without breaking older readers, and what lets a future framework
member arrive without a version bump.

**Signalling.** The envelope (§1.2) is the failure signal, not the body's content type:
`isSuccessful: false` is what tells a receiver this body is a problem document. The envelope's inner `headers.content-type` SHOULD be `application/problem+json`
when the response has one; readers MUST NOT require it — the outer transport content-type (e.g.
the HTTP body carrying the envelope itself) is a separate concern and stays whatever it already is
(§4.1 covers the case where the transport response itself *is* the problem document, i.e. HTTP).

**Without `errors`.** A reader of a problem document with no `errors` member treats `detail` as a
single opaque message — it never splits `detail` into errors, since messages contain commas — and a
missing or empty `detail` yields a failed result with no errors.

## 2. Header conventions

Headers are the portable metadata channel. Every transport binding maps its native metadata
(HTTP headers, gRPC metadata, SQS/SNS message attributes, Kafka headers, the envelope's `headers`
field) to and from this flat string→string dictionary.

Every header below is classified by who must implement it: **Core** (every implementation that
uses the capability — an implementation that omits it cannot interoperate), a **Module** (only an
implementation of that module, e.g. claim check), or a **Binding** (only that transport binding,
e.g. EventBridge or MCP).

| Header | Class | Direction | Meaning |
|---|---|---|---|
| `topic` | Core | inbound (queue/stream transports) | On transports where the envelope isn't used but native metadata exists (SQS/SNS message attributes, Service Bus/Event Hub properties, Kafka/RabbitMQ headers, Pub/Sub attributes), the topic travels as an attribute of this name — the same spelling as the envelope field, so one concept has one name wherever it appears. The routing key: without it such a transport cannot dispatch. **Configurable** — see *Reserved names are defaults* below. |
| `content-type` | Core | outbound | Response content type where the transport has no native slot for it. A borrowed name (HTTP/MIME), used verbatim. |
| `benzene-version` | Core | both | The message's version: the one signal that selects the handler (core-concepts §2) and the payload's schema version (payload casting, [versioning.md](versioning.md)). One name, read and written as `benzene-version` and configurable like every name in this table — there is no fallback list, so an unrelated `version` or `x-version` header a caller sends is an ordinary header, never a version. Absent means the topic's default handler (core-concepts §2). |
| `traceparent`, `tracestate` | Core | both | W3C Trace Context. Benzene does not define these, and Core does not require them (the Cloud Service Profile's R8 does). **If** an implementation propagates trace context, it MUST follow the W3C Trace Context format and semantics: the trace-id and `tracestate` are carried forward unchanged, and the parent-id is set to the participant's own span. That is what makes traces from different languages join up, which the mesh depends on. |
| `x-correlation-id` | Core | outbound | A business correlation value, written by the outbound correlation client decorator when the application populates one. Implementations are NOT required to read it inbound; honouring a partner's correlation header is application middleware, not a framework contract. One convention among several — Benzene does not own this name. |
| `benzene-headers` | Binding: EventBridge | both (EventBridge) | On transports with no native per-message metadata (EventBridge), wire headers travel as a reserved string→string object named `benzene-headers` at the top level of the payload (`detail`), embedded by the sender only when headers exist and the payload is a JSON object, and read back into the header dictionary by the receiver. The body the handler receives is `detail` unchanged, the embedded member included, so a handler's request type must tolerate it. Although its carrier is a payload field, it **names a header** rather than being one of the payload's own fields, so it is spelled like every other header — lowercase kebab-case with the `benzene-` marker. |
| `benzene-mcp-tool` | Binding: MCP | inbound (MCP) | The name of the MCP tool the call came through, set by the MCP binding ([mcp.md](mcp.md)). |
| `benzene-mcp-session` | Binding: MCP | inbound (MCP) | The MCP session id, where the transport has one. |
| `benzene-claim-check` | Module: claim check | both | Carries an opaque reference to a payload that the optional claim-check middleware offloaded to an external store because it exceeded a configured size threshold — see [claim-check.md](claim-check.md). |

`benzene-status` is a gRPC trailer, specified with the gRPC mapping it serves in [§4.2](#42-grpc).

**Naming.** Names Benzene invents carry the `benzene-` marker, because a header sits in a namespace
shared with the application and the transport. Names Benzene *borrows* — `content-type`,
`traceparent`, `tracestate`, `x-correlation-id` — are never renamed: interoperating with the
standard is the entire reason for using them. (The full rule is in
`work/archive/benzene-naming-principle.md`.)

`topic` is the deliberate exception: it keeps the envelope field's spelling so that one concept has
one name wherever it travels. The collision the marker guards against — an application that already
puts its own `topic` attribute on a message — is handled instead by making the name configurable.

**Reserved names are defaults.** Every name in this table is a *default*, not a literal an
implementation may hard-code: each is configurable in one place, and the setting applies in both
directions. Two consequences follow, and both are normative:

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
to its generic-error row **unless the result's `isSuccessful` (§1.2) is true**, in which case an
application-defined status maps to the protocol's generic-success row instead — a custom status
does not have to look like a framework failure just because the protocol's status-code space can't
express it distinctly. Note this only applies to *unknown* statuses: a status that collides with a
known failure string's spelling is still classified as that failure regardless of `isSuccessful`.

### 3.1 Problem-type registry

A framework-produced problem document's `type` is `https://benzene.app/problems/` followed by its
`benzeneStatus` — an opaque identifier compared by string equality, never dereferenced (§1.3). An
application-defined failure status has no framework `type`: the application supplies its own URI, or
omits it.

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
| `unexpected-error`, missing | 500 |
| unknown, `isSuccessful: true` | 200 |
| unknown, `isSuccessful: false` (or missing `isSuccessful`) | 500 |
| `not-implemented` | 501 |
| `service-unavailable` | 503 |
| `timeout` | 504 |

This table maps a result onto a native HTTP response — a route the binding maps to a topic. The
envelope endpoint does not use it: it answers 200 for every envelope it processed, because the
envelope in the body carries the status (transport-bindings §1).

Reverse (HTTP → Benzene, used by HTTP clients): 200→`ok`, 201→`created`, 202→`accepted`,
204→`deleted`, 400→`bad-request`, 401→`unauthorized`, 403→`forbidden`, 404→`not-found`,
408→`timeout`, 409→`conflict`, 422→`validation-error`, 429→`too-many-requests`,
501→`not-implemented`, 502→`service-unavailable`, 503→`service-unavailable`, 504→`timeout`,
anything else→`unexpected-error`.

**Problem details on failure.** When the negotiated response format is JSON, the HTTP response for
an unsuccessful result has a `content-type` that MUST be `application/problem+json` (charset as for any other JSON
response), and the body's `status` member (§1.3) MUST be present and equal the HTTP response code
in this table. When another format was negotiated, the problem document is serialized in that
format (e.g. `application/problem+xml` for XML, per RFC 9457 §11.2; other negotiated formats keep
their own content type — informative, not enumerated here). Clients MUST accept both
`application/json` and `application/problem+json` as failure-body content types.

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
| `unexpected-error`, missing | `Internal` |
| unknown, `isSuccessful: true` | `OK` |
| unknown, `isSuccessful: false` (or missing `isSuccessful`) | `Internal` |

**The `benzene-status` trailer**: because several Benzene statuses collapse to one gRPC code, a
Benzene gRPC server MUST attach a response trailer `benzene-status` carrying the raw status string
verbatim, on success and failure alike. A missing result maps the trailer value to `Unknown`.
Non-`OK` outcomes are surfaced as a gRPC error with the mapped code and a detail string of the
joined `errors` (or the raw status if `errors` is empty). There is no JSON problem document over
gRPC; the problem's information (§1.3) maps onto gRPC's own error model instead — the
`benzene-status` trailer already carries `benzeneStatus`, and structured `errors` map onto
`google.rpc.BadRequest` in the `grpc-status-details-bin` trailer, one `FieldViolation` per error.
`FieldViolation.field` is a dotted path, so the binding converts the §1.3 pointer: its segments,
unescaped per RFC 6901, joined with `.`, and a segment made only of decimal digits written `[n]`
(`/items/0/sku` → `items[0].sku`). An error with no `field` has an empty `FieldViolation.field`.

Reverse (client): a `benzene-status` trailer, when present, wins verbatim. Otherwise: `OK`→`ok`,
`InvalidArgument`→`bad-request`, `Unauthenticated`→`unauthorized`, `PermissionDenied`→`forbidden`,
`NotFound`→`not-found`, `AlreadyExists`→`conflict`, `Unimplemented`→`not-implemented`,
`Unavailable`/`Cancelled`→`service-unavailable`, `ResourceExhausted`→`too-many-requests`,
`DeadlineExceeded`→`timeout`, anything else→`unexpected-error`.

**Cancellation**: a cancelled invocation maps to gRPC `DeadlineExceeded` if the call's deadline
has passed, else `Cancelled`.

## 5. Health check response

Returned for the reserved health topics (§7) and any app-configured alias. The result's status is
`ok` whenever the checks ran — an unhealthy service is still a successful health query — and
`isHealthy` in the payload carries the verdict. A transport that has a status of its own maps the
verdict onto it where that is the convention: the HTTP binding answers `GET /benzene/health` with
**503** when `isHealthy` is false (transport-bindings §1), and gRPC uses `grpc.health.v1` (below).

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
- `healthChecks` keys are check names, and are unique.
- `data` is a free-form diagnostic bag; its keys are written verbatim (no naming policy applied).

gRPC hosts additionally expose the same aggregate over standard
[grpc.health.v1](https://github.com/grpc/grpc/blob/master/doc/health-checking.md): `SERVING` iff
no check failed (a warning maps to a degraded-but-serving state).

## 6. Serialization defaults

- Default payload encoding is JSON, UTF-8.
- Writers MUST write camelCase property names, and readers MUST accept them; a reader MAY also match
  property names case-insensitively. Writers MAY omit null-valued properties or emit them as `null`;
  readers MUST accept both.
- gRPC payload bridging uses **protobuf's own JSON mapping**
  ([proto3 JSON](https://protobuf.dev/programming-guides/proto3/#json)) between protobuf messages
  and plain types — not a naive reflection round-trip — so enums, well-known types, and oneofs
  convert per protobuf rules. Property matching is against the protobuf JSON names.

## 7. Reserved topics

The `benzene:` prefix is reserved to the framework, and a port MAY reject an application topic under
it. The topics below are the **interoperable** reserved topics — the ones a tool may rely on across
ports. A port MAY serve other topics under the prefix; a tool MUST NOT depend on one this section
does not list.

A reserved topic is **intercepted by its topic id alone, ignoring version**: a message to it never
reaches version selection (core-concepts §2), and any other topic passes through unchanged. Every
reserved topic is optional unless a profile requires it (the
[Cloud Service Profile](cloud-service-profile.md)'s R3, R5 and R6).

| Topic | Request | Success response |
|---|---|---|
| `benzene:healthcheck` | none | `ok`, the health response (§5): every registered check |
| `benzene:liveness` | none | `ok`, the health response, from checks that never depend on anything outside the process |
| `benzene:readiness` | none | `ok`, the health response, from the checks that decide whether this instance should receive traffic |
| `benzene:spec` | optional `{ "type"?: string, "format"?: string }` | `ok`, the contract document ([contract-document.md](contract-document.md) §7), or the format its request names |
| `benzene:ping` | none | `ok`, no payload — its body is unspecified, and a caller reads only the status |
| `benzene:mesh:register`, `benzene:mesh:heartbeat`, `benzene:mesh:traces`, `benzene:mesh:issues` | per [mesh.md](mesh.md) §4 | per [mesh.md](mesh.md) §4 — served by a collector, not by every service |
| `benzene:mesh:query:fleet`, `benzene:mesh:query:service`, `benzene:mesh:query:topic` *(Provisional)* | per collector | the collector's read models ([mesh.md](mesh.md) §4) — served by a collector; the shapes are not pinned |
| `benzene:mesh:dispatch` *(Provisional)* | informative: `{ "service": string, "topic": string, "headers"?: object, "body"?: string }` | the target service's result for that envelope — served by a collector that offers live dispatch; neither shape is pinned while the topic is Provisional |

- **`benzene:spec`.** Its request, defaults and the `not-implemented` rule for an unsupported
  `type` or `format` are [contract-document.md](contract-document.md) §7.
- **`benzene:ping`** is a reachability probe: health checks send it into a queue or a function to
  prove the path to a service works. A service that consumes a one-way transport SHOULD serve it,
  because an unanswered probe is a `not-found` failure that the transport redelivers until it
  dead-letters it (transport-bindings §1, obligation 8).
- The health topics answer `ok` whenever the checks ran; `isHealthy` carries the verdict (§5).
- The HTTP routes these topics are reachable on by default are the HTTP binding's default
  mappings (transport-bindings §1), not part of the topics' definition.
