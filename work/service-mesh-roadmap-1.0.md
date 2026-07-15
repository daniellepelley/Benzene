# Benzene Service Mesh Visibility — Rough Roadmap & Design (2026-07-14)

**Status:** IN PROGRESS.
> **2026-07-14 implementation update:** Phase 0 (§4.1, structured `HealthCheckDependency`) is
> done and on `main`. Phase 1 is **partially** done, as `Benzene.Mesh.Contracts` +
> `Benzene.Mesh.Aggregator`, with some deliberate deviations from this document's original
> sketch, flagged here rather than silently diverging:
> - The aggregator polls each registered service's live `/spec` + `/health` endpoint (per §4.2's
>   "simpler" recommendation) and publishes to **local disk** only (`IMeshArtifactStore` port +
>   `FileSystemMeshArtifactStore`) — no S3/Azure Blob adapter yet, though the interface is
>   designed so one is a drop-in addition, not a rewrite.
> - `services/{name}.json` stores the raw spec JSON **verbatim as an opaque string**, not
>   deserialized into `EventServiceDocument` — §7.2's proposed shape assumed structural parsing,
>   but contract-drift detection only needs a hash of the raw text, so `Benzene.Mesh.Contracts`
>   takes no dependency on `Benzene.Schema.OpenApi` (a deviation from §8's package table).
> - **Contract drift detection (the first half of Phase 2) is already done**, not just Phase 1 —
>   the aggregator hashes each fetch and compares against the previous run's hash
>   (`MeshHashing`, deliberately reimplementing rather than depending on
>   `Benzene.CodeGen.Core.CodeGenHelpers.GenerateHash`'s identical algorithm, to keep a runtime
>   aggregator's dependency graph clean — a cross-check test keeps the two in sync).
> - `topology.json`/edge derivation is **not built** — deliberately deferred, since the
>   "structural edges from generated `CodeGen.Client`s" idea in §4.6 isn't observable at runtime
>   by an HTTP-polling aggregator (it's a compile-time/source fact); a workable alternative
>   (matching `HealthCheckDependency` entries against other registered services' identifiers)
>   is a real design question of its own, not yet done.
> - Per an explicit "dogfood Benzene" steer: the aggregator is exposed as a genuine Benzene
>   message handler (`[Message("mesh:aggregate")]`/`[HttpEndpoint("POST", "/mesh/aggregate")]`),
>   not a bare service class or standalone CLI — a design choice this document didn't
>   anticipate.
> - **The Mesh UI is now built** (`Benzene.Mesh.Ui`), closing "Phase 2: surfaced in the UI." It
>   mirrors `Benzene.Spec.Ui`'s exact shape (`MeshUiPage`/`MeshUiMiddleware`/`MeshUiExtensions`,
>   same embedded-HTML/theming/boot-precedence pattern), but its *primary* deployment target is a
>   plain static file host serving `mesh-ui.html` alongside the aggregator's generated
>   `manifest.json`/`services/*.json` — unlike Spec.Ui, there's usually no single live service to
>   serve it from, since the aggregator's output is meant to be published somewhere and read from
>   wherever it lands. The `UseMeshUi` middleware exists as a secondary convenience (local demo,
>   or an aggregator host self-serving its own dashboard), not the main path. Scope: service
>   catalog (health status + contract-drift badges), expandable per-service health-check detail
>   (lazy `fetch` of `services/{name}.json`), search/filter, theme toggle, a "load a different
>   manifest" dialog, and a best-effort link out to each service's own `/spec-ui` derived from its
>   `specUrl`. **No topology/graph rendering** — `topology.json` doesn't exist yet (see above);
>   this is catalog/health/drift only, matching the roadmap's own observation that "Phases 1-2
>   alone already deliver most of what was asked... the smallest useful v1."
>
> **Phase 0, Phase 1's data-pipeline half, and Phase 2 are now complete** by this reading. Phase
> 1's `topology.json`/edge-derivation gap and Phase 3 (live Tempo trace overlay) remain open, as
> does Phase 4 (field-level contract compatibility) and Phase 5 (polish).
>
> **2026-07-14 Phase 3 update:** `Benzene.Mesh.Tracing.Tempo` is built - the PromQL/Prometheus
> adapter §4.6.1 designed, publishing `topology.json` (§7.3) with tempo-sourced edges. Also
> dogfooded (`[Message("mesh:topology")]`/`[HttpEndpoint("POST", "/mesh/topology")]`, same shape
> as `MeshAggregateMessageHandler`). Notes and deviations:
> - `TopologyEdge`/`MeshTopology` live in `Benzene.Mesh.Contracts` (shared shapes, matching §8's
>   original intent), but the new package also references `Benzene.Mesh.Aggregator` for
>   `IMeshArtifactStore` - a deliberate deviation from §8's package table (which had this package
>   depending only on `Contracts`), since that port ended up living in `Aggregator` during Phase 1a
>   rather than `Contracts`, and relocating it now purely for dependency-graph tidiness wasn't
>   judged worth the churn to already-shipped code.
> - `AddTempoTopology(options)` deliberately does not register its own `IMeshArtifactStore` -
>   it requires `AddMeshAggregator(...)` to already be registered in the same container, so
>   `topology.json` publishes alongside `manifest.json`/`services/*.json` in the same directory
>   rather than risking two artifact stores pointed at different places.
> - Adds p50/p99 latency alongside the §7.3 sample's `p95LatencyMs` - free from the same
>   histogram query, judged a worthwhile enrichment rather than a shape change.
> - Error rate is computed client-side (`failedPerMinute / requestsPerMinute`, not a 6th PromQL
>   query), sidestepping Prometheus's own NaN-on-zero-division semantics.
> - **Live verification against a real Tempo + Prometheus stack was attempted but blocked by this
>   environment's own network egress policy** (Docker Hub image pulls and direct GitHub release
>   downloads both returned `403`/`Forbidden` from the sandbox's proxy - not a Benzene-side
>   problem, and not something to route around). So the original open caveat - "exact label/metric
>   names should be confirmed against the specific Tempo/Grafana version in use" - **remains open**,
>   same as before this pass. What ships instead is thorough mocked-HTTP test coverage against the
>   documented Prometheus API response shape and the 3 named metrics, which is real coverage of
>   this package's own parsing/joining logic, just not proof that a live Tempo's actual metric
>   names/labels match what's assumed here. Worth a real follow-up the next time this is picked up
>   somewhere with registry access.
> - **No Mesh UI rendering of `topology.json`** in this pass - deliberate, matching how Phase 2
>   itself deferred all graph/topology rendering; the data is real and published, not yet surfaced
>   visually.
> - The checked-in `examples/Mesh/` example was **not** extended with real inter-service tracing
>   or a bundled Tempo+Prometheus stack - the 3 demo services don't currently call each other, so
>   there's no real traffic to generate a topology from without first building that (a separable,
>   larger follow-up).
>
> **Phase 3's adapter + data pipeline is built and thoroughly unit-tested**, but not confirmed
> against a real Tempo instance (environment-blocked, see above) - a real, not just cosmetic,
> caveat worth resolving before leaning on this in production. Phase 1's `topology.json`
> *structural*-edge gap remains open (Tempo covers the *observed* half only), as does Phase 4
> (field-level contract compatibility), Phase 5 (polish), and Mesh UI topology rendering (not
> phased explicitly, but a natural next step once structural edges exist too).
>
> **2026-07-15 examples/UI update:** Closes the two gaps the previous update flagged as open at
> the demo/UI layer (does **not** resolve the live-Tempo-verification caveat itself - see below).
> - `examples/Mesh` now demonstrates the Tempo integration end to end via a bundled fake
>   Prometheus endpoint (`Benzene.Examples.Mesh.Aggregator/FakePrometheus.cs`,
>   `/fake-prometheus/api/v1/query`) rather than a real Tempo+Prometheus stack - deliberate, since
>   standing up real infrastructure hits the same network-egress-blocked sandbox problem noted in
>   the Phase 3 update above. `./run.sh` remains fully self-contained (no Docker, no external
>   network calls). This is mocked-HTTP coverage of the same shape at the example layer, not proof
>   against a live Tempo instance - the "verify against a real Tempo/Prometheus deployment" caveat
>   from the Phase 3 update **still stands, unchanged**.
> - `Benzene.Mesh.Ui`'s `mesh-ui.html` now renders `topology.json` as a sortable
>   client/server/source/req-per-min/error-rate/p50-p95-p99 table, fetched via the same
>   `resolveUrl()` precedence as `services/{name}.json` and hidden gracefully (no error state) when
>   `topology.json` is absent - closes the "No Mesh UI rendering of `topology.json`" gap the Phase
>   3 update flagged. Deliberately scoped as a table, not a graph: full node-link topology
>   visualization remains open, now tracked as a `mesh-product-owner` priority (see
>   `.claude/PRODUCT_OWNERS.md`).
> - A new `mesh-product-owner` Claude agent now owns this feature family
>   (`.claude/agents/mesh-product-owner.md`, registered in `.claude/PRODUCT_OWNERS.md`) and tracks
>   the real remaining phases: live Tempo verification, structural edge derivation, full graph
>   visualization, Phase 4 (field-level contract compatibility), and Phase 5 (polish).
**Owner:** `mesh-product-owner` (`.claude/agents/mesh-product-owner.md`)
**Purpose:** Scope a cross-service "service mesh" visibility layer for Benzene solutions:
a catalog of every service (topics, contracts, health/dependencies), a topology view of who
talks to whom, and a bridge into real trace data — built on top of Benzene's existing
per-service spec/health/tracing primitives rather than duplicating them.

