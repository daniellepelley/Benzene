# Mesh Contracts

**Status: DRAFT v0.1 — promoted from
[benzene-go](https://github.com/daniellepelley/benzene-go)'s `docs/design/mesh.md`. The .NET
implementation is the primary implementation of this document and covers the full contract:
`Benzene.Mesh.Wire` (descriptor, reserved topic, trace feed) and `Benzene.Mesh.Collector` (the
§4–§6 collector, including the §4.1 issue feed), together passing all four conformance fixture files via
`test/Benzene.Conformance.Test`. The Go port (its `mesh`/`meshd` packages) is a fully conforming
implementation — this contract was originally extracted from it — and the two have hosted each
other's services in live cross-language fleets, in both directions. The pre-existing `Benzene.Mesh.*`
visibility packages (aggregator/UI/Tempo, developed independently against the roadmap) are
collector-side idiom this contract doesn't constrain; §9 maps them, and bridging the aggregator's
artifact pipeline to `Benzene.Mesh.Collector` is the natural integration follow-up.**

Benzene Mesh is the *application-level* mesh: every service **declares its full contract** — the
topics it provides (§2, derived from its handler registry) and the topics it consumes (§2,
derived from its outbound registration) — so the fleet graph (catalog, who-calls-whom) is knowable
statically, from descriptors alone, before a single instance is deployed or a single message has
flowed. Running code supplies a second, independent signal — health (§5) and, from the trace feed
(§3), which declared edges are actually being exercised (§4.2) — but running code is never the
*source* of the graph, only a witness to it. This document specifies everything that crosses a
process boundary to make that work identically across language ports.

> **Revision note (2026-08).** Earlier drafts of this document derived consumer edges solely from
> trace parentage ("never declared"). That inverted the mesh's own stated goal — a graph that is
> *derived from running code* was read as *requiring running code*, which made the graph
> unavailable exactly when it is most useful: before deployment, for contract testing, and for a
> topic nothing has called yet but is nonetheless meant to be called. §2 and §4 below supersede
> that rule: **declared** (`ServiceDescriptor`) is now the sole source of the producer/consumer
> graph; **observed** (`TraceEvent` parentage) is a separate, additive signal for liveness and
> drift, never for graph membership. A port conforming to the prior rule is not spec-conformant
> under this revision and MUST add outbound registration (§2) to stay so.

At Core level, mesh is optional, and so is each of its feeds; the normative degradation rule of
§6 applies to every section here. A service claiming the
[Cloud Service Profile](cloud-service-profile.md) MUST provision the service-side feeds (its
R6) — for such a service, §6 governs runtime degradation, not whether the feeds exist.

## 1. The reserved `benzene:mesh` topic

A mesh-enabled service MUST intercept the reserved topic id `benzene:mesh` (plus any app-chosen aliases)
the same way health-check interception works (core-concepts.md §10): interception is by topic id
alone, ignoring version; any other topic passes through unchanged. The response is status `ok`
with the ServiceDescriptor (§2) as payload.

Provisioning this endpoint is a deployment decision: a service that must not expose it (e.g.
pending a security review) simply does not install the interception, and every other mesh feed
keeps working (§6).

## 2. ServiceDescriptor

The service's self-description, derived at startup from its handler registry (what it **provides**)
and its outbound registration (§2.3, what it **consumes**) — never hand-maintained. Also the body of
a `benzene:mesh:register` message (§4).

```json
{
  "service": "orders",
  "serviceVersion": "1.4.2",
  "instanceId": "orders-7f9c",
  "runtime": "go",
  "binding": "http",
  "placement": { "cloud": "aws", "region": "eu-west-1" },
  "topics": [
    {
      "id": "order:create",
      "version": "v2",
      "requestSchema":  { "type": "object", "properties": { "name": { "type": "string" } }, "required": ["name"] },
      "responseSchema": { "type": "object", "properties": { "id":   { "type": "string" } }, "required": ["id"] }
    }
  ],
  "consumes": [
    {
      "id": "payments:capture",
      "version": "v1",
      "requestSchema":  { "type": "object", "properties": { "orderId": { "type": "string" } }, "required": ["orderId"] },
      "responseSchema": {}
    }
  ],
  "descriptorHash": "sha256:…",
  "degraded": ["registry"],
  "profile": { "name": "cloud-service", "missing": ["R6"] }
}
```

- `service` — REQUIRED: the logical service name. Every other field is optional; a port MUST emit
  what it knows and omit (not null) what it doesn't.
- `runtime` — the implementing port identifier (`"go"`, `"dotnet"`, …).
- `binding` — the transport binding in use, when the service knows it.
- `placement.cloud` — detected from the platform's documented environment or configured
  explicitly: `"aws"`, `"azure"`, `"gcp"`, `"self-hosted"`, or any explicit override.
  `placement.region` MUST be emitted only when the platform documents a way to know it — a port
  MUST NOT guess.
- `topics` — every registered topic, sorted by id then version: what this service **provides**.
  Explicit registration (core-concepts.md §9) is what makes the registry the complete truth of what
  the service serves; this field is its projection.
- `consumes` — every registered outbound topic, sorted by id then version: what this service
  **consumes** (§2.3). Same shape as a `topics` entry (`TopicDescriptor`), same schema-derivation
  rules (§2.1) applied to the sender's declared request/response types; `responseSchema` is `{}`
  (unconstrained) when the sender doesn't declare an expected response type. This is the field §4
  reads to build consumer edges — a topic absent here is not consumed by this service, regardless of
  what traffic has or hasn't flowed.
- `degraded` — names the feeds that were unavailable when the descriptor was built (`"registry"` for
  `topics`, `"outbound-registry"` for `consumes`), so a reduced descriptor is distinguishable from a
  service that provides/consumes nothing. A port that has not yet implemented outbound registration
  (§2.3) MUST mark `consumes` degraded rather than emit an empty array — an empty array asserts "this
  service calls nothing," which a port that cannot yet know that has no right to assert.
- `profile` — OPTIONAL: a named conformance-profile self-assessment, when the service claims one
  (e.g. the [Cloud Service Profile](cloud-service-profile.md)'s `"cloud-service"`).
  `profile.name` identifies the profile; `profile.missing` lists the requirement ids the service's
  own wiring knows it does not satisfy, omitted (not empty) when fully conformant. Like
  `degraded`, this is self-description rather than contract — it MUST NOT participate in the
  `descriptorHash` (§2.2) — and it reflects provisioning at wire-up, not runtime health: a
  service's `profile` claim does not change because of runtime degradation (§6).

### 2.1 Schema derivation

`requestSchema`/`responseSchema` describe the **marshaled JSON form** of the registered
request/response types, expressed in a subset of the JSON Schema 2020-12 vocabulary. A port
derives them once at startup, from whatever type information its registration API captures.
The mapping (left column names the language-neutral construct; each port applies it to its own
type system):

| Construct | Schema |
|---|---|
| string | `{"type":"string"}` |
| boolean | `{"type":"boolean"}` |
| integer kinds | `{"type":"integer"}` |
| floating kinds | `{"type":"number"}` |
| timestamp type (marshals RFC 3339) | `{"type":"string","format":"date-time"}` |
| byte array (marshals base64) | `{"type":"string"}` |
| text-marshaling custom type | `{"type":"string"}` |
| raw/unknown JSON, dynamic values, custom serializers | `{}` (unconstrained) |
| nullable/optional of T | T's schema with `"null"` added to its `type` |
| list/array of T | `{"type":"array","items":<T>}` |
| string-keyed map of T | `{"type":"object","additionalProperties":<T>}` |
| object/record | `{"type":"object","properties":{…},"required":[…]}` |

Object rules:

- Serialization attributes/tags control property names and omission exactly as the port's JSON
  marshaler does.
- Properties the marshaler always emits are listed in `required`, in declaration order
  (determinism feeds the hash, §2.2); properties the marshaler may omit (optional/omit-empty)
  are not.
- Embedded/inherited members are flattened the way the port's marshaler flattens them.
- Recursive types MUST be cut at the cycle with `{}` — schemas stay self-contained; no `$ref`.
- Constructs the marshaler cannot serialize map to `{}`.

Two ports registering equivalent canonical types MUST produce identical `topics`/`consumes`
entries — this is pinned by `conformance/mesh-descriptor-cases.json`.

### 2.2 descriptorHash

`"sha256:" + lowercase-hex(sha256(canonicalJSON(descriptor)))`, where the hashed descriptor has
`instanceId`, `degraded`, `profile`, and `descriptorHash` itself blanked. The hash covers the *contract*
(identity, placement, topics, consumes, schemas):

- Two instances of the same build MUST hash identically (`instanceId` excluded).
- The hash MUST change when the contract changes (topics, consumes, schemas, `serviceVersion`,
  placement) — adding, removing, or re-typing a consumed topic is a contract change exactly as
  adding, removing, or re-typing a provided one is.

Canonical JSON: object members in a fixed documented order — declaration order for the fixed
descriptor shape, lexicographic for schema maps — with no insignificant whitespace. Because
`runtime` participates, the hash is per-port by design: it detects *this service's* redeploys,
and is never compared across ports.

### 2.3 Outbound registration

The **concept**, mirroring core-concepts.md §9's inbound handler discovery exactly: an application
hands the framework a list of (topic, version, request type, response type) records it *may send* —
no handler, since nothing here receives. This is what makes `consumes` (§2) a *hard-coded contract*
rather than an inference: the list is exactly as reliable as the registry that already makes
`topics` reliable, and for the identical reason — a port MUST NOT attempt to infer it by scanning
call sites, string literals, or any other form of static analysis over handler bodies, because that
degrades silently and unpredictably per language, and defeats the "identical across ports" promise
(§8) this whole document exists for.

- Explicit registration is a first-class path a port MUST support (core-concepts.md §9's same
  requirement, applied to outbound). Attribute/annotation sugar over it is an idiom, same as inbound.
- A registered outbound record needs no destination address, queue name, or topic ARN — those are
  transport/deployment configuration (transport-bindings.md), orthogonal to the *contract* this
  registers. A service can declare it consumes `payments:capture` while its actual SQS queue URL is
  injected at deploy time; the descriptor doesn't change between environments, only the wiring does.
- A send through an outbound client (`MessageSender`, core-concepts.md) to a topic **not** present in
  `consumes` is not a spec violation — Core does not require pre-declaration to send — but it MUST
  surface as `contract-drift` the first time a collector observes it (§4.2): the declared contract
  and the running system have diverged, and that divergence is exactly the signal this feed exists
  to raise, not to silently tolerate.
- A port that has not yet implemented outbound registration omits `consumes` and marks
  `degraded: ["outbound-registry"]` (§2) — this is a real, visible gap in that port's conformance,
  not a silent zero.

## 3. TraceEvent

One pipeline invocation as the mesh sees it — semantic (topic + Benzene status), not
transport-shaped.

```json
{
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7",
  "parentSpanId": "0af7651916cd43dd",
  "service": "orders",
  "instanceId": "orders-7f9c",
  "topic": "order:create",
  "topicVersion": "v2",
  "status": "validation-error",
  "exceptionType": "System.InvalidOperationException",
  "durationMs": 12.4,
  "startedAt": "2026-07-16T09:14:03.120Z",
  "correlationId": "abc-123"
}
```

- `traceId`/`spanId`/`parentSpanId` are the W3C Trace Context fields (32/16/16 lowercase hex).
  An inbound `traceparent` header (wire-contracts header conventions) joins the existing trace:
  its trace-id is adopted and its parent-id recorded. An absent or malformed header — wrong
  segment count or length, non-hex, or the all-zero ids the W3C spec defines as invalid — MUST
  yield a fresh trace-id and no parent: a bad caller header degrades correlation, never the
  invocation. Pinned by the `traceparent` section of `conformance/mesh-trace-cases.json`.
- Outbound propagation: a handler making a downstream Benzene call SHOULD forward
  `traceparent: 00-<traceId>-<spanId>-01` built from its own invocation's span. This is what lets a
  collector correlate an *observed* call with the declared edge it exercises (§4.2) — propagation
  feeds observation, not the graph itself (§4), which comes from `consumes` (§2) regardless of
  whether any call has ever propagated a trace at all.
- `status` is the Benzene status verbatim (wire-contracts.md §3); empty only when no downstream
  middleware produced a result (a wiring gap, reported as-is).
- `exceptionType` *(optional, additive 2026-07-25)* — when the invocation's failure originated in a
  thrown exception, the exception's language-native **type name** (e.g. a fully-qualified CLR or Java
  class name, a Go error type). Never the exception message, stack trace, or any payload-derived text —
  the type is a stable, non-sensitive discriminator (the same classification rule the health-check
  plane uses). Omitted for non-exception failures and by emitters that don't capture it; a collector
  MUST accept its absence.
- `correlationId` mirrors the `x-correlation-id` header when present.
- Coverage MUST be structural: because the router already converts a missing handler, a request
  conversion failure, and a handler panic/exception into results (core-concepts.md §5), every
  routed invocation yields exactly one TraceEvent. Pinned by the `invocations` section of
  `conformance/mesh-trace-cases.json`.

## 4. Collector topics

A collector is an ordinary Benzene service serving these topics over any envelope-capable
transport (transport-bindings.md):

| Topic | Body | Success payload |
|---|---|---|
| `benzene:mesh:register` | ServiceDescriptor (§2) | `{"accepted":1}` |
| `benzene:mesh:heartbeat` | Heartbeat (§5) | `{"accepted":1}` |
| `benzene:mesh:traces` | `{"events":[TraceEvent…]}` | `{"accepted":<count>}` |
| `benzene:mesh:issues` | IssueBatch (§4.1) | `{"accepted":<count>}` |

- `service` is REQUIRED on register, heartbeat, and issues → `bad-request` when missing. A
  `benzene:mesh:traces` or `benzene:mesh:issues` batch of any size, including empty, MUST be accepted.
- Re-registration replaces the previous registration wholesale, including the claim to provide
  *and* the claim to consume each topic — a redeploy that drops a topic from `topics` drops the
  provider edge with it, and a redeploy that drops a topic from `consumes` drops the consumer edge
  with it, the same rule applied symmetrically to both declared lists (§2).
- **The producer/consumer graph MUST be built from the latest registered `ServiceDescriptor` alone**
  — `topics` for providers, `consumes` for consumers (§2, §2.3). A collector MUST report this graph
  in full for a service that has registered but never sent or received a single message: the graph
  is the declared contract, not a summary of traffic. Trace parentage (§3) MUST NOT be used to admit
  an edge into this graph, add a consumer/provider the descriptor didn't declare, or remove one it
  did — its role is §4.2, entirely separate from graph membership.

Sender behavior (normative for ports): trace export MUST be asynchronous, non-blocking, and
lossy under backpressure — a full buffer drops events, a failed send drops the batch, and no
mesh feed may ever fail, slow, or block the invocation it observed.

Collector behavior is pinned by `conformance/mesh-collector-cases.json`. Query read models
(`benzene:mesh:query:*`) as implemented by the Go collector are deliberately not part of this contract
yet: they are one collector's read models, and join the spec if a second collector or
third-party view needs them pinned. The collector fixtures exercise them only as the observable
surface for asserting ingest/derivation behavior.

### 4.1 Issues (`benzene:mesh:issues`)

*Additive 2026-07. Optional on both sides (§6 rules apply); Go reference parity pending. A
collector that doesn't implement it is unaffected; a service that doesn't emit degrades to
today. This feed adds **zero** Cloud Service Profile requirements.*

The pipeline-native failure feed: where traces record *what happened*, an issue records *what is
wrong* — a deduplicated, classified failure signature emitted by the pipeline itself, which
uniquely holds the wire status and the thrown exception at the moment of failure.

```json
{
  "service": "orders",
  "issues": [
    {
      "fingerprint": "9f2c1a7e0d4b86513a9e2f70c4d1b8a2",
      "classification": "exception",
      "service": "orders",
      "topic": "order:create",
      "version": "v2",
      "transport": "sqs",
      "status": "service-unavailable",
      "exceptionType": "System.Net.Http.HttpRequestException",
      "count": 12,
      "firstSeen": "2026-07-25T09:14:03Z",
      "lastSeen": "2026-07-25T09:41:12Z",
      "exemplarTraceIds": ["4bf92f3577b34da6a3ce929d0e0e4736"],
      "resolutionHint": "deserialization"
    }
  ]
}
```

- **Batch-level `service` is REQUIRED** (→ `bad-request`), even though each issue also carries
  `service` (an aggregating relay may batch for several services): an **empty batch is the feed's
  liveness assertion** — "feed alive, nothing failing" — and must be attributable. Emitters
  SHOULD flush on an interval even when empty, so a collector can distinguish a quiet wired
  service from an unwired one; a collector MUST accept an empty batch.
- **`count` is a DELTA**: occurrences since the emitter's previous successful flush, never a
  cumulative total. Collectors merge by fingerprint: `count += delta`, `firstSeen = min`,
  `lastSeen = max`, exemplars keep the newest (≤3), other fields latest-wins. Delta semantics
  make merge restart-proof and need no instance identity on the wire; a dropped batch loses its
  delta — lossy by design, the same trade as `benzene:mesh:traces`.
- **`fingerprint` derivation is normative**: the lowercase hex of the first 16 bytes of SHA-256
  over the UTF-8 bytes of `service|topic|version|classification|discriminator` (pipe-joined),
  where `version` is the empty string when absent and `discriminator` is `exceptionType` when
  present, else `status`. `transport` is deliberately excluded — the same failure over two
  transports is one issue. Cross-language fingerprint equality holds only for non-exception
  classes (`exceptionType` is language-native); same-service equality across instances and
  restarts is the property that matters for merge. **Neither the problem-document `code` nor
  `type` (wire-contracts.md §1.3, §3.1) participates in the fingerprint** — both are open,
  per-error or per-response identifiers, and admitting either would explode issue cardinality and
  defeat the merge above.
- **`classification` is a closed vocabulary** — `exception`, `validation`, `config-wiring`,
  `dependency`, `contract-drift`, `unclassified` — assigned by normative precedence, evaluated
  in order against the invocation's Benzene status (wire-contracts.md §3) and captured exception
  type:
  1. `bad-request`, `validation-error` → **validation** (even when an exception type is present —
     a deserialization or argument exception is still a validation issue; the exception type
     remains the fingerprint discriminator);
  2. exception type present → **exception** (a thrown-and-converted failure classifies by its
     throw, not its mapped status);
  3. `not-found`, `unauthorized`, `forbidden`, `not-implemented`, or an **empty status** (§3: a
     wiring gap) → **config-wiring**;
  4. `service-unavailable`, `timeout`, `too-many-requests` → **dependency**;
  5. `unexpected-error` → **exception**;
  6. any other failing status → **unclassified** (an honest fallback beats a lying class).
  `contract-drift` is never produced by this table — it is reserved for catalog/heartbeat-derived
  issues (descriptor-hash mismatch, schema divergence) and the undeclared-edge case §4.2 defines
  (a trace names a topic absent from the caller's `consumes` or the handler's `topics`), filed in
  this same shape by a collector or reader, so emitter implementers should not hunt for its trigger.
- `exceptionType` is the language-native type name only — never a message, stack trace, payload,
  or header. `resolutionHint` is an optional key into a remediation catalog, never prose;
  registered keys so far: `no-handler` (routing genuinely matched no handler — distinguishes a
  wiring `not-found` from a handler-returned business `not-found`), `deserialization` (the request
  could not be read into the handler's request type). Unknown keys MUST be tolerated (readers
  fall back to classification-level guidance).
- Sender rules are `benzene:mesh:traces`' rules (§4): asynchronous, non-blocking, lossy, dedup at source
  (per-occurrence events MUST NOT be sent), and the feed may never fail, slow, or block the
  invocation it observed.

Pinned by `conformance/mesh-issue-cases.json` — required only for collectors claiming the issue
feed (see `conformance/README.md`).

### 4.2 Declared vs. observed — liveness and drift

The graph (§4) is declared and does not need traffic to exist. Traces still matter — they are the
*only* way to know whether a declared edge is actually being exercised, and the only way to notice
an edge nobody declared. Both are collector-derived read models over the same two inputs
(`ServiceDescriptor.consumes` and `TraceEvent` parentage), never a change to either input:

- **Unobserved** — a declared edge (in `topics` or `consumes`) with no corresponding trace parentage
  within the collector's retention window. This is a **decommission candidate**, not a fact: trace
  export is lossy by design (§4), so absence of evidence is not evidence of absence, only a prompt
  to go check. A collector MUST report *last observed at* (or its absence) per edge rather than
  collapsing it to a boolean, so a reader can judge staleness for itself.
- **Undeclared** — trace parentage between two services on a topic that is not present in the
  caller's registered `consumes` (or, symmetrically, a trace naming a topic absent from the
  handler's registered `topics`). This is **`contract-drift`** (§4.1's classification vocabulary,
  which already reserves this class for exactly this collector-derived case): the declared contract
  and the running system disagree, and a reader needs to know which one is stale — the descriptor
  the last deploy forgot to update, or a caller that shouldn't be doing what it's doing.

