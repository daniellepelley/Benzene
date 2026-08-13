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
  "isSuccessful": true,
  "headers": { },
  "body": "{ …serialized response… }"
}
```

| Field | Type | Rules |
|---|---|---|
| `statusCode` | string | A status vocabulary value (§3) — the *Benzene* status, not an HTTP code. Clients MAY additionally tolerate numeric HTTP codes here for interop with older or HTTP-shaped services, but MUST NOT write them. |
| `isSuccessful` | boolean | Required. The authoritative success/failure signal. For a status in §3's vocabulary this MUST match that status's Success? column. **A receiver MUST prefer this field over any classification it derives from `statusCode` text** — necessary for an application-defined status (§3), which is outside the sender's and receiver's shared vocabulary and therefore means nothing to a receiver classifying by string alone. A receiver reading an envelope from a sender that predates this field (absent `isSuccessful`) MAY fall back to classifying `statusCode` against §3, accepting that an application-defined status from such a sender classifies as failure (there is no other signal to trust it with). |
| `headers` | object (string→string) | Response headers, including `content-type` when set. |
| `body` | string | Pre-serialized response payload: on success, the handler's response payload; on failure, the error payload (§1.3). |

### 1.3 Problem details payload

When a result is unsuccessful, the response `body` **is** the serialized problem document —
**replace, not wrap**: the failure payload takes the place a success payload would otherwise
occupy, exactly as today (a `DefaultResponsePayloadMapper` never emits both). The one existing
carve-out is unchanged: a result marked `isSuccessful: true` (the `Set<T>(status, payload,
isSuccessful)` escape hatch some health-check-shaped results use) still renders its payload, never
a problem document — the branch is on `isSuccessful` (§1.2), not on status class.

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
    { "message": "Name must not be empty",     "field": "Name", "code": "NotEmptyValidator" },
    { "message": "Age must be greater than 0", "field": "Age",  "code": "GreaterThanValidator" }
  ]
}
```

| Field | Type | Rules |
|---|---|---|
| `type` | string (URI ref) | Framework-produced failures MUST use the registry URI for the status (§3.1). Application-authored problems SHOULD use their own absolute URI; absent or `about:blank` is tolerated on read. Readers treat it as an **opaque identifier** — comparison is string equality, never dereference (the registry URIs are not live pages). |
| `title` | string | Short human summary of the *type*, fixed per type (the registry's value for framework types). Never asserted by conformance fixtures — wording is free. |
| `status` | integer | **HTTP bindings only.** MUST equal the actual HTTP response status code (§4.1). MUST be **omitted** — not emitted as `null` — where no HTTP response exists (the envelope over a non-HTTP transport, a queue reply). Benzene clients MUST NOT classify a result from this member; classification is envelope-first (§1.2). |
| `detail` | string | Human-readable occurrence detail — the result's error messages joined with `", "`, unchanged from every prior version of this document. The compatibility member: every existing reader can keep using only this one. |
| `instance` | string (URI ref) | Optional, application-owned. The framework never fabricates it. |
| `benzeneStatus` | string | **Required.** The §3 status string, mirroring the envelope's `statusCode` (§1.2). The transport-neutral discriminator — present on every transport regardless of whether `status` is. It carries the `benzene` marker because this member namespace is shared with RFC 9457 itself and with applications (the naming rule of §2's "Naming" paragraph); `errors`, below, is Benzene's own extension and stays unmarked. |
| `errors` | array | Optional. When present, **authoritative and ordered** — supersedes the "recover `errors` from `detail`" rule this document carried previously (withdrawn, see below). Each item: `message` (string, required), `field` (string, optional — the producer's property path; JSON Pointer for schema-based validators, the host language's property path for others — document which per integration), `code` (string, optional — a machine-readable, producer-owned rule identifier, emitted verbatim, never normalized or reworded by the framework). |
| *(extensions)* | any | Applications MAY add further members (RFC 9457 §3.2). Readers MUST ignore unknown members. **Neither `code` nor `type` participates in the mesh issue fingerprint** — see §3.1. |

**Unknown-member tolerance.** A reader MUST ignore any problem-document member it does not
recognize, framework-defined or application-added alike — this is what lets applications extend
the document (RFC 9457 §3.2) without breaking older readers, and what lets a future framework
member arrive without a version bump.

**Signalling.** The envelope (§1.2) is the failure signal, not the body's content type: a
non-`ok`-class `statusCode` / `isSuccessful: false` is what tells a receiver this body is a
problem document. The envelope's inner `headers.content-type` SHOULD be `application/problem+json`
when the response has one; readers MUST NOT require it — the outer transport content-type (e.g.
the HTTP body carrying the envelope itself) is a separate concern and stays whatever it already is
(§4.1 covers the case where the transport response itself *is* the problem document, i.e. HTTP).

**This document previously described two rules this profile withdraws, not softens:**

1. The body member was named `status` and typed as a string carrying the Benzene status — a name
   collision with RFC 9457's own `status`, which is defined as the integer HTTP response code. That
   collision is resolved by rename, not by removing the RFC alignment: the Benzene status now
   travels as `benzeneStatus`, and `status`, when present at all, is genuinely the integer HTTP
   code RFC 9457 defines.
