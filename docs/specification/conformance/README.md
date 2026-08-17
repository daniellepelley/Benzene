# Conformance Fixtures

**Status: DRAFT v0.1**

Language-neutral test fixtures for the contracts in [wire-contracts.md](../wire-contracts.md) and
the behaviors in [core-concepts.md](../core-concepts.md). Every Benzene implementation runs the
same fixtures through its own runner; an implementation that passes them (plus the live-interop
tests described in [porting-guide.md §3](../porting-guide.md#3-conformance-testing)) is conformant.
API shape is explicitly not part of conformance.

The .NET runner lives at `test/Benzene.Conformance.Test/` and is the reference for how a runner
consumes these files.

## Fixture files

| File | Verifies |
|---|---|
| `status-vocabulary.json` | The status vocabulary strings and their success/failure classification (wire-contracts §3) |
| `http-status-mapping.json` | Benzene→HTTP and HTTP→Benzene status tables (wire-contracts §4.1) |
| `grpc-status-mapping.json` | Benzene→gRPC and gRPC→Benzene status tables (wire-contracts §4.2) |
| `envelope-cases.json` | End-to-end message envelope handling: request in, pipeline + canonical handler, response envelope out (wire-contracts §1, core-concepts §4–6) |
| `problem-details-cases.json` | The problem-type registry, the canonical `conformance:problem` handler's envelope behavior, and the HTTP-binding-only signalling rules (wire-contracts §1.3, §3.1, §4.1) |
| `transport-metadata-cases.json` | Topic resolution and header mapping on transports carrying Benzene metadata natively — the reserved metadata key names (wire-contracts §2, transport-bindings §1) — required for ports binding any such transport |
| `mesh-descriptor-cases.json` | ServiceDescriptor derivation from the canonical handlers and the canonical outbound registration, including payload schemas, `produces`, and descriptorHash properties (mesh §2) — required for ports that implement mesh |
| `mesh-trace-cases.json` | TraceEvent behavior: traceparent join/reject rules and the invocation→semantic-status mapping (mesh §3) — required for ports that implement mesh |
| `mesh-collector-cases.json` | Collector ingest, validation, derivation, and degradation behavior (mesh §4–6) — required for ports that implement a collector |
| `mesh-issue-cases.json` | Issue-feed collector behavior: `benzene:mesh:issues` ingest, fingerprint delta-merge, liveness/feed-absence derivation (mesh §4.1) — required only for collectors claiming the issue feed |
| `mesh-service-version-cases.json` | Service-version identity: the catalog keyed by `(service, serviceVersion)`, so two releases side by side are two entries (mesh §2.4, §4) — required only for collectors claiming service-version identity |
| `mesh-version-order-cases.json` | Service-version **order**: the declared comparison schemes, the not-orderable outcome, and parse rejection (mesh §2.5) — required only for ports that order service versions |
| `contract-document-cases.json` | Contract Document parse/validate, topic-scope projection, and schema-closure behavior (contract-document.md §§1-5) — required for ports that ship a client generator |
| `contract-hash-cases.json` | Exact `contractHash` values for the normalization + canonicalization + hash pipeline (contract-document.md §6) — required for ports that ship a client generator |

Which fixtures a given conformance claim requires
([cloud-service-profile.md](../cloud-service-profile.md) §5):

| Claim | Required fixtures |
|---|---|
| Benzene Core | `status-vocabulary.json`, the mapping tables for each protocol the port binds, `envelope-cases.json`, `transport-metadata-cases.json` for each metadata-carrying transport the port binds, and `problem-details-cases.json`'s `registry` and `envelopeCases` groups |
| HTTP binding conformance | additionally `problem-details-cases.json`'s `httpRules` group — required only for each HTTP binding a port ships (the same conditional shape as `transport-metadata-cases.json` above); a port with no HTTP binding is unaffected |
| Cloud Service Profile support | Core, plus `mesh-descriptor-cases.json` and `mesh-trace-cases.json` |
| Collector implementations | additionally `mesh-collector-cases.json` (collector-only; not part of the profile) |
| Issue-feed collectors | additionally `mesh-issue-cases.json` (optional feed, mesh §4.1; a collector without it stays collector-conformant) |
| Service-version-aware collectors | additionally `mesh-service-version-cases.json` (mesh §2.4; a collector without it stays collector-conformant, because a descriptor that omits `serviceVersion` keys exactly as it always did) |
| Service-version **ordering** | additionally `mesh-version-order-cases.json` (mesh §2.5; a port implementing §2.4 identity without §2.5 ordering stays service-version conformant) |
| Client-generation conformance | `contract-document-cases.json` and `contract-hash-cases.json` — required only for a port that ships a client generator (contract-document.md); a port that never generates clients from the Contract Document is unaffected by these two fixtures, the same conditional shape as the collector fixtures above |

At Core level the mesh fixtures apply only to ports that implement the optional mesh module
(mesh.md §7); a port without mesh skips them and remains Core-conformant.

## Canonical handlers

Envelope cases run against a fixed set of handlers that every runner MUST register natively,
with exactly these topics and behaviors:

| Topic | Request body | Behavior |
|---|---|---|
| `conformance:greet` | `{ "name": string }` | Returns `ok` with payload `{ "greeting": "Hello <name>" }` |
| `conformance:status` | `{ "status": string, "errors": string[]? }` | Returns the given status verbatim. For a success-class status, the payload is `{ "applied": "<status>" }`; for a failure-class status, the result carries the given `errors` (each string projected to a message-only structured error) and no payload. |
| `conformance:problem` | `{ "message": string, "field"?: string, "code"?: string, "appType"?: string }` | Returns a `validation-error` result carrying exactly one structured error (`message`/`field`/`code` from the request). When `appType` is given, the emitted problem document's `type` member is `appType` verbatim instead of the registry URI — the application-authored-problem case (wire-contracts §1.3); `benzeneStatus` is still `validation-error` and `errors` still carries the one structured error. |

No handler is registered for any other topic — cases targeting unregistered or empty topics
verify the router's `not-found` / `validation-error` behavior.

## Canonical outbound registration

`mesh-descriptor-cases.json` additionally registers one **outbound** record (mesh.md §2.3) — no
handler, since nothing here receives:

| Topic | Request body | Response |
|---|---|---|
| `conformance:log` | `{ "message": string }` | none declared |

This is what populates `ServiceDescriptor.produces` for the fixture; it is never sent, and no
handler answers it in any other fixture file — it exists purely to give every runner one shared,
concrete outbound registration to derive a `produces` entry from.

## Status mapping case format

`http-status-mapping.json` and `grpc-status-mapping.json` each list `forward`/`reverse`
`{ "from": ..., "to": ... }` rows. A `forward` row's `from` may be the sentinel `"<unknown>"`
(a status outside wire-contracts §3's vocabulary) rather than a real status string; such a row also
carries `isSuccessful` (boolean), since wire-contracts §4's per-protocol tables route an unknown
status to the generic-success or generic-error row depending on it (§1.2's authoritative signal —
see §3's note on unknown-status routing). Each fixture accordingly lists **two** `"<unknown>"` rows,
one per `isSuccessful` value. `isSuccessful` is meaningless on any other row (a known status maps by
its own row regardless) and MUST be omitted there.

## Envelope case format

```json
{
  "name": "unique-case-name",
  "request":  { "topic": "...", "headers": { }, "body": "..." },
  "expected": {
    "statusCode": "ok",
    "body": { "greeting": "Hello world" },
    "headers": { "content-type": "application/json" }
  }
}
```

- `request` is the inbound envelope (wire-contracts §1.1), verbatim.
- `expected.statusCode` is compared exactly.
- `expected.isSuccessful`, when present, is compared exactly against the response envelope's
  `isSuccessful` field (wire-contracts §1.2) — the wire's authoritative success/failure signal.
- `expected.body`, when present, is parsed JSON compared by **subset**: every field in the
  expected object must be present in the actual (parsed) response body with a deeply-equal value;
  extra fields in the actual body (including null-valued ones) are ignored. This is deliberate —
  implementations may enrich responses, and writers may emit or omit null properties
  (wire-contracts §6).
- `expected.headers`, when present, is compared by subset the same way (keys case-insensitive).
- `expected.bodyExclude`, when present, lists members that must **not** appear in the parsed
  response body at all — a negative assertion, mirroring `transport-metadata-cases.json`'s
  `headersExclude`. A failure case listing `"status"` pins that no numeric `status` member
  (wire-contracts §1.3) leaks onto a transport with no HTTP response, e.g. the raw envelope; a
  member absent from `expected.body` is otherwise unconstrained (subset matching alone would
  permit it either way), so `bodyExclude` is the only way to pin genuine absence.
- Human-readable message wording (e.g. the `detail`/`title` text of router-generated or registry
  problem documents) is intentionally not asserted.

## Problem details case format

`problem-details-cases.json` (wire-contracts §1.3, §3.1, §4.1) has three independent groups:

- **`registry`** — `rows` lists every problem-type registry entry
  (`benzeneStatus`/`type`/`httpStatus`) directly, with no message to build — the cheapest check,
  the same rationale as `defaultMetadataKeys`. `unknownStatus` pins the fallback for an
  application-defined status: no registry row, HTTP status 500.
- **`envelopeCases`** — cases in exactly the [envelope case format](#envelope-case-format) above,
  run against the canonical `conformance:problem` handler (see Canonical handlers). They pin
  structured `errors` round-tripping through the problem document, application-defined `type`
  passthrough via `appType`, and (via `bodyExclude`) that framework-produced problem documents
  carry no `instance` and no numeric `status` off an HTTP transport.
- **`httpRules`** — required only for ports that ship an HTTP binding (see the fixture-claims
  table above; the same conditional shape as `transport-metadata-cases.json`). `failureCases`
  lists, per Benzene status, the `httpStatus` an HTTP-bound failure response for that status MUST
  carry both as its response line and as the problem document's `status` member — the two MUST
  come from the same mapping (wire-contracts §4.1) so they can never disagree. The response's
  `content-type` MUST be `application/problem+json` for a JSON-negotiated failure response.
  `successCase` pins that success responses are unaffected: no problem document, ordinary content
  type. Title/detail wording is never asserted, per the envelope case format rule above.

## Transport metadata case format

`transport-metadata-cases.json` covers the transports that carry Benzene metadata natively —
SQS/SNS message attributes, Pub/Sub attributes, Service Bus/Event Hub application properties,
Kafka/RabbitMQ headers. Each exposes the same shape under a different native name (a string→string
dictionary), so the cases are written against that **neutral dictionary** and each runner adapts a
case to its own native message before decoding it.

```json
{
  "name": "topic-resolves-from-the-reserved-key",
  "metadata": { "topic": "conformance:greet", "x-correlation-id": "abc-123" },
  "expected": {
    "topic": "conformance:greet",
    "headers": { "x-correlation-id": "abc-123" },
    "headersExclude": ["topic"]
  }
}
```

- `metadata` is the native metadata dictionary, before decoding.
- `expected.topic` is compared exactly; `""` means the binding resolved no topic (not an error —
  what an empty topic *means* is the router's business, pinned by `envelope-cases.json`).
- `expected.headers` is subset-matched, keys case-insensitive, as for envelope cases.
- `expected.headersExclude` lists keys that must **not** appear as headers — a metadata key the
  binding consumed as routing information must not also leak into the header dictionary.
- `expected.version`, where present, is the resolved payload version.
- A case with `"requires": "versioning"` applies only to ports implementing payload versioning
  (`benzene-version` is tier C).

`defaultMetadataKeys` names the keys themselves, and `topicSources` records where each binding
gets its topic. Both are directly assertable: a port can compare its own constant against
`defaultMetadataKeys.topic` without building a message at all, which is the cheapest possible
check and the one that catches a rename.

**The names are configurable, so the fixture asserts two things.** `metadataCases` run against the
defaults — the baseline that lets two untouched Benzene services interoperate. `overrideCases` each
carry a `metadataKeys` object the runner applies before decoding, and assert that the replacement
actually routes *and* that the default key becomes an ordinary header once overridden. A port that
hard-codes a name passes the first group and fails the second.

Bindings whose `source` is not `metadata` are listed deliberately: EventBridge routes on
`detail-type` and DynamoDB Streams derives `{tableName}:{eventName}`, so those bindings MUST NOT
require a `topic` attribute, and the metadata cases do not apply to them.

**Why this fixture exists.** The metadata key names are the one part of the wire contract that no
other fixture touches — envelope, status and protocol-mapping cases never look at native metadata.
A port can therefore rename or misspell the topic attribute, pass every other fixture, and still be
unable to exchange a single queue message with another port. That is not hypothetical: it is
exactly how the .NET and Python ports diverged when the key was briefly `benzene-topic`
(`work/benzene-naming-principle.md` §3c, since reversed).

The EventBridge embedded-headers key (`_benzeneHeaders`, wire-contracts §2, tier D) is deliberately
**not** pinned here: it is scheduled to be renamed to `benzene-headers`
(`work/benzene-headers-plan.md`), and a fixture asserting the current spelling would have to change
with it.

## Mesh fixture formats

Subset matching is as for envelope cases, with one addition needed by these fixtures:
**arrays compare by exact length with per-element subset matching**, and an expected empty
array (`[]`) matches an actual array that is empty *or absent* (writers may omit empty
collections).

- `mesh-descriptor-cases.json` — the runner registers the two canonical envelope handlers and the
  one canonical outbound registration (both above) natively, builds the service descriptor with
  the fixture's `serviceInfo`, and subset-compares `expectedDescriptor` (`topics` from the
  handlers, `produces` from the outbound registration). `runtime` and the hash value are per-port
  and not pinned; instead `hash` asserts the hash's *properties*: its `sha256:` + 64-hex format,
  invariance to `instanceId`, and sensitivity to `serviceVersion`, to the topic set, and to the
  produced-topic set.
- `mesh-trace-cases.json` — `traceparent` rows assert the join/reject rules of mesh §3
  observationally (a valid header's ids are adopted; an invalid one yields a fresh 32-hex
  trace id and no parent). `invocations` rows run one envelope each through a pipeline with
  the trace middleware installed and the canonical handlers plus one extra canonical mesh
  handler registered:

  | Topic | Request body | Behavior |
  |---|---|---|
  | `conformance:panic` | any | Panics/throws unconditionally — pins the rule that a handler panic is traced as `service-unavailable`, not lost |

  `expectedEvent` is subset-matched against the single TraceEvent exported for that
  invocation. (`conformance:panic` is registered only for these trace cases, not for
  descriptor or envelope cases.)
- `mesh-collector-cases.json` — each case's `steps` run in order against one fresh collector;
  each step is an envelope request/expected pair asserted like an envelope case. The
  `benzene:mesh:query:*` responses are asserted as the observable surface for the ingest/derivation
  rules of mesh §4–6; those query shapes are not themselves promoted contracts.
- `mesh-service-version-cases.json` — same runner and assertion rules as
  `mesh-collector-cases.json`, applied to the keying rule alone. The cases pin that two declared
  versions of one service are two catalog entries rather than one overwriting the other, that this
  still holds when the two versions carry **identical** contracts (a version is an entity, not a
  shape — mesh §2.4, so a collector keying on `descriptorHash` instead of on `serviceVersion`
  fails here), that re-registering one version leaves the other's topics intact, and that a
  descriptor omitting `serviceVersion` keys exactly as it did before the rule existed. That last
  case is why this fixture is separate: it is the compatibility guarantee that lets an existing
  collector keep passing `mesh-collector-cases.json` untouched.

## Contract Document and contract hash case formats

Both files pin [contract-document.md](../contract-document.md). Every case group in
`contract-document-cases.json` shares a `documents` map at the top of the file, keyed by id;
each case references one document by `documentRef` rather than repeating it. `expectedTopics` and
`expectedComponents` are compared as **sets** — order-independent, no duplicates expected either
side — unlike the positional array comparisons used elsewhere in this directory, since a topic
scope or a schema closure is a membership question, not a sequence.

- **`parseCases`** — each gives a `documentRef` (and, for the fail-loud case, `options`) and an
  `expected` (subset-matched: `openapi`, and per-entry `requests[]`/`events[]` rows keyed by
  `topic`, asserting `versionPresent`/`version`/`reserved` as applicable) or an `expectedError`
  (asserting the failure's `unknownTopics` and `validTopics` sets, contract-document.md §5.2).
  `versionPresent: false` asserts the field is genuinely **absent**, not an empty string
  (contract-document.md §2's absent-means-unversioned rule).
- **`topicScopeCases`** — each gives a `documentRef`, `options` (`topics`/`includeReserved`, both
  optional, contract-document.md §5.2), and the resulting `expectedTopics` set of surviving
  `requests[]` topic ids.
- **`schemaClosureCases`** — each gives a `documentRef`, a `topic`, and the `expectedComponents`
  set: every `components.schemas` key reachable from that topic's request and response by the walk
  of contract-document.md §5.3. Includes a `$ref` cycle case and an `allOf`/`oneOf` reach case, per
  that section's normative walk.

`contract-hash-cases.json` lists `cases`, each an input `document` (already in whatever projection
the case is about — a whole-service document, or one already topic-scoped per §5.3) and the exact
`expectedHash` a conformant `normalize` → `canonicalJSON` (RFC 8785/JCS) → SHA-256 pipeline
(contract-document.md §6.2) MUST produce. Two cases are deliberately **not already normalized**
(one carries `example`/`messageEndpoint`/`transports`, one carries reserved entries) specifically so
their `expectedHash` — identical to the minimal case's — proves an implementation performs the
strip, not merely hashes its input verbatim.

## Mapping table format

```json
{
  "forward": [ { "from": "ok", "to": "200" } ],
  "reverse": [ { "from": "200", "to": "ok" } ]
}
```

Each row is asserted independently against the implementation's forward/reverse mapper. Rows with
`from` of `"<unknown>"` assert the mapper's default for an unrecognized input.