Neither signal is itself a topology edge. A view MAY render "declared, unobserved" and "observed,
undeclared" states distinctly from a confirmed (declared *and* observed) edge, but the graph (§4)
itself contains only what's declared, unconditionally.

## 5. Heartbeat

The health-check aggregate response (wire-contracts.md §5) reused byte-for-byte, wrapped with
identity:

```json
{
  "service": "orders",
  "instanceId": "orders-7f9c",
  "descriptorHash": "sha256:…",
  "sentAt": "2026-07-16T09:14:03Z",
  "health": { "isHealthy": true, "healthChecks": { "db": { "status": "ok", "type": "postgres" } } }
}
```

A heartbeat whose `descriptorHash` differs from the registered descriptor's hash means the
instance runs a contract the collector hasn't learned. The collector MUST surface the mismatch
(the Go collector reports per-instance `hashMatches`) rather than silently keeping stale topics.

## 6. Degradation (normative)

Every mesh feed — the descriptor endpoint, registration, heartbeats, traces — is independent and
optional, on both sides:

- **Service side**: an unprovisioned descriptor endpoint, an unreachable collector, a failing or
  absent exporter, or an absent registry each reduce the mesh and MUST NOT affect the service's
  own traffic in any way.
- **Collector side**: partial fleets MUST be accepted and rendered as reduced. Traces from a
  service that never registered present it as known-but-reduced (missing descriptor feed); a
  registered service with no traffic is a catalog entry with no stats; no heartbeats means
  unknown health. A missing feed MUST NOT fail ingestion or queries.