---

## 1. The ask, restated

A Benzene *solution* is many independently-deployed services (Lambdas, Azure Functions,
Kubernetes pods, ASP.NET Core hosts — any mix), each internally organized as topics/message
handlers with a per-service spec (`UseSpec`) and a per-service Spec UI. Today there is no
picture that spans *across* services. The ask is a layer that gives:

1. A catalog of every service in the solution and the topics/contracts each one exposes.
2. A topology/mesh view — which services call which, whether those calls succeed, and how
   long they take.
3. Contract testing across that topology (request/response schema compatibility between a
   caller's expectation and a callee's actual current contract).
4. Health, enriched with *what* each service depends on externally (a specific queue, a
   specific database, a specific downstream service) — not just pass/fail.
5. A backend that serves this up as JSON, and a frontend that renders it — with a specific
   preferred shape: an aggregator pulls data from each service and lands it as files in blob
   storage; the UI is a static thing that reads those files; live trace data is pulled
   separately from whatever OpenTelemetry-compatible backend the solution already exports to.

## 2. What Benzene already has (verified against current `main`, not assumed)

This matters because most of the *primitives* already exist — the gap is almost entirely at
the cross-service aggregation layer, not at the per-service data-generation layer.

| Building block | State | Detail |
|---|---|---|
| Per-service topic/schema catalog | **Exists** | `UseSpec` (`Benzene.Schema.OpenApi`) publishes `EventServiceDocument` — topics, request/response JSON schema, per-service `Name`/`Description`/`Version` — in `openapi`, `asyncapi`, or Benzene-native format, fetchable at runtime on any transport. |
| Per-service spec UI | **Exists, single-service only** | `Benzene.Spec.Ui` (merged today) renders one spec URL as a static, self-contained HTML page. No multi-service/catalog notion — this is the per-service "detail page" our mesh UI would link out to, not the catalog itself. |
| Contract-drift detection | **Exists, coarse-grained** | `Benzene.CodeGen.Client` bakes a whole-document hash into generated clients; `ClientHealthCheckProcessor`/`ClientHashMatch` compares it against the live service's current hash and flags a `"warning"` health status on any drift. Real signal, but "something changed" not "field X removed from schema Y." |
| Health checks | **Exists, unstructured** | `IHealthCheck`/`IHealthCheckResult` give pass/fail + a loose `Type` string + a free-form `Data` dictionary. Concrete checks (`SqsHealthCheck`, `DatabaseHealthCheck`, `HttpPingHealthCheck`) stuff resource identity into `Data` inconsistently — no structured "this depends on {kind, name, criticality}" field. |
| Tracing spans | **Exists, local only** | `AddDiagnostics()` auto-wraps every middleware call in an `Activity` tagged `benzene.transport`/`benzene.topic`/`benzene.handler`/`benzene.version`. `UseW3CTraceContext()` parses inbound `traceparent` (HTTP transports only); `WithW3CTraceContext()` stamps it on outbound client calls. So service-A-calls-service-B trace continuity already works end-to-end for HTTP, if both ends opt in. |
| Trace export | **Doesn't ship one** | `Benzene.OpenTelemetry` only registers the `Benzene` `ActivitySource`/`Meter` with the OTel SDK's provider builders — exporting to any actual backend (Tempo, Jaeger, X-Ray, Datadog, etc.) is left to the consuming app's own OTel config. |
| Static artifact export / blob publish | **Doesn't exist** | No code anywhere writes generated artifacts to S3/Azure Blob. The nearest thing is an unbuilt design idea (`benzene spec export` CLI) noted in an earlier design review, never implemented. |
| Prior scoping of this exact feature | **None found** | Grepped all of `work/*.md` and `docs/*.md` — no service-catalog/topology/registry concept exists anywhere prior to this document. Genuinely net-new territory. |