2. "Clients recover `errors` from `detail`" was never implementable (splitting human prose on `,
   ` is unsafe — messages contain commas) and no reader ever attempted it. It is replaced by the
   rule above: `errors`, when present, is authoritative and ordered; a reader without an `errors`
   member treats `detail` as a single opaque message, and a missing/empty `detail` yields an
   error-free failed result.

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
| `benzene-claim-check` | **C** | both | Carries an opaque reference to a payload that the optional claim-check middleware offloaded to an external store because it exceeded a configured size threshold — see [§2.1](#21-claim-check-add-on). |

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

### 2.1 Claim check (add-on)

`benzene-claim-check` is written by an **optional** outbound middleware when a payload exceeds a
configured size threshold, letting a large payload bypass a transport's message-size limit.

- The header's value is an **opaque, URI-form reference** of the shape `scheme://location/key`,
  issued by the sender's payload store (e.g. `s3://bucket/key`, `azblob://container/key`).
- The message **body** of an offloaded message is **unspecified** — a consumer MUST NOT interpret
  it directly, and MUST treat the header as authoritative.
- A consumer with the claim-check add-on wired MUST replace the body with the stored content
  verbatim **before** deserialization; every other header (including `content-type`) applies to
  the **hydrated** body, not the placeholder.
- A consumer MUST resolve a claim-check reference only through its own configured store, and MUST
  fail the message loud — never silently skip it — when the reference cannot be resolved or lies
  outside that store's own configuration. This is a security boundary: a consumer MUST NOT fetch
  an attacker-supplied arbitrary location.
- Deleting the stored payload at read time is **forbidden**. A fan-out transport (e.g. pub/sub) may
  have multiple consumers reading the same offloaded message, and at-least-once redelivery would
  find the blob already gone if the first reader deleted it. Retention is store-side expiry (e.g. a
  lifecycle rule) agreed between the communicating services — not specified further here; that is
  deployment-specific.

Porting implication: Tier C means each language port adopts this add-on on its own schedule; a
service that offloads is only interoperable with consumers that have wired the add-on and share
access to the same store — an explicit deployment agreement, exactly like any other Tier C
middleware.

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

One row per **failure** status in §3 — the registry is *keyed by the status vocabulary above*, so
this introduces no second taxonomy. Success statuses have no row: problem documents (§1.3) exist
only on failure. Every URI lives under `https://benzene.app/problems/`; these are **opaque
identifiers**, not live pages — see §1.3's rule that readers compare by string equality, never
dereference.

| `benzeneStatus` | `type` (`https://benzene.app/problems/` +) | `title` | HTTP `status` |
|---|---|---|---|
| `bad-request` | `bad-request` | Bad request | 400 |
| `unauthorized` | `unauthorized` | Unauthorized | 401 |
| `forbidden` | `forbidden` | Forbidden | 403 |
| `not-found` | `not-found` | Not found | 404 |
| `conflict` | `conflict` | Conflict | 409 |
| `validation-error` | `validation-error` | Validation failed | 422 |
| `too-many-requests` | `too-many-requests` | Too many requests | 429 |
| `unexpected-error` | `unexpected-error` | Unexpected error | 500 |
| `not-implemented` | `not-implemented` | Not implemented | 501 |
| `service-unavailable` | `service-unavailable` | Service unavailable | 503 |
| `timeout` | `timeout` | Timeout | 504 |

**Application-defined failure statuses** (§3): `type` is the application's own URI, or omitted;
`benzeneStatus` carries the application's status string verbatim; the HTTP `status` value falls to
the §4.1 unknown-status row (500) exactly as an application-defined status does everywhere else in
this document.

**Relationship to mesh issue `classification` (informative; no new mechanism).** The operator-side
roll-up of the same failure is the mesh issue `classification`
(`exception`/`validation`/`config-wiring`/`dependency`/`contract-drift`/`unclassified`,
[mesh.md §4.1](mesh.md#41-issues-benzenemeshissues)), derived from the Benzene status and the
captured exception type by that section's precedence rules — a different, already-implemented
mechanism, not this registry. Problem `type` is the **caller-facing** identity of a failure (open
vocabulary, one per response); `classification` is the **operator-facing** identity of the same
failure (closed vocabulary, one per invocation, fingerprint-stable). Both are derived from the one
status vocabulary above; the registry deliberately introduces no third vocabulary between them.
**Neither `code` (§1.3) nor problem `type` may enter the mesh issue fingerprint** — the fingerprint
is `service|topic|version|classification|discriminator` (mesh.md §4.1), and both `code` and `type`
are open, per-error or per-response identifiers that would explode issue cardinality and defeat
fingerprint-based merge.

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

Reverse (HTTP → Benzene, used by HTTP clients): 200→`ok`, 201→`created`, 202→`accepted`,
204→`deleted`, 400→`bad-request`, 401→`unauthorized`, 403→`forbidden`, 404→`not-found`,
408→`timeout`, 409→`conflict`, 422→`validation-error`, 429→`too-many-requests`,
501→`not-implemented`, 502→`service-unavailable`, 503→`service-unavailable`, 504→`timeout`,
anything else→`unexpected-error`.

**Problem details on failure.** When the negotiated response format is JSON, an HTTP failure
response's `content-type` MUST be `application/problem+json` (charset as for any other JSON
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