## 7. Conformance

Four fixture files in [conformance/](conformance/README.md) pin this document; their formats
and the canonical mesh handlers are documented there. A port that implements mesh MUST pass
`mesh-descriptor-cases.json` (now including `consumes`/outbound-registration cases, §2.3) and
`mesh-trace-cases.json`; a port that additionally implements a collector MUST pass
`mesh-collector-cases.json` (now including graph-from-descriptor cases — a registered service with
zero traffic reporting its full provider/consumer graph — and the declared-vs-observed cases of
§4.2), and a collector that additionally claims the §4.1 issue feed MUST pass
`mesh-issue-cases.json` (optional — a collector without it stays collector-conformant, and picking
up the undeclared-edge trigger of §4.2 is bundled with it, not a separate obligation). A port that
implements neither is unaffected at Core level — mesh is an optional module there, and the Core
spec creates no obligation to implement it, only the obligation to implement it *compatibly*.
Supporting the [Cloud Service Profile](cloud-service-profile.md), however, requires the
service-side feeds, so a port that wants its services to claim the profile implements §§1–3 and §5
and passes the two service-side fixture files.

A port already conformant under the pre-2026-08 revision (trace-derived consumer edges) is not
conformant under this one until it adds outbound registration (§2.3) and re-points its collector's
consumer-edge derivation at `consumes` (§4) — this is a breaking change to an existing MUST rule,
not an additive one; see the revision note at the top of this document.

