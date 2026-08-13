# Contract Document

**Status: DRAFT v0.1 — promoted from the .NET-only `Benzene.Schema.OpenApi.EventService.EventServiceDocument`
(`benzene-dotnet`). This document supersedes that artifact's `.NET-side, not promoted` status
(`spec-mesh-tooling-implementation-plan.md` Amendment C, 2026-08-12): cross-language client
generation needs one parseable format, so the format, its generation semantics, and its hash
algorithm are now spec, pinned by `conformance/contract-document-cases.json` and
`conformance/contract-hash-cases.json`. The .NET port is this document's reference implementation.**

The Contract Document — conventionally emitted as `{Service}.spec.json` — is what a service derives
from its handler registry to describe every topic it serves: request/response shapes, payload
schemas, and (optionally) how the topics are reachable over HTTP. It is the single input every
language's client generator parses to produce a typed, topic-scoped client — the same file drives
the .NET `benzene build` CLI today and will drive the TypeScript, Python, and Go generators.

This is also the document the [Cloud Service Profile](cloud-service-profile.md) **R5** requires a
conformant service to derive and serve at `/benzene/spec`
(`?type=benzene&format=json`) — R5 names *that* a spec document must exist and be served; this
document is the spec document's shape.

## 1. Top-level shape

```json
{
  "openapi": "3.0.1",
  "info": { "title": "payments-api", "description": "", "version": "" },
  "messageEndpoint": "/benzene/invoke",
  "transports": ["api-gateway", "benzene", "sqs", "sns", "eventbridge"],
  "requests": [ /* RequestResponse, §2 */ ],
  "events": [ /* Event, §3 */ ],
  "components": { "schemas": { /* OpenAPI 3.0 Schema Objects, §4 */ } }
}
```

| Field | Presence | Meaning |
|---|---|---|
| `openapi` | REQUIRED, always the literal `"3.0.1"` | A heritage marker only — this document is not full OpenAPI (no `paths`; requests/events carry the topic-addressed shapes below). Consumers MUST NOT reject a document on this value; it identifies the schema-object dialect (OpenAPI 3.0), nothing more. |
| `info` | REQUIRED | `title` (the service name) and `version`; a producer that has neither writes empty strings, not a missing object. |
| `messageEndpoint` | OPTIONAL | The path of the service's Benzene-message-over-HTTP endpoint (wire-contracts.md §1). Absent when the service exposes no such endpoint — consumers feature-detect send capability on its presence, never assume a default path. |
| `transports` | OPTIONAL, omitted when empty (never an empty array) | Every transport this host is wired to *receive* messages over (`"sqs"`, `"kafka"`, `"http"`, …). Document-level, not per-topic: any wired non-HTTP transport can reach any registered handler by topic, so a per-topic list would just repeat this array. HTTP is the one exception — a topic's actual HTTP reachability is its own `httpMappings` (§2), unaffected by this field. |
| `requests[]` | REQUIRED, always present (possibly empty) | One entry per request/response topic. §2. |
| `events[]` | REQUIRED, always present (possibly empty) | One entry per produced-event topic. §3. |
| `components` | REQUIRED | `{ "schemas": { <name>: <Schema Object> } }` — the document's schema catalogue. `$ref` values anywhere in the document point only into `#/components/schemas/<name>`; no external or relative refs. §4. |

*(Non-normative note: the .NET producer's in-memory model also carries an OpenAPI `tags` array,
serialized only when non-empty. It is unused OpenAPI heritage — no producer today populates it and
no consumer needs to — and is deliberately not pinned here. A consumer MUST tolerate an unknown
top-level `tags` field if present, per the general "ignore what you don't recognize" posture this
document expects of every consumer for forward compatibility.)*

## 2. `requests[]` entries

One entry per request/response ("RPC-shaped") topic:

```json
{
  "topic": "payments:capture",
  "version": "v2",
  "reserved": true,
  "httpMappings": [ { "method": "POST", "path": "/payments" } ],
  "request": { "$ref": "#/components/schemas/CapturePayment" },
  "response": { "$ref": "#/components/schemas/PaymentDto" },
  "example": { "orderId": "value", "amount": 42.42, "currency": "value" }
}
```