## 3. Proposed architecture

```
 ┌─────────────┐   /spec?type=benzene   ┌──────────────┐
 │  Service A  │ ─────────────────────▶ │              │
 │ (Lambda)    │   /health              │              │
 ├─────────────┤ ─────────────────────▶ │  Aggregator  │
 │  Service B  │   /spec, /health       │  (Benzene    │──▶ Blob storage (S3/Azure Blob)
 │ (Function)  │ ─────────────────────▶ │  app itself) │      manifest.json
 ├─────────────┤                        │              │      services/{name}.json
 │  Service C  │ ─────────────────────▶ │              │      topology.json
 │ (K8s pod)   │                        └──────┬───────┘
 └─────────────┘                               │
                                                │ optional: query service-graph API
                                                ▼
                                   OTel-compatible backend
                                   (Tempo / X-Ray / Jaeger / vendor)
                                                │
                                                ▼
                                   ┌─────────────────────┐
                                   │   Mesh UI (static)   │  reads only from blob storage
                                   │  topology graph,      │  (never calls services directly —
                                   │  per-service detail,   │   same "static file" pattern the
                                   │  health rollup,         │   user described)
                                   │  contract-drift flags    │
                                   └─────────────────────┘
```

The key design choice this makes (matching what was described): **the "backend API" is just
static JSON in blob storage**, refreshed on a schedule or on deploy by the aggregator. No
always-on backend service is required for the catalog/health/contract data. Live trace data
is the one piece that's genuinely dynamic, so it's fetched separately, on UI load, directly
from whatever OTel backend the solution already has — Benzene doesn't own or replace that.

