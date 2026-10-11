# Contract Document

**Module — Stable** (self-description and client generation). Required by the Cloud Service
Profile's R5 as the document served at `/benzene/spec`, and by any port that ships a client
generator.

The Contract Document — conventionally emitted as `{service}.spec.json` — is what a service derives
from its registrations to describe itself: the topics it serves, the events it publishes, the
messages it sends to others, the payload schemas of all three, and (optionally) how its topics are
reachable over HTTP. It is the one input every language's client generator reads, and the document
the reserved `benzene:spec` topic answers with (§7).

## 1. Top-level shape

```json
{
  "schemaDialect": "openapi-3.0",
  "service": "orders",
  "serviceVersion": "1.4.2",
  "versionScheme": "semver",
  "description": "Order intake",
  "messageEndpoint": "/benzene/invoke",
  "transports": ["http", "sqs"],
  "contractHash": "sha256:…",
  "registrationHash": "sha256:…",
  "requests": [ /* §2 */ ],
  "events": [ /* §3.1 */ ],
  "sends": [ /* §3.2 */ ],
  "components": { "schemas": { /* §4 */ } }
}
```

| Field | Presence | Meaning |
|---|---|---|
| `schemaDialect` | REQUIRED, `"openapi-3.0"` | The dialect of every schema in the document (OpenAPI 3.0 Schema Objects). A consumer MUST reject a document whose dialect it does not know. The document is not itself an OpenAPI document. |
| `service` | REQUIRED | The service's name. |
| `serviceVersion` | OPTIONAL | Which release this is ([service-versions.md](service-versions.md)). |
| `versionScheme` | OPTIONAL; REQUIRED for an orderable `serviceVersion` | The comparison scheme of `serviceVersion` ([service-versions.md](service-versions.md) §2). |
| `description` | OPTIONAL | Free text. |
| `messageEndpoint` | OPTIONAL | The path of the service's envelope endpoint over HTTP (transport-bindings §1). Absent when there is none: a consumer feature-detects send capability on its presence and never assumes a default. |
| `transports` | OPTIONAL, omitted when empty | The transports this service receives messages over (`"http"`, `"sqs"`, `"kafka"`, …). Document-level: any wired non-HTTP transport reaches any registered topic. HTTP reachability is per topic, its `httpMappings` (§2). |
| `contractHash`, `registrationHash` | REQUIRED | The two hashes of §6, computed over this document. |
| `requests[]` | REQUIRED (possibly empty) | The topics this service serves. §2. |
| `events[]` | REQUIRED (possibly empty) | The events this service publishes. §3.1. |
| `sends[]` | REQUIRED (possibly empty) | The messages this service sends to topics others define. §3.2. |
| `components` | REQUIRED | `{ "schemas": { <name>: <Schema Object> } }`. Every `$ref` anywhere in the document points into `#/components/schemas/<name>`; there are no external or relative refs. §4. |

A consumer MUST ignore a top-level field it does not recognize. The register body that a service
sends to a mesh collector carries further, deployment-side fields ([mesh.md](mesh.md) §2).

**No reserved topics.** A producer MUST NOT list a `benzene:`-prefixed topic in `requests`, `events`
or `sends`; the reserved topics' shapes are defined once, in wire-contracts §7. A consumer MUST
ignore any such entry it meets.

## 2. `requests[]` entries

One entry per topic the service serves:

```json
{
  "topic": "payments:capture",
  "version": "v2",
  "httpMappings": [ { "method": "POST", "path": "/payments" } ],
  "request": { "$ref": "#/components/schemas/CapturePayment" },
  "response": { "$ref": "#/components/schemas/PaymentDto" },
  "example": { "orderId": "o-1", "amount": 42.42, "currency": "GBP" }
}
```

| Field | Presence | Meaning |
|---|---|---|
| `topic` | REQUIRED | The topic id (core-concepts §2). |
| `version` | OPTIONAL | The handler version (core-concepts §2). An unversioned entry omits `version`; a producer MUST NOT write `"version": ""`. |
| `httpMappings[]` | OPTIONAL, omitted when empty | `{ "method": "<HTTP verb>", "path": "<route>" }` pairs: the topic's HTTP routes. A topic without them is not reachable over HTTP. |
| `request` | REQUIRED | The request payload's schema. |
| `response` | OPTIONAL | The response payload's schema. **Absent means the handler returns no payload**; `{}` means it returns a payload whose shape is unconstrained. |
| `example` | OPTIONAL | An example request payload. Producers SHOULD emit only an author-supplied example, not one synthesized from the schema. |

## 3. Outbound: `events[]` and `sends[]`