| Field | Presence | Meaning |
|---|---|---|
| `topic` | REQUIRED | The topic id (core-concepts.md §2). |
| `version` | OPTIONAL | The topic's **handler version** (core-concepts.md §2) — distinct from a payload schema version (versioning.md). **Absent and empty are not the same thing**: absence means the unversioned handler; a producer MUST NOT write `"version": ""` for it — the field is omitted entirely. A consumer that finds `version` absent MUST treat the entry as unversioned, not as `version: ""`. |
| `reserved` | OPTIONAL, written only when `true` | Marks a reserved Benzene utility topic (`benzene:spec`, `benzene:healthcheck`, `benzene:mesh`, …) rather than a service's domain topic. Never written as `false` — its **absence also means not-reserved**. See §5.1 for the full reserved-detection rule a consumer must apply, which does not stop at this flag. |
| `httpMappings[]` | OPTIONAL, omitted when empty | Zero or more `{ "method": "<HTTP verb>", "path": "<route>" }` pairs — the topic's explicit HTTP exposure, if any. A topic with no HTTP exposure omits this array; it is not reachable over HTTP at all (independent of the document-level `transports`, §1). |
| `request` | REQUIRED | An OpenAPI 3.0 Schema Object (inline or `$ref`) for the request payload. |
| `response` | REQUIRED | Likewise, for the response payload. |
| `example` | OPTIONAL | An example request payload, generated from `request`'s schema unless the producer supplied one. Informative decoration only — see §6 (contract hash), which strips it. |

## 3. `events[]` entries

One entry per topic the service produces as a fire-and-forget event (no response):

```json
{
  "topic": "payment:captured",
  "version": "v1",
  "message": { "$ref": "#/components/schemas/OutboundPaymentCaptured" },
  "example": { "orderId": "value", "paymentId": "value", "amount": 42.42, "currency": "value" }
}
```

| Field | Presence | Meaning |
|---|---|---|
| `topic` | REQUIRED | The event's topic id. |
| `version` | OPTIONAL | Same absent-means-unversioned rule as `requests[].version`. |
| `message` | REQUIRED | An OpenAPI 3.0 Schema Object (inline or `$ref`) for the event payload. |
| `example` | OPTIONAL | Same role as `requests[].example`. |

There is no `reserved` field on an event entry: no reserved Benzene utility topic is currently
produced as an event (they are all request/response, §2). §5.1's reserved-detection rule (flag OR
`benzene:` prefix) is written generically and a future reserved *event* topic would still be
detected by the prefix half of that rule even without a flag to carry — but as of this version, no
producer emits one, so this field is simply absent from the shape.

## 4. `components`

`components.schemas` is an object mapping a schema name to an **OpenAPI 3.0 Schema Object**. Every
`$ref` anywhere in the document (in `requests[].request`/`.response`, `events[].message`, or nested
inside another schema) is a JSON Pointer of the exact form `#/components/schemas/<name>` — no
external files, no relative refs, no refs into any other part of the document. Schema declaration
order inside `components.schemas` is producer-defined and carries no meaning (§6.3 covers the one
place order *is* normative: canonical JSON for hashing sorts object keys regardless of source
order).

## 5. Generation semantics

This section defines what a conforming client generator does with a Contract Document — the rules
four independent generators (today: .NET; planned: TypeScript, Python, Go) must implement
identically for "the same client" to mean the same thing in every language. Method naming and file
layout are **not** part of this — see §5.5.

### 5.1 Domain-only default and reserved-topic detection

A generator's default output covers a service's **domain** topics only — the reserved Benzene
utility topics (`benzene:spec`, `benzene:healthcheck`, `benzene:mesh`, …) are excluded unless
explicitly asked for (§5.2). This is a deliberate ruling, not an oversight: a generated client is
for a service's business surface, and emitting a reserved endpoint into it would force every
consumer to register an outbound route it never asked for, or fail a startup route-validation check
over framework plumbing it doesn't care about.