## 4. New/extended pieces, in the order they'd need to be built

### 4.1 Structured health-check dependency metadata (foundational, low risk)
Add an additive `Dependencies` field to `IHealthCheckResult`/`HealthCheckResult` — a small
list of `{ Kind, Name, Criticality }` (e.g. `Kind: "Sqs", Name: "orders-queue"`). Backfill
it into the existing AWS/EF/HTTP health checks. Purely additive — no breaking change, and it
directly produces the "what does this service depend on externally" data the mesh view needs.
Also worth fixing while touching this: `HealthCheckMessageHandler` in `Benzene.HealthChecks`
is currently dead/commented-out code that should either be deleted or become the source of
this dependency data (needs a decision — see §6).

### 4.2 Static spec+health export
Extend the existing runtime `UseSpec` endpoint story with a way to produce the same
`EventServiceDocument` + health snapshot *without* a live HTTP round-trip per service — either
a small CLI (`benzene mesh export`) that loads a service's assembly headlessly and dumps spec
+ health-check-metadata to a file, or (simpler, reuses more existing code) the aggregator just
calls each service's existing `/spec` and `/health` endpoints directly. Recommend starting
with the simpler "call existing endpoints" approach and only building the offline-export CLI
if there's a real need to catalog services that can't be safely health-checked from outside
(e.g. a Lambda with no public URL) — that's a §6 open question, not a given.