Two lists describe what a service puts on the wire, distinguished by **who owns the contract**, not
by whether a reply comes back:

- an **event** is a topic this service publishes and defines. Anyone may subscribe, and its payload
  is this service's to change — it is part of what a client of this service depends on;
- a **send** is a message to a topic **another party** defines — a command to a queue, a request to
  another service, a publish onto someone else's topic. It is a dependency of this service, which a
  client of it cannot observe.

So for an `orders` service, `order:placed` is an event and `payment:take` (owned by payments) is a
send, though both are one-way.

### 3.1 `events[]` entries

| Field | Presence | Meaning |
|---|---|---|
| `topic` | REQUIRED | The event's topic id. |
| `version` | OPTIONAL | As `requests[].version`. |
| `message` | REQUIRED | The event payload's schema. |
| `example` | OPTIONAL | As `requests[].example`. |

### 3.2 `sends[]` entries

| Field | Presence | Meaning |
|---|---|---|
| `topic` | REQUIRED | The target topic id. |
| `version` | OPTIONAL | The version the sender writes into `benzene-version`. |
| `request` | REQUIRED | The schema of the message sent. |
| `response` | OPTIONAL | The schema of the reply. **Absent means one-way** (nothing comes back); `{}` means a reply of unconstrained shape. |

The field names mirror `requests[]`: a send is the client-side image of someone else's request.

### 3.3 Declaring them

- **Explicitly.** Both lists come from explicit declarations — (topic, version, types) records an
  application hands the framework, as it registers handlers (core-concepts §9, §12). A port MUST
  NOT infer them from call sites, string literals or any other analysis of code. The API is each
  port's; the spec names the two concepts *event declarations* and *send declarations*.
- **No addresses.** A declaration names a topic, never a queue URL, ARN or other destination — that
  is deployment configuration, and the document does not change between environments.
- **One list each.** A (topic, version) MUST NOT appear in both `events` and `sends`; declaring it
  so is a start-up error. A topic MAY also appear in `requests` (a service that forwards).
- **Undeclared sends.** Sending to an undeclared topic is not a violation, but a collector that
  observes it reports `contract-drift` ([mesh.md](mesh.md) §4.2).

## 4. Components and derivation

### 4.1 `components` and naming

`components.schemas` maps a name to an OpenAPI 3.0 Schema Object. Component names are not part of
the contract — the hashes (§6) dereference every `$ref`, so two ports that name or hoist differently
still agree — but they are what generated clients call their types, so they follow these rules:

1. **What a component is.** The schema of a *named* object or enum type. A producer SHOULD hoist
   every named object type reachable from an entry, and MAY inline any schema. Arrays, maps and
   primitives are never components.
2. **Default name.** The type's declared schema name if one is declared, else its simple name, with
   no namespace, module or package. A generic instance SHOULD put the type-argument names before
   the definition's (`Page<Order>` → `OrderPage`).
3. **Validity.** Names MUST match `^[A-Za-z0-9._-]+$`; other characters are dropped.
4. **Uniqueness.** Two schemas that differ after normalization (§6.2, steps 3–4) MUST NOT share a
   name: derivation fails, before any message is handled, naming both types, the topics that use
   them, and the remedy. Two identical schemas MAY share one entry. Suffixing a name or letting the
   first registration win is not permitted.
5. **Declared names.** A port MUST offer a way to declare a payload type's schema name — the remedy
   for rule 4. The mechanism is the port's idiom.
6. **Determinism.** The same registrations yield the same names on every build; registration order
   never influences a name.
7. **Recursion.** A recursive named type is hoisted and refers to itself by `$ref`.

### 4.2 Derivation

A schema describes the **marshaled JSON form** of a registered type, derived once from whatever
type information the port's registration captures. The mapping (the left column names the
language-neutral construct; each port applies it to its own type system):

| Construct | Schema |
|---|---|
| string; text-marshaling custom type; byte array (marshals base64) | `{"type":"string"}` |
| boolean | `{"type":"boolean"}` |
| integer kinds | `{"type":"integer"}` |
| floating kinds | `{"type":"number"}` |
| timestamp (marshals RFC 3339) | `{"type":"string","format":"date-time"}` |
| enum | the base type with its values (`{"type":"string","enum":[…]}`) where the port knows them, else the base type alone; `{}` only for a type that cannot be serialized |
| list or array of T | `{"type":"array","items":T}` |
| string-keyed map of T | `{"type":"object","additionalProperties":T}` |
| object or record | `{"type":"object","properties":{…},"required":[…]}`, with no `additionalProperties` |
| nullable primitive, array or map T | T's schema plus `"nullable": true` |
| nullable named object or enum type T | `{"type":<T's type>,"nullable":true,"allOf":[<T's $ref, or T's schema if inlined>]}`, whether or not T is hoisted |
| raw or unknown JSON, dynamic values, custom serializers, anything the marshaler cannot serialize | `{}` |