**Detection rule:** a `requests[]` entry is reserved when **either** of these hold:

1. its `reserved` field is `true`, **or**
2. its `topic` starts with the `benzene:` prefix.

Both conditions are checked — not only the flag — because a document from an older producer build
may carry a reserved topic with no `reserved` flag at all (the flag is an additive, not foundational,
signal). A generator MUST implement the OR of both, not just one.

### 5.2 Include-list and fail-loud unknown topics

A generator accepts an optional **topic include-list**: when given, only the named topics are in
scope, and this list overrides §5.1's default entirely — naming a reserved topic in the include-list
admits it, regardless of any separate "include reserved" setting. When the include-list is absent
(or empty), every domain topic is in scope by §5.1's rule (plus reserved topics too, if a separate
"include reserved" setting is on).

**Fail loud:** if the include-list names a topic the document does not have among its `requests[]`
entries, the generator MUST fail (non-zero exit / thrown error), not silently skip it, and the
error MUST list the topic(s) it couldn't find. Listing the document's actual topic ids in the error
is REQUIRED by the parity checklist; the exact message wording is not — the fixture asserts only
the unknown-topic set and the valid-topic set.

The include-list scopes `requests[]` only. It has no effect on `events[]`, `components`, or any of
the document-level fields (`info`, `messageEndpoint`, `transports`) — those pass through unchanged
in a projected document, except where §5.3's schema-closure projection additionally narrows
`components` and drops `events[]` for a **topic-scoped** (single-topic) client shape.

### 5.3 Topic-scoped schema closure

A **topic-scoped client** — a self-contained client generated for exactly one topic — includes only
the component schemas that topic's request and response actually reach, not the whole document's
catalogue. This closure walk MUST be implemented identically across languages; it is pinned by
`conformance/contract-document-cases.json`'s schema-closure cases.

Given a topic's `request` and `response` schema objects and the document's `components.schemas`
catalogue, the reachable set is computed by walking both schemas with the following rules, starting
from an empty "reached" set:

1. **`$ref`**: if the schema is a reference into `#/components/schemas/<name>`, and `<name>` is not
   already in the reached set, add `<name>` to the reached set and then walk `<name>`'s schema
   object from the catalogue (recursively, by these same rules). **Already-reached names are not
   walked again** — this is what makes the walk terminate on a reference cycle (two schemas that
   `$ref` each other, directly or transitively).
2. **`items`**: walk the schema's `items` (array element schema), if present.
3. **`additionalProperties`**: walk the schema's `additionalProperties`, if present **and it is a
   schema** (a boolean `additionalProperties` has nothing to walk).
4. **`properties`**: walk every value in the schema's `properties` map.
5. **`allOf` / `anyOf` / `oneOf`**: walk every member schema in each of these three arrays, if
   present.
6. Every walked schema (inline or resolved-from-`$ref`) is itself re-examined by rules 2–5 — the
   walk is fully recursive; only rule 1's cycle guard is needed for termination, since 2–5 always
   make structural progress into a schema that has already been reached via 1, or examine an inline
   schema which cannot itself cycle back through `$ref` without going through rule 1 again.

The final reachable set is every catalogue name added by rule 1 while walking the topic's `request`
and `response` in turn. The **topic-scoped projection** of the document for that topic is:

- `requests`: exactly the one entry for that topic.
- `events`: empty — a topic-scoped (per-topic) client shape carries no produced events; those
  belong to the service-level client shape, not a single request/response topic's self-contained
  client.
- `components.schemas`: exactly the reached set (as computed above), keyed the same as the source
  catalogue.
- `info`, `messageEndpoint`, `transports`: unchanged from the source document.

A **service-level client** generated for several topics via the include-list (§5.2) is a different,
coarser projection: it filters `requests[]` to the include-list but leaves `events[]` and
`components` **unnarrowed** — the whole document's event list and schema catalogue pass through.
Only the topic-scoped (single-topic, §5.3) shape narrows components and drops events. §6.4 explains
why these are two different, non-interchangeable hash inputs.