### 4.3 Aggregator
A small worker (could itself be a Benzene app — dogfooding the framework, or a plain console
tool) that: reads a service registry (see §4.4), calls each service's `/spec` + `/health`,
assembles a `manifest.json` (service list + summary) and one `services/{name}.json` per
service, and uploads them to blob storage. Runs on a schedule (e.g. every N minutes) or is
triggered post-deploy.

### 4.4 Service registry
The aggregator needs to know what services exist and where. Simplest viable v1: a checked-in
config file (`mesh.json` — service name → spec/health URLs, maybe owning team). Fancier
options (cloud-API discovery, self-registration on startup) are real but add real complexity
and dependency surface — recommend deferring past v1 (see §6).

### 4.5 Contract testing
Two tiers, buildable independently:
- **Drift detection (mostly exists already)** — generalize the existing `ClientHashMatch`
  whole-document-hash check so it isn't tied to a generated client; the aggregator can hash
  each service's current spec and diff against the previous snapshot in blob storage to flag
  "this service's contract changed since last publish."
- **Structural compatibility (net-new, bigger)** — an actual per-topic, per-field schema diff
  between what a caller expects (e.g. a generated client's baked-in schema, or a declared
  dependency) and what the callee currently serves, so a breaking removal/type-change surfaces
  as "topic `orders:create`, field `discountCode` removed" rather than just "something
  changed." This is the same shape of problem as a JSON-Schema-compatibility checker; worth
  checking whether the schema-compatibility engine referenced in earlier design reviews
  (`Benzene.Schema.OpenApi/Compatibility`) already covers this before building it from scratch
  — flagged for verification, not confirmed in this pass.

### 4.6 Tracing/topology
Two distinct sources of "who calls whom," which should be layered, not chosen between:
- **Structural edges (cheap, always available):** derive them from which services generate a
  `Benzene.CodeGen.Client` against which other service's spec — a "designed" call graph with
  no runtime dependency. The aggregator can compute this today from the spec data alone.
- **Observed edges (real traffic — Tempo, chosen 2026-07-14):** see below. Since the
  `benzene.transport`/`benzene.topic` tags are already on every span (§2), no new Benzene-side
  span tagging is required to make this work — the existing `AddDiagnostics()` instrumentation
  is already sufficient input.

#### 4.6.1 Why Tempo needs a different integration shape than "call a graph API"
Grafana Tempo does not expose a standalone "give me the service graph" REST endpoint the way
this design initially assumed a backend might. Tempo's **metrics-generator**, when its
`service-graphs` processor is enabled, derives *Prometheus metrics* from spans as they're
ingested and remote-writes them to a Prometheus-compatible store (Prometheus, Mimir, or
Grafana Cloud Metrics) — the metrics Grafana's own "Service Graph" panel queries are, by
convention:
- `traces_service_graph_request_total{client="...", server="..."}` — call count per edge
- `traces_service_graph_request_failed_total{client="...", server="..."}` — failed-call count
- `traces_service_graph_request_server_seconds_bucket{...}` /
  `traces_service_graph_request_client_seconds_bucket{...}` — latency histograms per edge

(Exact label/metric names should be confirmed against the specific Tempo/Grafana version in
use before implementation — this is the documented convention as of Tempo's metrics-generator
feature, not verified against a live instance in this pass.)

**Design implication:** the Benzene Tempo adapter is a PromQL client, not a Tempo-API client.
It queries these rate/histogram metrics from whatever Prometheus-compatible endpoint Tempo (or
the collector in front of it) remote-writes to, for a given time window, and converts the
result into `{client, server, requestsPerMin, errorRate, p50/p95/p99LatencyMs}` edges — which
the aggregator merges onto the structural graph from §4.6 by matching `client`/`server` label
values to Benzene service names (this requires the span's/metric's service-name label to
match the name used in the spec/registry — worth standardizing on `IApplicationInfo.Name` as
that join key). Tempo's own trace-search API (`/api/search`, `/api/traces/{traceID}`) is a
secondary, optional feature: "click an edge → see 3 example traces" drill-down, not required
for the graph itself.