- Property names, and whether a property can be omitted, are exactly what the port's JSON marshaler
  does. A property the marshaler always emits is `required`, even when its value may be `null`; one
  it may omit is not.
- Inherited or embedded members are flattened the way the marshaler flattens them.
- A producer SHOULD emit a `format` it knows (`int64`, `double`, `uuid`) — generated clients need it
  to choose a type — and MAY omit it. `format` never reaches a hash (§6.2).
- Validation constraints (`minLength`, `pattern`, `minimum`, …) MAY be emitted and are part of the
  contract: they are hashed.
- A handler that returns no payload has no `response` (§2).

## 5. Generation semantics

What a conforming client generator does with a Contract Document — the rules generators in
different languages implement identically, so that "the same client" means the same thing in
every language. Method naming and file layout are not part of this (§5.5).

### 5.1 Reserved topics

A generator ignores any `benzene:`-prefixed entry (§1); a generated client covers a service's own
topics only.

### 5.2 Include-list and fail-loud unknown topics

A generator accepts an optional **topic include-list**: when given, only the named `requests[]`
topics are in scope; when absent or empty, every one is.

**Fail loud:** if the include-list names a topic the document does not have among its `requests[]`,
the generator MUST fail (non-zero exit or thrown error), not skip it, and the error MUST list both
the topics it could not find and the document's actual topic ids. The wording is not asserted.

The include-list scopes `requests[]` only. `events[]`, `sends[]`, `components` and the document-level
fields pass through unchanged, except `contractHash` and `registrationHash`, which are recomputed over
the projection (§6.4).

### 5.3 Topic-scoped schema closure

A **topic-scoped client** — a self-contained client for exactly one topic — includes only the
component schemas that topic's request and response reach. Given those two schemas and
`components.schemas`, the reachable set is computed by walking both, starting from an empty
"reached" set:

1. **`$ref`**: if the schema references `#/components/schemas/<name>` and `<name>` is not yet
   reached, add it and walk its schema by these same rules. Already-reached names are not walked
   again, which is what makes the walk terminate on a cycle.
2. **`items`**: walk it, if present.
3. **`additionalProperties`**: walk it, if present and a schema.
4. **`properties`**: walk every value.
5. **`allOf` / `anyOf` / `oneOf`**: walk every member.

The **topic-scoped projection** of the document is: `requests` with exactly that topic's entry;
`events` and `sends` empty; `components.schemas` exactly the reached set; `contractHash` and
`registrationHash` recomputed over the projection; every other field unchanged.

A **service-level client** over an include-list (§5.2) is a different projection: it filters
`requests[]` and leaves everything else — including `events`, `sends` and `components` — unnarrowed.
§6.4 explains why the two are not interchangeable hash inputs.

### 5.4 Transport-agnostic output

Generated code's only runtime dependency MUST be the consuming port's transport-agnostic sender
(core-concepts §12) and its result type — never a transport-specific client. Which transport carries
a call is the consumer's own configuration.

### 5.5 Out of conformance scope

Method naming, file and module layout, and the registration idiom are API shape, not contract, and
each port decides them. What is pinned — the topic scope (§5.1–§5.2), the shape of what is generated
(§5.3) and the embedded hash (§6) — is pinned because a consumer can observe or rely on it.

## 6. Hashes

### 6.1 Two hashes

A document carries two hashes, each a pure function of a **projection** of the document:

- **`contractHash`** covers `requests` and `events`: what a client of the service depends on. A
  generated client embeds it, so that a drift check can compare the client's value with the served
  one.
- **`registrationHash`** covers `requests`, `events` and `sends`: what a mesh collector's graph
  depends on.

Both are REQUIRED in every document a producer emits. Any tool MAY recompute them.

### 6.2 Algorithm

```
hash(P) = "sha256:" + lowercase-hex(sha256(JCS(P)))
contractHash     = hash({ "requests": …, "events": … })
registrationHash = hash({ "requests": …, "events": …, "sends": … })
```

The projection P is built from the document by:

1. **Members.** Take `requests` and `events` (for `registrationHash`, `sends` too, always present,
   empty or not) and nothing else. Every other top-level field — identity, the hashes themselves,
   `components`, `messageEndpoint`, `transports`, and any future field — is outside P.
2. **Entries.** Drop any entry whose topic starts with `benzene:`. Keep only these members:
   `requests {topic, version?, httpMappings?, request, response?}`, `events {topic, version?,
   message}`, `sends {topic, version?, request, response?}`.