### 5.4 Transport-agnostic output

Generated code's only runtime dependency MUST be the consuming port's **transport-agnostic message
sender abstraction** (the concept behind .NET's `IBenzeneMessageSender`, wire-contracts.md's sender
side) plus its result type — never a transport-specific client (no direct SQS/HTTP/Kafka client
type in generated code). Transport binding is the consumer's own outbound-routing configuration,
wired separately; the generated client must work unmodified regardless of which transport the
consumer's outbound route resolves to at runtime.

### 5.5 Out of conformance scope

**Method naming and file layout are API shape, not contract**, and are explicitly not pinned by
this document or its fixtures — matching this repository's standing rule that API shape stays out
of conformance (`conformance/README.md`). A topic's derived method/function name
(`payments:capture` → `Capture`, `capturePayment`, or any other per-language casing/naming idiom),
the generated file/module layout, and the registration/DI idiom (§ the parity checklist's row 9 in
`cross-language-clients-plan.md`) are each language port's own design decision. What *is* pinned —
the shape of what gets generated (service-level vs. topic-scoped, §5.3), the topic scoping rules
(§5.1–§5.2), and the embedded contract hash (§6) — is pinned because those change what a consumer
can observe or rely on; naming and layout do not.

## 6. Contract hash

Every generated client embeds a **contract hash**: a value that changes if and only if the
contract it was generated from changes, so a consumer can detect drift between the client it holds
and the service it's calling (the health-check/drift-check plane compares this value, when that
feature is wired — see `cross-language-clients-plan.md`; the drift-check *feature* itself is out of
this document's scope, only the hash *value* it consumes is specified here).

### 6.1 Why a new algorithm

Today's .NET-only hash (`Benzene.CodeGen.Core.CodeGenHelpers.GenerateHash`) computes lowercase-hex
HMAC-SHA256, with an empty key, over the **Microsoft.OpenApi library's own JSON serializer output**
of a normalized document. That byte stream is a property of one specific .NET serializer — no other
language can reproduce it without embedding a clone of that serializer's exact formatting decisions
(property ordering, whitespace, escaping). It is not portable, and this document replaces it with
one that is — every port, including .NET, adopts the algorithm below (`cross-language-clients-plan.md`
Phase 2 migrates the .NET side; this document specifies the target algorithm only).

### 6.2 Algorithm

```
contractHash = "sha256:" + lowercase-hex(sha256(canonicalJSON(normalize(document))))
```

- **`normalize(document)`** strips, from the (possibly already topic-scoped, §5.3) document:
  - every `example` field, at any `requests[]` or `events[]` entry;
  - the top-level `messageEndpoint` field, if present;
  - the top-level `transports` field, if present;
  - the `reserved` flag itself from every surviving `requests[]` entry that carries `reserved:
    true` — a *classification* detail, stripped unconditionally (whether or not the entry survives
    the next step), since it is metadata about the entry, not part of the topic's contract;
  - **and, additionally, when hashing a whole-service (not topic-scoped) document**: every
    `requests[]` entry detected as reserved by §5.1's rule (flag OR `benzene:` prefix — the flag has
    already been stripped by the previous step by the time this rule is evaluated, so implementers
    MUST apply §5.1 detection *before* stripping the flag, or otherwise retain the pre-strip
    reserved-ness for this step), removed **entirely** (the entry itself, not just its flag) — the
    published whole-service contract hash covers a service's **domain projection**, consistent with
    §5.1's domain-only default. A topic-scoped document (§5.3) never contains a reserved entry to
    begin with unless one was explicitly asked for via the include-list — in that case it survives
    with only its `reserved` flag stripped (previous step), since asking for it explicitly makes it
    part of what's being hashed.
  - Nothing else is touched: `info`, the surviving `requests[]`/`events[]` entries' remaining
    fields (`topic`, `version`, `httpMappings`, `request`/`response`/`message` schemas), and
    `components` are hashed as they stand in the (projected) document.