This also means Phase 3 has a real prerequisite beyond Benzene: the solution's Tempo
deployment must have the metrics-generator's `service-graphs` processor enabled and a
Prometheus-compatible remote-write target configured — that's operator/infra setup, not
something Benzene's adapter package can do for them. Worth stating explicitly in this doc's
eventual "prerequisites" section for whoever picks this phase up.

### 4.7 Mesh UI
A new static, self-contained page mirroring `Benzene.Spec.Ui`'s existing pattern (inline
CSS/JS, no CDN calls, embeddable) but at the catalog level: reads `manifest.json` +
`topology.json` from blob storage, renders a service graph (nodes = services, edges = calls,
colored/weighted by health + observed error rate + latency if trace data is present), and
links each node out to that service's own `/spec-ui` page for topic-level detail.

## 5. Phasing (rough)

| Phase | Delivers | Depends on |
|---|---|---|
| 0 | Structured health dependency metadata (§4.1) | nothing — pure Benzene source change |
| 1 | Aggregator + blob publish + static catalog UI, structural topology only (no live traces yet) | 0, service registry config (§4.4) |
| 2 | Contract drift detection generalized + surfaced in the UI | 1 |
| 3 | Live trace integration — one OTel backend's service-graph API, overlaid on the topology | 1, and a decision on which backend (§6) |
| 4 | Structural (field-level) contract compatibility checking, ideally as a CI gate | 2, and verifying whether `Compatibility` engine already covers this |
| 5 | Polish: health rollup at the mesh level, historical trend storage, alerting | 1-4 |

Phases 1-2 alone already deliver most of what was asked (catalog + topology + health +
coarse contract drift) without touching a live trace backend at all — recommend treating that
as the smallest useful v1, with 3-4 as a clearly separable second milestone.

## 6. Open questions

1. ~~**Which OTel backend to integrate first for live trace/topology data?**~~ **Resolved
   2026-07-14: Grafana Tempo.** See §4.6 below for what this specifically means for the
   adapter design — Tempo's service-graph data is not served as its own REST API, so the
   adapter queries Prometheus-compatible metrics, not Tempo's trace-query API directly.
