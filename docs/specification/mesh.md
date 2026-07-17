# Mesh Contracts

**Status: DRAFT v0.1 — promoted from
[benzene-go](https://github.com/daniellepelley/benzene-go)'s `docs/design/mesh.md`. The .NET
implementation is the primary implementation of this document and covers the full contract:
`Benzene.Mesh.Wire` (descriptor, reserved topic, trace feed) and `Benzene.Mesh.Collector` (the
§4–§6 collector), together passing all three conformance fixture files via
`test/Benzene.Conformance.Test`. The Go port (its `mesh`/`meshd` packages) is a fully conforming
implementation — this contract was originally extracted from it — and the two have hosted each
other's services in live cross-language fleets, in both directions (see
`work/service-mesh-roadmap-1.0.md`'s 2026-07-16 updates). The pre-existing `Benzene.Mesh.*`
visibility packages (aggregator/UI/Tempo, developed independently against the roadmap) are
collector-side idiom this contract doesn't constrain; §9 maps them, and bridging the aggregator's
artifact pipeline to `Benzene.Mesh.Collector` is the natural integration follow-up.**

Benzene Mesh is the *application-level* mesh: every service self-describes (its topics, versions,
and payload schemas, derived from its handler registry), reports health, and emits one semantic
trace event per invocation — so a collector can render a live, cross-cloud fleet view (catalog,
health, stats, who-calls-whom) that is **derived from running code, never declared**. This
document specifies everything that crosses a process boundary to make that work identically
across language ports.

At Core level, mesh is optional, and so is each of its feeds; the normative degradation rule of
§6 applies to every section here. A service claiming the
[Cloud Service Profile](cloud-service-profile.md) MUST provision the service-side feeds (its
R6) — for such a service, §6 governs runtime degradation, not whether the feeds exist.

## 1. The reserved `mesh` topic

A mesh-enabled service MUST intercept the reserved topic id `mesh` (plus any app-chosen aliases)
the same way health-check interception works (core-concepts.md §5): interception is by topic id
alone, ignoring version; any other topic passes through unchanged. The response is status `Ok`
with the ServiceDescriptor (§2) as payload.

Provisioning this endpoint is a deployment decision: a service that must not expose it (e.g.
pending a security review) simply does not install the interception, and every other mesh feed
keeps working (§6).

## 2. ServiceDescriptor

The service's self-description, derived at startup from its handler registry — never
hand-maintained. Also the body of a `mesh:register` message (§4).

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
- `topics` — every registered topic, sorted by id then version. Explicit registration
  (core-concepts.md §9) is what makes the registry the complete truth of what the service serves;
  this field is its projection.
- `degraded` — names the feeds that were unavailable when the descriptor was built (currently
  only `"registry"`), so a reduced descriptor is distinguishable from a service with no topics.
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

Two ports registering equivalent canonical types MUST produce identical `topics` entries — this
is pinned by `conformance/mesh-descriptor-cases.json`.

### 2.2 descriptorHash

`"sha256:" + lowercase-hex(sha256(canonicalJSON(descriptor)))`, where the hashed descriptor has
`instanceId`, `degraded`, `profile`, and `descriptorHash` itself blanked. The hash covers the *contract*
(identity, placement, topics, schemas):

- Two instances of the same build MUST hash identically (`instanceId` excluded).
- The hash MUST change when the contract changes (topics, schemas, `serviceVersion`, placement).

Canonical JSON: object members in a fixed documented order — declaration order for the fixed
descriptor shape, lexicographic for schema maps — with no insignificant whitespace. Because
`runtime` participates, the hash is per-port by design: it detects *this service's* redeploys,
and is never compared across ports.

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
  "status": "ValidationError",
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
  `traceparent: 00-<traceId>-<spanId>-01` built from its own invocation's span. This is what
  lets a collector derive consumer edges (§4) from parentage.
- `status` is the Benzene status verbatim (wire-contracts.md §3); empty only when no downstream
  middleware produced a result (a wiring gap, reported as-is).
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
| `mesh:register` | ServiceDescriptor (§2) | `{"accepted":1}` |
| `mesh:heartbeat` | Heartbeat (§5) | `{"accepted":1}` |
| `mesh:traces` | `{"events":[TraceEvent…]}` | `{"accepted":<count>}` |

- `service` is REQUIRED on register and heartbeat → `BadRequest` when missing. A `mesh:traces`
  batch of any size, including empty, MUST be accepted.
- Re-registration replaces the previous registration wholesale, including the claim to provide
  each topic — a redeploy that drops a topic drops the provider edge with it.
- Consumer edges MUST be derived from trace parentage (an event whose parent span belongs to a
  different service makes that service a consumer of the event's topic), never declared.

Sender behavior (normative for ports): trace export MUST be asynchronous, non-blocking, and
lossy under backpressure — a full buffer drops events, a failed send drops the batch, and no
mesh feed may ever fail, slow, or block the invocation it observed.

Collector behavior is pinned by `conformance/mesh-collector-cases.json`. Query read models
(`mesh:query:*`) as implemented by the Go collector are deliberately not part of this contract
yet: they are one collector's read models, and join the spec if a second collector or
third-party view needs them pinned. The collector fixtures exercise them only as the observable
surface for asserting ingest/derivation behavior.

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

Three fixture files in [conformance/](conformance/README.md) pin this document; their formats
and the canonical mesh handlers are documented there. A port that implements mesh MUST pass
`mesh-descriptor-cases.json` and `mesh-trace-cases.json`; a port that additionally implements a
collector MUST pass `mesh-collector-cases.json`. A port that implements neither is unaffected
at Core level — mesh is an optional module there, and the Core spec creates no obligation to
implement it, only the obligation to implement it *compatibly*. Supporting the
[Cloud Service Profile](cloud-service-profile.md), however, requires the service-side feeds, so
a port that wants its services to claim the profile implements §§1–3 and §5 and passes the two
service-side fixture files.

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
| `mesh.json` registry, human-edited | catalog derived from `mesh:register` + heartbeats | registry remains a pull-mode bootstrap for unmeshed services; meshed services need no entry |
| `MeshServiceReport` (name, reportedAt, opaque OpenAPI `SpecJson`, health, error) | ServiceDescriptor (§2, topics + derived schemas) + Heartbeat (§5) | the self-report is register+heartbeat in one; the descriptor replaces the opaque spec for wire purposes — the OpenAPI artifact can remain as an enrichment |
| `MeshHashing` (HMAC-SHA256 of raw spec text) | `descriptorHash` (§2.2, SHA-256 of canonical descriptor JSON) | .NET adopts §2.2 on the wire; `MeshHashing` stays internal to its OpenAPI-artifact drift feature |
| `TopologyEdge` from Tempo/Prometheus (client/server + rates/latencies) | consumer edges derived from TraceEvent parentage (§3–4) | the native trace feed yields edges with no external tracing stack; Tempo remains an optional additional `TopologyEdgeSource` |
| `MeshSelfReportMiddleware` (opportunistic, throttled, never blocks) | trace middleware + heartbeat, same never-affect-the-service rule (§6) | the ethos is already shared; the shapes converge |
| aggregator + `manifest.json`/`services/*.json` + Mesh UI | a collector (§4) with its own read models and view | the aggregator becomes a conformant collector by also accepting the three ingest topics alongside its pull sources |
| known staleness gap (no `Stale` status) | heartbeats give last-seen; missing feeds are rendered as reduced (§6) | solved by adopting §5 |
| health: `HealthCheckResponse` | the same wire-contracts §5 shape, reused verbatim | already shared — no change |

Nothing in the existing packages needs to be discarded: pull-based aggregation, the OpenAPI
artifacts, Tempo topology, and the UI are collector-side idioms this contract deliberately does
not constrain. Conformance for the .NET port means adding the wire layer: descriptor derivation
(§2) with the reserved topic (§1), the trace feed (§3), and — for the aggregator — the ingest
topics (§4). The concrete plan lives in `work/service-mesh-roadmap-1.0.md`.