- **`canonicalJSON`** is **RFC 8785 (JCS — JSON Canonicalization Scheme)**: object members sorted
  by their UTF-16 code unit sequence, numbers formatted per the ECMAScript number-to-string
  algorithm JCS mandates, no insignificant whitespace, and consistent string escaping. JCS has
  off-the-shelf, spec-conformant implementations in all four ports' ecosystems today — .NET, the
  `canonicalize` package on npm, `rfc8785` on PyPI, `github.com/gowebpki/jcs` for Go — so no port
  needs to hand-write a canonicalizer to be conformant.
- **`sha256`** is the standard SHA-256 digest of the UTF-8 bytes of the canonical JSON string,
  rendered as lowercase hex, prefixed `sha256:` (matching `descriptorHash`'s prefix convention,
  mesh.md §2.2).

### 6.3 Why JCS here, and documented order for `descriptorHash`

This is a deliberate divergence from `descriptorHash` (mesh.md §2.2), which uses a **documented
member order** (declaration order for the fixed descriptor shape, lexicographic for its schema
maps) rather than JCS. The difference is not an inconsistency — it follows directly from what each
hash is *for*, and mesh.md §2.2 says so explicitly: `descriptorHash` "is per-port by design... and
is never compared across ports" — it exists to detect *one service's* redeploys, and a documented
order is sufficient because only that one port's implementation of the documented order ever needs
to agree with itself.

`contractHash` has the opposite job: it is compared **across ports** — a TypeScript-generated
client's embedded hash is meaningful precisely because it can be compared against a Go service's
served contract hash for the same projection. That comparison is only trustworthy if canonicalization
is **mechanical** (no documented-but-hand-implementable ordering rule that four independent
implementations could each get subtly wrong in the corners — nested map ordering, Unicode escaping,
number formatting) — which is exactly what JCS, an actual IETF RFC with conformance-tested
libraries, provides. `components.schemas`, in particular, is producer-defined arbitrary JSON where
"declaration order" was never meaningful to begin with; JCS's key-sort makes that moot instead of
requiring a documented tiebreak.

### 6.4 Projection comparability

A contract hash is a pure function of **whatever document (or document projection) it is computed
over**. This has one important consequence a consumer of this document must not miss: **a hash is
only meaningful compared against an identically-scoped projection.**

- The whole-service (domain-projected, §6.2) hash of a service is comparable only to another
  whole-service domain-projected hash of the (possibly different-build) same service.
- A topic-scoped client's embedded hash (§5.3's projection, then §6.2's `normalize`) is comparable
  only to the *same topic's* topic-scoped hash served or recomputed by the producing service — never
  to that service's whole-service hash, and never to a different topic's topic-scoped hash.
- A service-level client generated over an include-list (§5.2, unnarrowed `events`/`components`) is
  yet a third projection, with its own hash, comparable only to the identical include-list's
  projection.

A drift check (or any comparison) that hashes two different projections and compares the results is
not detecting drift — it is comparing two different, deliberately-unequal numbers and will produce
false results in both directions. Implementers wiring a drift-check feature on top of this hash
MUST ensure both sides compute the hash over the same projection rule.

## 7. Conformance

Two fixture files pin this document:

- **`conformance/contract-document-cases.json`** — document parse/validate cases (including the
  reserved-detection rule of §5.1 and the fail-loud unknown-topic rule of §5.2), topic-scope
  projection cases (§5.2), and schema-closure cases (§5.3), including a `$ref` cycle and an
  `allOf`/`oneOf` reach case.
- **`conformance/contract-hash-cases.json`** — exact expected `contractHash` values (§6.2) for a
  minimal document, a document proving `example`/`messageEndpoint`/`transports` normalization, a
  document proving the reserved-entry domain-projection rule, and a topic-scoped projection.

See [conformance/README.md](conformance/README.md) for the full case formats and which conformance
claims require these fixtures (in short: required only for a port that ships a client generator —
a port that never generates clients is unaffected, the same conditional shape as the mesh
collector fixtures).
