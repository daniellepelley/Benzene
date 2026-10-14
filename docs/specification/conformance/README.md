# Conformance Fixtures

Language-neutral test fixtures for the contracts in [wire-contracts.md](../wire-contracts.md) and
the behaviors in [core-concepts.md](../core-concepts.md). Every Benzene implementation runs the
same fixtures through its own runner; an implementation that passes them (plus the live-interop
tests described in the [porting guide](../../guides/porting-guide.md#3-conformance-testing)) is conformant.
API shape is explicitly not part of conformance.

The .NET runner lives at `test/Benzene.Conformance.Test/` and is the reference for how a runner
consumes these files.

## Spec releases and pinning

The specification is released as git tags of this repository, `spec-<version>`, where `<version>` is
the content of `docs/specification/VERSION` at that commit. Releasing is one edit — bump `VERSION`;
the `release-spec` workflow tags the commit, and never moves a tag once published. A release is cut
whenever the fixtures or a contract change, and only then; the workflow warns while `main`'s fixtures
differ from the release `VERSION` still names.

Every repository that vendors these fixtures records the release it took them from in a
`SPEC_VERSION` file beside its snapshot: a tag (`spec-0.1.0`) — or, while catching up mid-release, a
commit of this repository. Its drift check has two jobs:

- **Pinned (blocking).** The snapshot must be byte-identical, in both directions, to this directory
  at the pinned ref. A change here can never turn another repository red: it stays green on the
  release it claims until it chooses to move.
- **Behind latest (non-blocking).** A warning when a newer `spec-*` release exists, so being behind
  is visible rather than silent.

To move forward, a port implements the release, copies this directory's `*.json` into its snapshot,
writes the new tag into `SPEC_VERSION`, and lands all three in one change.

## Fixture files

| File | Verifies |
|---|---|
| `status-vocabulary.json` | The status vocabulary strings and their success/failure classification (wire-contracts §3) |
| `http-status-mapping.json` | Benzene→HTTP and HTTP→Benzene status tables (wire-contracts §4.1) |
| `grpc-status-mapping.json` | Benzene→gRPC and gRPC→Benzene status tables (wire-contracts §4.2) |
| `envelope-cases.json` | End-to-end message envelope handling: request in, pipeline + canonical handler, response envelope out, including version selection (wire-contracts §1, core-concepts §2, §4–6) |
| `problem-details-cases.json` | The problem-type registry, the canonical `conformance:problem` and `conformance:validate` handlers' envelope behavior, and the HTTP-binding-only rules: failure signalling, the envelope endpoint and the health routes (wire-contracts §1.3, §3.1, §4.1, §5; transport-bindings §1) |
| `transport-metadata-cases.json` | Topic resolution and header mapping on transports carrying Benzene metadata natively — the reserved metadata key names (wire-contracts §2, transport-bindings §1) — required for ports binding any such transport; plus each one-way binding's settlement shape and allowed defaults (transport-bindings §1, obligation 8) |
| `mesh-trace-cases.json` | TraceEvent behavior: traceparent join/reject rules and the invocation→semantic-status mapping (mesh §3) — required of a meshed service's port (mesh §7.1) |
| `mesh-collector-cases.json` | Collector ingest, validation, derivation, and degradation behavior (mesh §4–6) — required for ports that implement a collector |
| `mesh-issue-cases.json` | Issue-feed collector behavior: `benzene:mesh:issues` ingest, fingerprint delta-merge, error-detail merge and bound, liveness/feed-absence derivation (mesh §4.1) — required only for collectors claiming the issue feed |
| `mesh-service-version-cases.json` | Service-version identity: the catalog keyed by `(service, serviceVersion)`, so two releases side by side are two entries (service-versions.md §1, mesh §4) — required only for collectors claiming service-version identity |
| `mesh-version-order-cases.json` | Service-version **order**: the declared comparison schemes, the not-orderable outcome, and parse rejection (service-versions.md §2) — required only for ports that order service versions |
| `contract-document-cases.json` | The contract document: derivation from the canonical registrations with exact hashes, the naming rules, the `benzene:spec` request (producer groups); parse, topic scope and schema closure (client-generator groups) (contract-document.md) |
| `contract-hash-cases.json` | Exact `contractHash` and `registrationHash` values for the projection, canonicalization and hash pipeline (contract-document.md §6) — the cases without a projection are required of every contract-document producer; the projection cases, of a client generator |

Which fixtures a given conformance claim requires. A claim is made per level — **Core**, **per
binding**, the **Profile**, and **per module** — and a port claims only what it implements.

| Level | Claim | Required fixtures |
|---|---|---|
| Core | Benzene Core | `status-vocabulary.json`, the mapping tables for each protocol the port binds, `envelope-cases.json`, and `problem-details-cases.json`'s `registry` and `envelopeCases` groups |
| Per binding | each metadata-carrying binding (queues, topics, streams) | `transport-metadata-cases.json` for that binding, and its row in `settlementDefaults` (every one-way binding the port ships) |
| Per binding | each HTTP binding | `problem-details-cases.json`'s `httpRules` group. Within it, `envelopeEndpoint` applies to a port that serves the envelope over HTTP, and `healthEndpoint` to a port that also ships health checks |
| Profile | Cloud Service Profile support | Core, and the contract-document producer claim below |
| Per module | Meshed service (mesh §7.1) | The profile, and `mesh-trace-cases.json` |
| Per module | Mesh collector | `mesh-collector-cases.json` (not required of a meshed service, M1) |
| Per module | Mesh issue feed | `mesh-issue-cases.json` (optional feed, mesh §4.1; a collector without it stays collector-conformant) |
| Per module | Service versions — *Provisional* | `mesh-service-version-cases.json` (identity) and `mesh-version-order-cases.json` (order), [service-versions.md](../service-versions.md); a collector without the module stays collector-conformant, because a registration that omits `serviceVersion` keys exactly as it always did |
| Profile | Contract-document producer (R5) | `contract-document-cases.json`'s `derivationCases`, `namingCases`, `specTopicCases`, and `specHttpCases` for a port with an HTTP binding; `contract-hash-cases.json`'s cases without a `projection` |
| Per module | Client generator | `contract-document-cases.json`'s `parseCases`, `topicScopeCases` and `schemaClosureCases`, and every case in `contract-hash-cases.json` |

MCP and claim check (both *Provisional*) have no fixtures yet. At Core level the mesh fixtures apply
only to ports that implement the optional mesh module; a port without mesh skips them and remains
Core-conformant.

## Canonical handlers

Envelope cases run against a fixed set of handlers that every runner MUST register natively,
with exactly these topics and behaviors:

| Topic | Request body | Behavior |
|---|---|---|
| `conformance:greet` | `{ "name": string }` | Returns `ok` with payload `{ "greeting": "Hello <name>" }` |
| `conformance:status` | `{ "status": string, "errors": string[]? }` | Returns the given status verbatim. For a success-class status, the payload is `{ "applied": "<status>" }`; for a failure-class status, the result carries the given `errors` (each string projected to a message-only structured error) and no payload. |
| `conformance:problem` | `{ "message": string, "field"?: string, "code"?: string, "appType"?: string }` | Returns a `validation-error` result carrying exactly one structured error (`message`/`field`/`code` from the request). When `appType` is given, the emitted problem document's `type` member is `appType` verbatim instead of the registry URI — the application-authored-problem case (wire-contracts §1.3); `benzeneStatus` is still `validation-error` and `errors` still carries the one structured error. |
| `conformance:versioned` | none (`{}`) | Registered **twice**: at version `v1`, declared as the topic's default handler, and at version `v2`. Each returns `ok` with payload `{ "version": "<its version>" }`. Exercises version selection (core-concepts §2): an exact match, an absent version going to the default, and an unknown version as `not-found`. |
| `conformance:validate` | `{ "name": string, "items": [{ "sku": string }] }` | Validated through the port's **own** validation integration (not hand-written in the handler), with two rules: `name` non-empty, and every `items[].sku` present and non-empty. Returns `ok` when valid. Only cases marked `"requires": "validation"` use it, so a port without a validation integration skips them. |

No handler is registered for any other topic — cases targeting unregistered or empty topics
verify the router's `not-found` / `validation-error` behavior.

**The canonical health check.** `problem-details-cases.json`'s `httpRules.healthEndpoint` cases
each list the checks to register — a name and the status (`ok`, `warning` or `failed`) the check
reports — in place of any the runner would otherwise register.

## Canonical outbound declarations

`contract-document-cases.json`'s `derivationCases` declare the canonical outbound (contract-document.md
§3): the **event** `conformance:greeted`, the one-way **send** `conformance:log`, and the **send**
`conformance:lookup`, which expects a reply. None is ever sent.

## Canonical payload types

The contract-document cases use payload types with fixed names, so that the canonical document's
component keys match across ports. A runner declares its types with exactly these names (by its
port's declared-name mechanism, contract-document.md §4.1 rule 5, where its default name would
differ). Every property is a string, and **every property is always emitted** — a nullable one as
`null` when unset — so every property is `required` (contract-document.md §4.2). A runner configures
its types and marshaler so: in TypeScript a nullable property is `T | null`, not optional; in .NET
nulls are written; in Python the field has no default and `None` is dumped; in Go the field has no
`omitempty`.

| Type | Shape | Used by |
|---|---|---|
| `GreetRequest` | `{ name }` | `conformance:greet` request |
| `GreetResponse` | `{ greeting }` | `conformance:greet` response |
| `Greeted` | `{ greeting }` | event `conformance:greeted` |
| `LogEntry` | `{ message }` | send `conformance:log` (one-way) |
| `LookupRequest` | `{ key }` | send `conformance:lookup` |
| `LookupResponse` | `{ value }` | send `conformance:lookup` reply |
| `ShapesRequest` | `{ count: integer, ratio: number, active: boolean, tags: string[], labels: map of string, note: nullable string, child: ShapesChild, nullableChild: nullable ShapesChild }` | `conformance:shapes`, a handler with no response payload |
| `ShapesChild` | `{ name }` | nested in `ShapesRequest` |
| `TreeNode` | `{ name, parent: nullable TreeNode, children: TreeNode[] }` | `conformance:tree`, a handler with no response payload |

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
- `expected.errorFields`, when present, lists the `field` values the problem document's `errors`
  must carry, compared as a **set**: every error carries a `field`, and the set of them equals the
  list. Order, messages and codes are not asserted — they are the validation library's own.
- A case with `"requires": "<capability>"` applies only to ports implementing it (`validation`:
  a validation integration that can run `conformance:validate`).
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
  carry no `instance`, and no numeric `status` anywhere but a route-mapped HTTP response — not
  inside an envelope, whatever carries it.
- **`httpRules`** — required only for ports that ship an HTTP binding (see the fixture-claims
  table above; the same conditional shape as `transport-metadata-cases.json`). `failureCases`
  lists, per Benzene status, the `httpStatus` that the response of a **route-mapped** topic (not
  the envelope endpoint, which answers 200) MUST
  carry both as its response line and as the problem document's `status` member — the two MUST
  come from the same mapping (wire-contracts §4.1) so they can never disagree. The response's
  `content-type` MUST be `application/problem+json` for a JSON-negotiated failure response.
  `successCase` pins that success responses are unaffected: no problem document, ordinary content
  type. Title/detail wording is never asserted, per the envelope case format rule above.
  - `envelopeEndpoint` — `POST` each case's `request` (or its `rawBody`, verbatim, when present) to
    `path`. `expected.httpStatus` is the response line; `expected.envelope`, when present, is an
    [envelope case](#envelope-case-format) expectation applied to the response body. Every
    processed envelope is 200, whatever its status; only a carrier failure (a malformed envelope)
    is not (transport-bindings §1).
  - `healthEndpoint` — register exactly the case's `checks` (see Canonical handlers), then `GET`
    `path`. `expected.httpStatus` is the response line — 503 when the report's `isHealthy` is
    false — and `expected.body` is subset-matched against the health response
    (wire-contracts §5).

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
- `benzene-version` is the one version header (wire-contracts §2, Core): every port resolves it,
  so the version cases here run unconditionally.

`defaultMetadataKeys` names the keys themselves, and `topicSources` records where each binding
gets its topic; a `fallback` names what a metadata binding routes on when the topic key is absent
(RabbitMQ's routing key, Kafka's own topic) — the key, when present, always wins. Both are directly assertable: a port can compare its own constant against
`defaultMetadataKeys.topic` without building a message at all, which is the cheapest possible
check and the one that catches a rename.

**The names are configurable, so the fixture asserts two things.** `metadataCases` run against the
defaults — the baseline that lets two untouched Benzene services interoperate. `overrideCases` each
carry a `metadataKeys` object the runner applies before decoding, and assert that the replacement
actually routes *and* that the default key becomes an ordinary header once overridden. A port that
hard-codes a name passes the first group and fails the second.

Bindings whose `source` is not `metadata` are listed deliberately: EventBridge routes on
`detail-type` and DynamoDB Streams derives `{tableName}:{eventName}`, so those bindings MUST NOT
require a `topic` attribute (transport-bindings §1), and the metadata cases do not apply to them.

`defaultMetadataKeys.headers` is the EventBridge embedded-headers key, `benzene-headers`
(wire-contracts §2, Binding: EventBridge); only a port with an EventBridge binding asserts it.

**Settlement defaults.** `settlementDefaults.bindings` records each one-way binding's **shape** —
`queue`, `one-shot` (a single delivery with no per-message ack, such as an asynchronous function
invocation), `stream` or `request-response` — and `settlementDefaults.shapes` the defaults each
shape allows: `onFailure` for a failed, result-less or erroring invocation, and `unmatchedTopic`
for a topic no handler serves. A port asserts, for each binding it ships, that its shape matches and
its default is in the allowed list; a stream binding that defaults to `skip-and-continue` must also
say so in its documentation (transport-bindings §1, obligation 8). These are constants, compared
without sending a message — the same cheap check as `defaultMetadataKeys`. A binding a port ships
that the list does not name is added here before the port claims it.

**Why this fixture exists.** The metadata key names are the one part of the wire contract that no
other fixture touches — envelope, status and protocol-mapping cases never look at native metadata.
A port can therefore rename or misspell the topic attribute, pass every other fixture, and still be
unable to exchange a single queue message with another port. That is not hypothetical: it is
exactly how the .NET and Python ports diverged when the key was briefly `benzene-topic`
(`work/archive/benzene-naming-principle.md` §3c, since reversed).

## Mesh fixture formats

Subset matching is as for envelope cases, with one addition needed by these fixtures:
**arrays compare by exact length with per-element subset matching**, and an expected empty
array (`[]`) matches an actual array that is empty *or absent* (writers may omit empty
collections).

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
  envelope or contract-document cases.)
- `mesh-collector-cases.json` — each case's `steps` run in order against one fresh collector;
  each step is an envelope request/expected pair asserted like an envelope case. The
  `benzene:mesh:query:*` responses are asserted as the observable surface for the ingest/derivation
  rules of mesh §4–6; those query shapes are not themselves promoted contracts. A
  `benzene:mesh:register` body is a contract document carrying only the members the case needs,
  with `{}` schemas (mesh §2 makes a collector accept such a body); the read models' `topics`
  count is the registration's `requests`, and their `descriptor` feed is its registration.
- `mesh-service-version-cases.json` — same runner and assertion rules as
  `mesh-collector-cases.json`, applied to the keying rule alone. The cases pin that two declared
  versions of one service are two catalog entries rather than one overwriting the other, that this
  still holds when the two versions carry **identical** contracts (a version is an entity, not a
  shape — service-versions.md §1, so a collector keying on `registrationHash` instead of on `serviceVersion`
  fails here), that re-registering one version leaves the other's topics intact, that a
  heartbeat's `registrationHash` is compared with its own version's registration (mesh §5), and
  that a registration omitting `serviceVersion` keys exactly as it did before the rule existed. That last
  case is why this fixture is separate: it is the compatibility guarantee that lets an existing
  collector keep passing `mesh-collector-cases.json` untouched. The entries are observed through
  the **service** query, whose `versions` array lists the live versions; in this fixture it is
  compared as a **set keyed by `serviceVersion`** (same length, and each expected element
  subset-matches the actual element with its `serviceVersion`), because no order is pinned. The
  fleet query keeps one row per service (graph membership is by service, mesh §4); which version
  the row headlines is not pinned.

## Contract Document and contract hash case formats

Both files pin [contract-document.md](../contract-document.md). `contract-document-cases.json` keeps
shared documents in a `documents` map, which cases reference by `documentRef`. Topic, event, send and
component lists in expectations compare as **sets**.

**Producer groups** — every contract-document producer:

- **`derivationCases`** — each lists `registrations` (handlers in `requests`, event declarations in
  `events`, send declarations in `sends`, each naming a canonical payload type), an optional
  `serviceInfo` (identity and placement), and an `expected` document. The runner registers exactly
  those, with no HTTP mappings and with that identity, derives its document, subset-matches
  `expected`'s identity fields against it, and
  compares each entry — matched by topic and version — exactly on `topic`, `version`, `request`,
  `response` and `message`, after applying contract-document.md §6.2 steps 3–4 to its own schemas
  (dereferenced, annotations removed, `required` sorted), so hoisting and naming differences do not
  fail it; other members (`example`) are not compared. A `response` absent from an expected entry
  MUST be absent from the derived one.
  It also asserts that every component key is valid (§4.1 rule 3) and every `$ref` resolves, and
  that the document's `contractHash` and `registrationHash` equal the expected values exactly.
- **`namingCases`** — each gives `types` (two distinct types, keyed by a qualified name, with their
  shapes), optional `declaredNames`, and `registrations`. `expected.error: "component-name-collision"`
  means derivation fails before any message is handled and the failure names `expected.name`;
  `expected.componentNames` means derivation succeeds and the components include those keys. These
  cases presuppose that the port hoists registered payload types (§4.1 rule 1); a port that inlines
  them has no names to collide, and records the group as a gap in its capability matrix.
- **`specTopicCases`** — each sends `request` as the body of a `benzene:spec` envelope (`null`
  means an absent body). `expected.statusCode` is compared exactly; `expected.document` is subset-matched
  against the response body. **`specHttpCases`** (ports with an HTTP binding) send `GET` to `path`
  and assert `expected.httpStatus` and, where given, `expected.document`.

**Client-generator groups:**

- **`parseCases`** — each gives a `documentRef` (and, for the fail-loud case, `options`) and either
  an `expected` (subset-matched; per-entry rows keyed by `topic` and `version` assert `versionPresent`/`version`
  and `responsePresent`, where "present: false" means the member is genuinely **absent**) or an
  `expectedError` (`kind`: `unknown-schema-dialect`, or `unknown-topics` with the `unknownTopics`
  and `validTopics` sets, contract-document.md §5.2). A `benzene:` entry is ignored, not listed.
- **`topicScopeCases`** — each gives a `documentRef`, `options` (`topics`, optional), and the
  `expectedTopics` set of surviving `requests[]` topics; where given, `expectedEvents` and
  `expectedSends` are the sets that pass through unchanged.
- **`schemaClosureCases`** — each gives a `documentRef`, a `topic`, and the `expectedComponents`
  set reachable by contract-document.md §5.3's walk, including a `$ref` cycle and an
  `allOf`/`oneOf` reach case.

`contract-hash-cases.json` lists `cases`, each a `document` and the exact `expectedContractHash` and
`expectedRegistrationHash` (contract-document.md §6.2). A case with a `projection` first projects the
document — `{"kind":"topic","topic":…}` is §5.3's topic-scoped projection, `{"kind":"include-list",
"topics":[…]}` §5.2's include-list projection — and expects the hashes of that projection; only a
client generator runs those. Several cases are deliberately decorated,
reordered, renamed, hoisted or annotated, and expect the same values as a plain case — proving an
implementation performs every normalization step rather than hashing its input verbatim.

## Mapping table format

```json
{
  "forward": [ { "from": "ok", "to": "200" } ],
  "reverse": [ { "from": "200", "to": "ok" } ]
}
```

Each row is asserted independently against the implementation's forward/reverse mapper. Rows with
`from` of `"<unknown>"` assert the mapper's default for an unrecognized input.