3. **Dereference.** At every schema position (step 4), an object with a `$ref` is replaced by the
   referenced component's schema, normalized by steps 3–4, recursively; the object's other members
   are ignored, as OpenAPI 3.0 ignores a Reference Object's siblings. A reference to a component
   already being expanded on the current path is **cut**: it becomes `{}`.
4. **Annotations.** The schema positions are exactly: an entry's `request`, `response` and `message`;
   and, within a schema at a position, each `properties` value, `items`, `additionalProperties` (when
   an object), `not`, and each member of `allOf`, `anyOf` and `oneOf`. Any other keyword's value is
   kept as written. At every position, remove the keywords `title`, `description`, `default`,
   `examples`, `example`, `deprecated`, `readOnly`, `writeOnly`, `$comment`, `$id`, `$schema` and
   `format`, every keyword starting `x-`, and `discriminator.mapping` (it holds component names).
   Sort every `required` array, and remove it if empty; remove an empty `properties` object. Every
   other keyword stays — validation constraints and unknown keywords are contract. Then, a schema
   whose members are exactly `nullable: true`, optionally `type`, and an `allOf` of one member that
   was a cut reference — the §4.2 nullable wrapper around a cycle — becomes `{}`.
5. **Order.** Sort entries by topic, then version (an absent version first). In each entry,
   upper-case every `httpMappings[].method` and sort the mappings by (method, path).

Every sort in steps 4–5 compares strings by UTF-16 code units, as JCS sorts member names.

`JCS` is **RFC 8785** (the JSON Canonicalization Scheme): members sorted by UTF-16 code units, the
ECMAScript number format, no insignificant whitespace. `sha256` is taken over the UTF-8 bytes of
the canonical string.

Because P dereferences, component names, hoisting depth, the order of entries and of `required`,
and schema documentation do not affect either hash: two ports deriving the same wire shapes per §4.2
produce the same two values.

### 6.3 Why JCS

The hashes are compared **across ports** — a TypeScript client's embedded `contractHash` against a
Go service's served one. That comparison is only trustworthy if canonicalization is mechanical,
leaving no corner (nested ordering, Unicode escaping, number formatting) for four implementations to
disagree on. RFC 8785 is that mechanism, with conformance-tested implementations available in every
port's ecosystem; a port MAY implement it itself, and `conformance/contract-hash-cases.json` is the
proof either way.

### 6.4 Projection comparability

A hash is only meaningful compared with a hash of the **identically scoped projection**:

| Projection | Members | Comparable only with |
|---|---|---|
| whole service | every `requests` entry, every `events` entry | the same service's `contractHash` |
| registration | as above, plus every `sends` entry | the same service's `registrationHash`, within one `(service, serviceVersion)` |
| include-list client (§5.2) | the listed `requests`, every `events` entry | the same include-list recomputed over the provider's current document |
| topic-scoped client (§5.3) | one `requests` entry, no `events` | the same topic recomputed over the provider's current document |

A default client — no include-list — embeds the whole-service `contractHash`, so it compares
directly with the served value. A drift check MUST compute both sides over the same projection;
comparing two different projections detects nothing.

## 7. The `benzene:spec` topic

The reserved `benzene:spec` topic (wire-contracts §7) answers `ok` with this document. Its optional
request is `{ "type"?: string, "format"?: string }`: an absent body, an empty body, a `null` body
and `{}` all mean `type: "benzene"`, `format: "json"` — this document as JSON. Values are lowercase ASCII,
compared case-insensitively. A `type` or `format` the service does not support is answered
`not-implemented`, never with a different document; other types (`openapi`, `asyncapi`, …) are port
extensions and are not pinned. Over HTTP, `GET /benzene/spec` copies its `type` and `format` query
parameters onto the request (transport-bindings §1).

`benzene:spec` runs the same pipeline, and so the same authentication, as every other topic. A port
that serves `GET /benzene/spec` some other way MUST route it through the pipeline.

## 8. Conformance

- **A contract-document producer** — any port whose services claim the Cloud Service Profile, whose
  R5 requires the document — passes `conformance/contract-document-cases.json`'s `derivationCases`,
  `namingCases` and `specTopicCases`, its `specHttpCases` if the port has an HTTP binding, and
  `conformance/contract-hash-cases.json`'s cases without a projection.
- **A client generator** passes `contract-document-cases.json`'s `parseCases`, `topicScopeCases` and
  `schemaClosureCases`, and every case in `contract-hash-cases.json`.

See [conformance/README.md](conformance/README.md) for the case formats and the canonical types.