2. **Service registry mechanism** — recommend a checked-in config file for v1 (simplest,
   matches Benzene's "convention over magic infrastructure" bias) rather than building
   self-registration or cloud-API discovery up front. See §8.4 for a concrete proposed shape.
3. **Package naming/placement** — recommend a new `Benzene.Mesh.*` family, detailed in §9,
   separate from `Benzene.HealthChecks*`/`Benzene.Diagnostics`, since this is a cross-service
   concern rather than a per-service one.
4. **Does the aggregator need an offline/headless export path (§4.2)**, or is "call each
   service's existing live `/spec` + `/health` endpoint" sufficient? Depends on whether every
   service in scope is reachable over HTTP from wherever the aggregator runs. Still open —
   recommend starting with the live-endpoint approach and only building headless export if a
   real service turns up that isn't reachable that way.
5. **How much of "contract testing" should gate CI** (phase 4) vs. be purely informational in
   the UI (phases 1-2)? Gating requires deciding what counts as a breaking vs. safe schema
   change, which is a product decision as much as a technical one. Still open.

## 7. Data shapes (concrete proposal)

All artifacts are plain JSON, written by the aggregator to blob storage, read only (never
written) by the UI. Shapes below are a first-pass proposal — field names/nesting should be
revisited once `Benzene.Mesh.Contracts` is actually implemented, but the semantics shouldn't
change much.

### 7.1 `manifest.json` — top-level index, one per solution
```json
{
  "generatedAtUtc": "2026-07-14T12:00:00Z",
  "solutionName": "acme-orders-platform",
  "services": [
    {
      "name": "orders-api",
      "version": "1.4.2",
      "transport": "AwsLambda",
      "specUrl": "https://.../spec?type=benzene",
      "healthUrl": "https://.../health",
      "detailFile": "services/orders-api.json",
      "status": "healthy",
      "contractDrift": false
    }
  ]
}
```
`status` and `contractDrift` are denormalized summaries of the corresponding `services/*.json`
file, purely so the catalog/topology overview page doesn't need to fetch every per-service
file just to render a health-colored node.

### 7.2 `services/{name}.json` — full per-service snapshot
```json
{
  "name": "orders-api",
  "fetchedAtUtc": "2026-07-14T12:00:00Z",
  "spec": { "...": "the EventServiceDocument as published by UseSpec, verbatim" },
  "specHash": "sha256:...",
  "previousSpecHash": "sha256:...",
  "contractDrift": false,
  "health": {
    "status": "healthy",
    "checks": [
      {
        "name": "orders-queue",
        "type": "Sqs",
        "status": "healthy",
        "dependencies": [
          { "kind": "Queue", "name": "orders-queue", "criticality": "required" }
        ],
        "data": { "queueUrl": "https://sqs..." }
      }
    ]
  }
}
```
`dependencies` is the new structured field from §4.1 — additive to the existing free-form
`data` dictionary, not a replacement for it.

### 7.3 `topology.json` — cross-service edges
```json
{
  "generatedAtUtc": "2026-07-14T12:00:00Z",
  "edges": [
    {
      "client": "orders-api",
      "server": "payments-api",
      "source": "structural",
      "requestsPerMin": null,
      "errorRate": null,
      "p95LatencyMs": null
    },
    {
      "client": "orders-api",
      "server": "payments-api",
      "source": "tempo",
      "requestsPerMin": 42.3,
      "errorRate": 0.004,
      "p95LatencyMs": 118
    }
  ]
}
```
Structural and observed edges for the same `client`/`server` pair are kept as separate entries
(not merged into one) so the UI can show "designed to call" vs. "actually calling, and how
it's performing" distinctly — an edge that's structural-only but has no matching observed edge
is itself a useful signal (dead code path, or traffic just hasn't happened in the query window).

### 7.4 `mesh.json` — service registry (checked-in config, not generated)
```json
{
  "services": [
    {
      "name": "orders-api",
      "specUrl": "https://.../spec?type=benzene",
      "healthUrl": "https://.../health",
      "owningTeam": "orders"
    }
  ],
  "tempo": {
    "prometheusUrl": "https://prometheus.internal/api/v1/query_range",
    "serviceNameLabel": "server"
  }
}
```
This is the one artifact a human edits directly (per §6 item 2's recommendation); everything
else is generated.

## 8. Proposed package layout

| Package | Responsibility | Depends on |
|---|---|---|
| `Benzene.Mesh.Contracts` | Shared data-shape types (§7) + JSON (de)serialization — referenced by both the aggregator and anything that needs to read its output | `Benzene.Schema.OpenApi` (for `EventServiceDocument`), `Benzene.HealthChecks.Core` (for the health result shape) |
| `Benzene.Mesh.Aggregator` | Reads `mesh.json`, calls each service's `/spec` + `/health`, computes structural edges + spec-hash drift, writes `manifest.json`/`services/*.json`/`topology.json` to blob storage | `Benzene.Mesh.Contracts`, a blob-storage client (S3 or Azure Blob — whichever the solution uses; not both bundled) |
| `Benzene.Mesh.Tracing.Tempo` | The PromQL adapter from §4.6.1 — queries the service-graph metrics, converts to `topology.json` edges with `source: "tempo"`, called by the aggregator as an optional extra step | `Benzene.Mesh.Contracts` |
| `Benzene.Mesh.Ui` | Static, self-contained HTML/JS page (mirrors `Benzene.Spec.Ui`'s pattern) reading the blob-storage JSON and rendering the catalog/topology/health views, linking out to each service's own `/spec-ui` | none (build-time only; ships as an embedded resource like `Benzene.Spec.Ui` does) |

`Benzene.Mesh.Aggregator` is deliberately the only piece with a network/cloud-SDK dependency;
`Contracts` and `Ui` stay portable, consistent with the rest of Benzene's dependency
discipline (thin adapters, transport-agnostic core).

## 9. Explicitly out of scope for this document

- Building a general-purpose distributed tracing backend — Benzene should integrate with one,
  not become one.
- Auto-remediation / alerting workflows — flagged as phase 5 polish at most.
- Any UI framework decision beyond "matches the existing static-HTML `Spec.Ui` pattern" — not
  designed here.