## 8. Conformance language note

Per the repository's one design rule, everything in §§1–6 is a Benzene *concept* — wire shapes
and cross-process behavior. How a port derives its descriptor (attribute scanning vs explicit
registration), how a collector stores state, and what a view renders are *idioms* and stay out
of this document.

## 9. Relationship to the existing .NET mesh packages *(informative)*

The `Benzene.Mesh.*` packages implement a mesh visibility pipeline that predates this contract:
a human-maintained `mesh.json` registry, an aggregator that polls each service's OpenAPI `/spec`
and `/health` endpoints (or receives opportunistic `MeshServiceReport` self-reports), raw-spec
hashing for contract drift (`MeshHashing`, HMAC-SHA256), Tempo/Prometheus-derived
`topology.json` edges, and a static Mesh UI. The two designs solve the same problem from
opposite ends, and several of the .NET roadmap's own open gaps are exactly what this contract
provides:

| `Benzene.Mesh.*` today | This contract | Convergence |
|---|---|---|
| `mesh.json` registry, human-edited | catalog derived from `benzene:mesh:register` + heartbeats | registry remains a pull-mode bootstrap for unmeshed services; meshed services need no entry |
| `MeshServiceReport` (name, reportedAt, opaque OpenAPI `SpecJson`, health, error) | ServiceDescriptor (§2, topics + derived schemas) + Heartbeat (§5) | the self-report is register+heartbeat in one; the descriptor replaces the opaque spec for wire purposes — the OpenAPI artifact can remain as an enrichment |
| `MeshHashing` (HMAC-SHA256 of raw spec text) | `descriptorHash` (§2.2, SHA-256 of canonical descriptor JSON) | .NET adopts §2.2 on the wire; `MeshHashing` stays internal to its OpenAPI-artifact drift feature |
| `TopologyEdge` from Tempo/Prometheus (client/server + rates/latencies) | consumer/producer graph declared via `ServiceDescriptor` (§2, §4); TraceEvent parentage (§3) feeds observed usage and drift (§4.2), not the graph | the native trace feed yields observation with no external tracing stack; Tempo remains an optional additional rate/latency source layered onto the declared edges, never a substitute for them |
| `MeshSelfReportMiddleware` (opportunistic, throttled, never blocks) | trace middleware + heartbeat, same never-affect-the-service rule (§6) | the ethos is already shared; the shapes converge |
| aggregator + `manifest.json`/`services/*.json` + Mesh UI | a collector (§4) with its own read models and view | the aggregator becomes a conformant collector by also accepting the three ingest topics alongside its pull sources |
| known staleness gap (no `Stale` status) | heartbeats give last-seen; missing feeds are rendered as reduced (§6) | solved by adopting §5 |
| health: `HealthCheckResponse` | the same wire-contracts §5 shape, reused verbatim | already shared — no change |

Nothing in the existing packages needs to be discarded: pull-based aggregation, the OpenAPI
artifacts, Tempo topology, and the UI are collector-side idioms this contract deliberately does
not constrain. Conformance for the .NET port means adding the wire layer: descriptor derivation
(§2) with the reserved topic (§1), the trace feed (§3), and — for the aggregator — the ingest
topics (§4).
