# Benzene Service Mesh Visibility — Rough Roadmap & Design (2026-07-14)

**Status:** IN PROGRESS.
> **2026-07-16 design-principles update:** the "opinionated but optional" strategy is now spec:
> `docs/specification/design-principles.md` records the adoption ladder (message handlers are the
> steer but optional, like controllers in ASP.NET - middleware-only and in-process pipelines are
> first-class), the capability matrix (what needs handlers vs. what works without, with mesh §6
> degradation as the worked example), the extension-point catalog (every wire convention
> overridable on BOTH producer and consumer sides - SQS's `topic` message attribute with its
> swappable topic getter and `ISqsClient` is the worked pair; string statuses exist precisely so
> users can add their own), and the **default service standard**: well-known HTTP surfaces under
> a `/benzene/` prefix (`/benzene/invoke`, `/benzene/spec`, `/benzene/health`, the UIs) so
> framework infrastructure is visibly not a domain endpoint and can be gateway-ruled as one
> group. Applied to the defaults new enough to change compatibly: `UseMeshFleetUi` now defaults
> to `/benzene/fleet-ui` polling `/benzene/invoke`, and examples/Mesh moved with it (the mentions
> of `/invoke`/`/fleet-ui` in the block below are the pre-standard paths of that day). The
> pre-existing `/mesh-ui`/`/spec-ui` defaults are unchanged - migration candidates for the 1.0
> release checklist.

> **2026-07-16 Fleet view update:** the collector now has its UI, and `examples/Mesh/run.sh`
> shows it. `Benzene.Mesh.Ui` gains `MeshFleetUiPage`/`MeshFleetUiMiddleware`/`UseMeshFleetUi`
> (same embedded-HTML pattern as the existing explorer): the **Fleet view** polls a
> `Benzene.Mesh.Collector`'s `mesh:query:fleet` through a wire-envelope endpoint and renders the
> derived fleet - services with health and reduced-feed markers, topic catalog with observed
> consumers, recent flows. examples/Mesh is now a two-halves demo: the services carry meshed
> wire-envelope hosts (`Benzene.Examples.Mesh.Shared.EnvelopeHost`: descriptor + trace feed +
> register/heartbeat announcing, log-and-continue), the aggregator hosts the collector at
> `/invoke` and the Fleet view at `/fleet-ui`, and the checkout handler's traceparent propagation
> produces a live `payments:get -> consumers: orders-api` edge. Verified by running run.sh:
> payments-api renders degraded from its heartbeat (DEMO_PAYMENTS_HEALTHY flips it), shipping-api
> has no row until it starts, and the DEMO_ADD_ENDPOINT restart changes the descriptor hash -
> the drift story from live data. Remaining polish: surfacing per-service drill-downs
> (mesh:query:service/topic/trace) in the page, and the aggregator/artifact bridging noted below.

> **2026-07-16 full-contract update: the .NET implementation is now the primary, complete
> implementation of `docs/specification/mesh.md`.** The previously open item 3 is done:
> `Benzene.Mesh.Collector` implements the spec collector (§4-§6) as dogfooded message handlers
> over an in-memory store - register/heartbeat/traces ingest with validation, provider edges
> replaced wholesale on re-registration, consumer edges derived from trace parentage at query
> time, per-instance health with descriptor-hash mismatch surfacing, missing-feed markers per
> service, and a bounded trace ring (cumulative stats outlive it; unit-pinned in
> `Benzene.Mesh.Test/MeshCollectorStoreTest`). `Benzene.Conformance.Test` now runs
> `mesh-collector-cases.json` too - **all three mesh fixture files pass (115 conformance tests
> green)**, making .NET the first implementation to cover the full contract in one codebase.
> The reversed cross-language demo also ran for real: the .NET collector (hosted behind a
> plain wire-envelope HTTP endpoint) ingested a GO service's registration/heartbeat/traces
> alongside the C# dotnet-orders service and derived the go-greeter↔dotnet-orders fleet -
> providers, consumers, health, and joined flows - across languages, mirroring the earlier
> demo against the Go collector. Remaining integration follow-ups (not contract gaps):
> bridging the aggregator's artifact/UI pipeline to `Benzene.Mesh.Collector` (e.g. the
> collector as an `IMeshServiceSource`, or the aggregator publishing collector state), and
> surfacing the collector's read models in the Mesh UI.

> **2026-07-16 .NET wire-layer update:** items 1, 2, and 4 of the spec-promotion update below
> are now DONE, as the new `Benzene.Mesh.Wire` package (see its CLAUDE.md):
> - Descriptor derivation (spec §2) from `IMessageHandlerDefinitionLookUp` with the §2.1
>   CLR→schema mapping and §2.2 hash, served on the reserved `mesh` topic via
>   `UseMeshDescriptor` (same interception pattern as `UseHealthCheck`).
> - Trace middleware (spec §3) via `UseMeshTrace`: W3C traceparent join/reject,
>   `MeshSpan.Current` propagation for outbound calls, per-transport status read-back through
>   the new `IMeshStatusReader<TContext>` mapper (BenzeneMessage reader included), and
>   `HttpMeshTraceExporter` with the §4 lossy/non-blocking sender rules.
> - `Benzene.Conformance.Test` now runs `mesh-descriptor-cases.json` and
>   `mesh-trace-cases.json` (plus the new canonical `conformance:panic` handler) - all green
>   alongside the existing envelope/status fixtures (109 tests).
> - **The cross-language fleet demo ran for real**: a C# service (`dotnet-orders`) registered,
>   heartbeated, and traced into the GO reference collector (benzene-go's `meshd`) while
>   calling the Go greeter with a propagated traceparent - the collector's fleet view showed
>   the C# and Go services side by side (runtime `dotnet` vs `go`, both healthy) and derived
>   the `greet` consumers as `dotnet-orders, frontdoor, legacy-portal`: a consumer edge joined
>   across languages purely from trace parentage. Verified locally in this pass (Go + .NET
>   toolchains in one environment); the demo program is a few dozen lines over
>   `Benzene.Mesh.Wire` + raw envelope POSTs and is worth productizing into `examples/Mesh`
>   as a follow-up.
> - Item 3 (the aggregator gaining the `mesh:register`/`mesh:heartbeat`/`mesh:traces` ingest
>   topics as sources beside HTTP-poll and Lambda-invoke, passing
>   `mesh-collector-cases.json`) remains OPEN - it is optional per spec §7 and a natural next
>   pass for whoever picks up the aggregator.

> **2026-07-16 spec-promotion update:** the Go port's mesh (benzene-go `mesh`/`meshd`, designed
> in its `docs/design/mesh.md`) has been promoted into this repo's spec as
> [`docs/specification/mesh.md`](../docs/specification/mesh.md), with three conformance fixture
> files (`mesh-descriptor-cases.json`, `mesh-trace-cases.json`, `mesh-collector-cases.json`)
> that the Go port passes as the reference implementation. That contract and this roadmap's
> packages evolved independently and overlap: spec §9 maps `Benzene.Mesh.*` onto the promoted
> wire shapes. The short version — nothing here is discarded (pull aggregation, OpenAPI
> artifacts, Tempo topology, and the UI are collector-side idioms the spec deliberately doesn't
> constrain), and three of this roadmap's own open items are solved by adopting the wire layer:
> the `topology.json` edge-derivation gap (spec §3–4: edges from native trace parentage, no
> Tempo/Prometheus required), the staleness gap flagged by `Benzene.Mesh.Reporting` (spec §5
> heartbeats), and the hand-maintained `mesh.json` (spec §2: derived descriptors; the registry
> remains as a pull-mode bootstrap for unmeshed services).
>
> **What .NET conformance requires** (spec §7 — mesh is optional, but implementing it means
> implementing it compatibly):
> 1. Descriptor derivation (§2) from registration + the §2.1 CLR→schema mapping + the §2.2
>    hash, served on the reserved `mesh` topic (§1) → pass `mesh-descriptor-cases.json`.
> 2. A trace middleware emitting §3 TraceEvents (W3C traceparent join; missing handler /
>    conversion failure / handler exception all traced via their result statuses) → pass
>    `mesh-trace-cases.json`.
> 3. Optionally, the aggregator gains the three ingest topics (`mesh:register`,
>    `mesh:heartbeat`, `mesh:traces`) as additional sources beside HTTP-poll and
>    Lambda-invoke → pass `mesh-collector-cases.json`; consumer edges from trace parentage
>    become a new `TopologyEdgeSource` alongside Tempo.
> 4. Cross-language fleet demo: a C# service and a Go service in one collector's view (either
>    collector — Go `meshd` or this aggregator — since both would speak the same envelopes).
>
> **Blocked in the authoring environment, explicitly:** none of the above C# work is started
> here — this sandbox has no .NET SDK and its egress policy 403s the SDK download (the same
> constraint that blocked Phase 3's live Tempo verification). Writing untested C# against a
> freshly promoted contract was judged worse than scoping it precisely for the next pass with
> a real toolchain. The Go side of the demo is ready: benzene-go's `examples/mesh-helloworld`
> runs the collector + a reduced-feed service today, and its conformance suite pins the wire
> shapes a C# implementation must match.

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
>
> **2026-07-15 multi-transport collection design (Phase A of 4 landed, B-D in progress):** Raised
> by the maintainer: the aggregator's HTTP-only fetch can't reach services with no public HTTP
> surface (AWS Lambda, reachable only via `Invoke`) or services with no synchronous response
> channel at all (SQS/SNS/EventBridge-only Lambdas). Resolved via `mesh-product-owner` design
> passes plus maintainer decisions: pull (via HTTP or a synchronous AWS Lambda `Invoke`) remains
> correct for any service with *some* synchronous entry point - invoking a Lambda causes a cold
> start rather than requiring one already be warm, the same characteristic as an HTTP health
> endpoint on a Lambda. Push is only structurally required for the narrower "zero synchronous
> entry point" case, and ships as **two** swappable client-side implementations (direct
> `IMeshArtifactStore` write, and an HTTP ingestion endpoint) rather than just one, per explicit
> maintainer request. Self-reporting is opportunistic only (piggybacks on real invocations) - no
> scheduled/cron reporting in v1, since that would defeat serverless on-demand billing; a cron
> option is parked, not built. A new config-driven, Docker/Compose-deployable host
> (`deploy/Mesh/Benzene.Mesh.Host`) is also planned, for running the Aggregator + Mesh UI against a
> developer's own real services during local development. Full design and phasing in
> `/root/.claude/plans/the-benzene-grpc-package-serialized-pond.md` (Phases A-D) - **Phase A**
> (the `IMeshServiceSource` seam, additive `MeshServiceRegistryEntry.Source`/`SourceOptions`) is
> landed; **Phase B** (`Benzene.Mesh.Aws.Lambda`, wrapping the already-existing
> `Benzene.Clients.Aws.Lambda.IAwsLambdaClient` rather than reimplementing Lambda invocation),
> **Phase C** (`Benzene.Mesh.Reporting`, `IMeshReportPublisher`/`MeshServiceReport` in
> `Benzene.Mesh.Contracts`), and **Phase D** (`deploy/Mesh/Benzene.Mesh.Host`) remain open.
>
> **2026-07-15 Phase B landed:** `Benzene.Mesh.Aws.Lambda`'s `LambdaMeshServiceSource` sends a
> `"spec"`/`"healthcheck"` topic message to `SourceOptions["functionName"]` via the pre-existing
> `Benzene.Clients.Aws.Lambda.IAwsLambdaClient` (`InvocationType.RequestResponse`) - confirmed the
> receiving side needs zero changes, since `Benzene.Aws.Lambda.Core.BenzeneMessage.BenzeneMessageLambdaHandler`
> already routes a raw Lambda invoke carrying either topic to the normal BenzeneMessage pipeline
> for any service already wired with `.UseSpec()`/`.UseHealthCheck(...)`. `MeshServiceSource.AwsLambdaInvoke`
> added to `Benzene.Mesh.Contracts`. Cross-check tests pin the adapter's hardcoded topic literals
> against `Benzene.Schema.OpenApi.Constants.DefaultSpecTopic`/`Benzene.HealthChecks.Constants.DefaultHealthCheckTopic`
> directly. Phases C and D remain open.
>
> **2026-07-15 Phase C landed:** The push path, for services with genuinely no synchronous entry
> point (Phase B's `LambdaMeshServiceSource` covers "on-demand but reachable via `Invoke`"; this
> covers "no response channel at all," e.g. SQS/SNS/EventBridge-only Lambdas). Per explicit
> maintainer direction, **two** swappable `IMeshReportPublisher` implementations ship, not one:
> `Benzene.Mesh.Aggregator.ArtifactStoreMeshReportPublisher` (direct write into the shared
> `IMeshArtifactStore`, for a reporter colocated with the aggregator's storage) and the new
> `Benzene.Mesh.Reporting.HttpMeshReportPublisher` (POSTs to a new ingestion endpoint,
> `MeshReportMessageHandler` - `[HttpEndpoint("POST", "/mesh/report")]`/`[Message("mesh:report")]`,
> opt-in the same way every Benzene handler is). `MeshServiceReport`/`IMeshReportPublisher` live in
> `Benzene.Mesh.Contracts` - a deliberate, small widening of that package's role from "pure data
> shapes" to "data shapes + zero-I/O port interfaces," so the new lightweight
> `Benzene.Mesh.Reporting` package depends on just `Contracts`, not the whole aggregator.
> `MeshSelfReportMiddleware<TContext>` publishes **opportunistically only** - as a side effect of
> real requests/messages, throttled by a configurable minimum interval - per the maintainer's
> explicit call that a scheduled/keep-warm reporter would defeat on-demand Lambda billing; a
> cron-based mode remains parked, not built. Spec/health are supplied to the middleware as
> delegates (`MeshSelfReportOptions.SpecProvider`/`HealthProvider`), not generated by this package,
> so it stays free of a dependency on `Benzene.Schema.OpenApi`/`Benzene.HealthChecks`. **Known,
> explicitly-flagged gap**: an idle self-reporting service's entry just ages with no signal that
> it's stale - `MeshServiceStatus` has no `Stale` value yet; not solved by this phase. Phase D
> (`deploy/Mesh/Benzene.Mesh.Host`) remains open.
>
> **2026-07-15 Phase D landed - all four multi-transport phases complete:** `deploy/Mesh/Benzene.Mesh.Host`,
> a config-driven, Docker/Compose-deployable Mesh Aggregator+UI - for a developer to run against
> their own real services in local dev, distinct from `examples/Mesh/`'s fake-data demo. Reads
> `mesh.json` (bind-mounted, `MESH_CONFIG_PATH`) via `IConfiguration.Get<MeshHostConfig>()` - this
> repo's first binding of a *list* of config objects, genuinely new territory, not an established
> Benzene pattern being reused. Wires `AddMeshAggregator` + `AddMeshLambdaSource` (both v1 pull
> sources), and adds a new `MeshPollBackgroundService` (`BackgroundService` on a timer) since a bare
> Compose deployment has no external scheduler - local to this Host app only, doesn't change
> `MeshAggregateMessageHandler`'s existing invocation-triggered contract. Own `Benzene.Mesh.Host.sln`
> (not part of `Benzene.sln`/`Benzene.Examples.sln`, mirroring `templates/Benzene.Templates.sln`'s
> precedent), own CI (`build-mesh-host.yml` compiles on every push/PR - real coverage, unlike
> `examples/`) and publish workflow (`deploy-mesh-host.yml`, GHCR, manual `workflow_dispatch` - the
> **first Docker image publish anywhere in this repo**). Two deliberate scope cuts from the original
> sketch, flagged in `deploy/Mesh/Benzene.Mesh.Host/CLAUDE.md`: no `selfReportIngestion.enabled`
> config toggle (reflection-based handler discovery makes `/mesh/report` unavoidably reachable once
> `Benzene.Mesh.Aggregator` is referenced at all, which this Host always does - gating it would need
> an explicit handler allow-list, judged not worth it for v1), and no Tempo wiring (a separable
> follow-up). This closes out the multi-transport data collection epic (Phases A-D) - remaining open
> items are the pre-existing ones tracked in `.claude/PRODUCT_OWNERS.md`'s Mesh PO priorities (live
> Tempo verification, structural edges, topology graph visualization, staleness representation,
> Phase 4/5 of the original roadmap).
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

## 10. 2026-07-19 proposal: transport-neutral topic bindings, a topic-level view, cross-linking, and a unified dashboard

**Status: PROPOSED, not yet implemented.** User feedback session on the spec + mesh UI family.
Grounded against the actual current implementation (see the file references below) before
writing this up; nothing here has been built yet.

### 10.1 The core gap: topics only carry HTTP-shaped transport metadata

Confirmed by reading the actual rendering code (`Benzene.Spec.Ui/spec-ui.html`,
`Benzene.Mesh.Ui/mesh-ui.html`, `Benzene.Mesh.Ui/mesh-fleet-ui.html`): none of them literally
concatenate an HTTP verb into the topic string — the topic renders clean
(`el("span", "op-topic", topic)`), with a colored `POST`/`GET`/… *badge* next to it. But that
badge visually dominates because it's the **only transport signal that exists at all**:

- `docs/specification/mesh.md`'s `ServiceDescriptor` has one **service-level** `binding` field
  (`"binding": "http"` — a single value for the whole service, §2).
- The only **per-topic** transport data anywhere is `Benzene.Schema.OpenApi`'s `HttpMappings`
  (`EventService/RequestResponse.cs`) — HTTP method + path, nothing else. A topic delivered by
  SQS, SNS, Kafka, EventBridge, RabbitMQ, or direct Lambda invoke has **zero** transport
  metadata in the spec or the mesh descriptor today, and a topic reachable by more than one
  transport (e.g. HTTP *and* a queue) has no way to express that either.

Topics are transport-neutral by design (`core-concepts.md` §2 — a topic is just an id + optional
version); the spec/descriptor should reflect that instead of privileging HTTP.

**Proposed fix — generalize `httpMappings` into a per-topic `bindings` array**, a superset that
subsumes the HTTP case rather than sitting alongside it:

```json
"topics": [
  {
    "id": "order:create",
    "version": "v2",
    "requestSchema": { "...": "..." },
    "responseSchema": { "...": "..." },
    "bindings": [
      { "transport": "http", "method": "POST", "path": "/orders" },
      { "transport": "sqs", "queue": "orders-queue" },
      { "transport": "lambda-invoke", "functionName": "orders-create-fn" }
    ]
  }
]
```

- `transport` values line up with the binding catalog `transport-bindings.md` §2 already
  documents (`http`, `grpc`, `sqs`, `sns`, `kafka`, `eventbridge`, `dynamodb-streams`,
  `rabbitmq`, `event-hub`, `service-bus`, `cosmos-changefeed`, `lambda-invoke`/`benzene-message`
  for the raw envelope) — each transport's binding-specific fields mirror what that binding's
  topic-resolution rule already needs (queue name, topic ARN, Kafka topic, EventBridge
  `detail-type`, …), not a new vocabulary invented here.
- A topic MAY list zero bindings (nothing wired to receive it directly — e.g. an outbound-only
  client-side topic) or several (the same handler reachable via HTTP *and* a queue).
- This is a **wire-contract / spec change**, not implementation-only: it touches
  `docs/specification/wire-contracts.md` (or a new subsection of `mesh.md` §2, since it's spec
  material, not payload envelope material) and the `EventServiceDocument`/`benzene` spec format
  (`Benzene.Schema.OpenApi`). Both are still draft v0.1, so this is an allowed pre-1.0 change per
  `specification/README.md`'s versioning note — do it now, not as a post-1.0 breaking change.
- UI follow-up in all three viewers: replace the HTTP-verb-badge-as-topic-decoration pattern with
  a neutral "bindings" chip row (one chip per transport, HTTP included as just one of the
  possible chips, not the default assumption).

### 10.2 Transport-native test payloads in "Try it" — generate/copy only, not live-dispatch

Confirmed **this mechanism already exists**, just not where a user would find it live:
`Benzene.CodeGen.LambdaTestTool` (CLI: `benzene lambda-test-tool`) already builds real SQS/SNS
event JSON, API Gateway request JSON, and the raw envelope per topic, from the same deterministic
example generator (`Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder`) the spec itself
embeds (`DefaultExampleBuilders.cs`). It's AWS-only today and it's an offline file-writer — Spec
UI's "Try it" panel (`spec-ui.html`'s `tryItBlock`) only ever sends the raw
`{topic, headers, body}` envelope over HTTP.

**Decision (confirmed with the user):** generate-and-copy only, not live multi-protocol dispatch.
A live "Send" only makes protocol sense for transports actually reachable over HTTP from a
browser; a genuine direct Lambda invoke or an SQS send isn't reachable from client-side JS at all
without a server-side proxy holding cloud credentials — a materially bigger and riskier piece of
engineering than the value justifies here, and unnecessary when "copy the exact payload, paste it
into the AWS CLI/console/Lambda Test Tool" already satisfies the actual workflow described.

**Proposed shape:** add a transport picker to the "Try it" block, populated from the topic's new
`bindings` (§10.1) plus the always-available raw envelope. Selecting a non-HTTP transport swaps
the payload textarea's content to that transport's full event-shaped JSON (reusing
`DefaultExampleBuilders`' generation logic, exposed as a library call the spec/UI layer can reach,
not just the CLI) and shows "copy" instead of "send". Selecting HTTP/envelope keeps today's live
Send behavior unchanged. Azure-shaped generators (Service Bus, Event Hub) are a documented
follow-up, not blocking — call this out explicitly rather than silently shipping AWS-only and
calling it done (per this repo's honesty-over-false-capability convention).

### 10.3 A topic-level view: producers, consumers, and per-version deprecation signal

**The data mostly already exists — the page doesn't.** The collector already derives consumer
edges from trace parentage (`mesh.md` §4), and `TraceEvent` already carries `topicVersion` per
invocation (`mesh.md` §3) — so "is anything in the estate still consuming `shipping:booked@v1`"
is *already computable* from data `Benzene.Mesh.Collector` ingests today. Nobody surfaces it:
Mesh Explorer's topic table shows producers only (`mesh-ui.html` `renderTopicRows`); Fleet view's
topic table (`mesh-fleet-ui.html`) is a flat all-topics-at-once list with observed consumers but
no per-version breakdown and no drill-down.

**This is the highest-value, lowest-risk item here** — it needs no wire-contract change, only a
new collector query and a new page:

- New collector query topic, e.g. `mesh:query:topic` (body: `{ "topic": "...", "version": "..." }`
  or version omitted for "all versions of this topic id") — collector-side read model, same
  category as the existing `mesh:query:fleet` (`mesh.md` §4's note that `mesh:query:*` are
  collector idiom, not wire contract, so this needs no spec change either).
- Response: the topic's declared bindings (§10.1) from every service that provides it, every
  distinct version seen (from trace events, not just the descriptor's current version — a
  deprecated version can still show up here if traces still reference it), per-version
  invocation/error counts, and the **consumer list per version**, derived from trace parentage
  the same way Fleet view already does it, just scoped to one topic instead of the whole fleet.
- New page (`Benzene.Mesh.Ui`, a third viewer alongside Mesh Explorer/Fleet view): one topic,
  full producer/consumer/version breakdown, with a version reporting **zero observed consumers
  across the trace window** flagged as a deprecation candidate — directly answering "can we
  retire `shipping:booked` v1" and "is anything still listening to `shipping:booked` at all"
  without the cross-team archaeology the user described. Zero-consumer detection needs an
  explicit trace-window caveat in the UI (a version with no *recent* traffic isn't proof of zero
  consumers forever, only within the collector's retained trace window) — state that honestly
  rather than implying certainty a live system can't actually provide.

### 10.4 Cross-linking between the viewers

Confirmed gap: Spec UI, Mesh Explorer, and Fleet view are three independent pages today with no
links between them. Proposed: each viewer gains outbound links wherever it already has the
target's identity —

- Fleet view's service rows → that service's Spec UI (needs the service's spec URL, which the
  descriptor doesn't carry today — either add it to `ServiceDescriptor` or derive it from a
  per-service base-URL convention the dashboard host, §10.5, already knows).
- Fleet view's topic rows and the new Topic view (§10.3) → the new Topic view, and from there
  back out to each listed producer/consumer's Spec UI and Fleet view service row.
- Spec UI's topic cards → the Topic view for that topic (needs Spec UI to know the mesh
  collector's URL, analogous to how it already knows its own `messageEndpoint`).

All of this is plain deep-linking (`?url=`/query-param navigation, the same mechanism `SpecUiPage`
already uses for `data-spec-url`) — no new transport, just wiring pages that already independently
support "point me at an arbitrary URL" together with actual links instead of requiring a user to
copy URLs by hand.

### 10.5 A unified dashboard host, not one Spec UI per service

**Confirmed mostly already possible with existing building blocks — the gap is that nobody has
assembled them.** `SpecUiPage.GetHtml(specUrl)` already supports rendering any service's spec
from a cross-origin URL; `Benzene.Http`'s `UseCors` already exists as off-the-shelf middleware
(`docs/common-middleware.md`). Today's default story is still "each service serves its own Spec
UI, pointed at its own `/spec`" — fine standalone, but wrong for the fleet-browsing experience
being designed here: clicking from the mesh dashboard into a service's spec shouldn't hop to that
service's own hosted page, it should stay inside one dashboard rendering that service's *JSON*.

**Proposed:** a new package (working name `Benzene.Mesh.Dashboard`, or extend
`Benzene.Mesh.Collector`'s own host if collocating is preferred — open question, not decided
here) that serves one copy of Mesh Explorer, Fleet view, the new Topic view, and Spec UI, and
navigates between them by fetching each target service's `/benzene/spec`, `/benzene/health`, and
the collector's `mesh:query:*` topics — never redirecting to a service's own hosted UI. This
requires:

- Every service fronted by this dashboard to serve CORS headers permitting the dashboard's
  origin on `/benzene/spec` and `/benzene/health` (`UseCors` already does this; it just isn't
  part of the default service standard today). If this direction is adopted, add it to
  `design-principles.md` §5 as a documented expectation for services that want to participate in
  a central dashboard — don't leave it as an undocumented prerequisite that only shows up as a
  failed fetch in the browser console.
- The per-service Spec UI hosting (`UseSpecUi()` on each service) doesn't go away — it stays the
  right choice for a team browsing *just their own* service's spec without a dashboard deployed
  at all (adoption-ladder consistency: nothing here should make the non-dashboard path worse).

### 10.6 Suggested sequencing

1. **§10.1 (transport-neutral `bindings`)** first — it's the wire-contract foundation everything
   else displays. Touches `Benzene.Schema.OpenApi` (spec generation), `mesh.md`/descriptor
   derivation, and all three viewers' badge rendering.
2. **§10.3 (topic view)** next — highest standalone value, no further wire-contract dependency
   beyond §10.1's bindings display, buildable against data the collector already has.
3. **§10.2 (transport-native Try it)** — independent of the above, can slot in anytime; reuses
   `Benzene.CodeGen.LambdaTestTool`'s existing generators.
4. **§10.4 (cross-linking)** — needs §10.1 and §10.3 to have somewhere to link *to*.
5. **§10.5 (unified dashboard)** last — the biggest new package, and the one place a real
   decision (dashboard-hosts-collector vs. standalone) needs settling before writing code.

Each of 1–4 ships value standalone even if §10.5 never happens — a service can keep self-hosting
its own Spec UI forever and still benefit from richer bindings, a topic view, better test
payloads, and cross-links between whichever pages a team does deploy.

### 10.7 2026-07-19 (same-day) revision: narrow the scope, and build on the Aggregator, not a new package

Follow-up user feedback narrows §10.1–10.6 considerably. This section supersedes the priority
order and the shape of §10.2/§10.4/§10.5 above; §10.1 and §10.3's substance stand, with §10.3
reframed below.

**1. De-scope §10.2 (transport-native "Try it" payloads).** Explicitly called out as low value
and a security concern: generating test payloads is one thing, but any UI that reaches into
"different systems" from a central, aggregated view raises exactly the kind of access/blast-radius
question a platform-wide dashboard should avoid inviting. Not part of the near-term plan. If it
ever happens, it stays scoped to a single service's own self-hosted Spec UI (where "this page can
reach this one service" is an unremarkable, already-true statement), never the centralized view.

**2. Reject the micro-frontend pattern outright.** Explicit instruction: don't build toward a
world where each domain service serves its own UI fragment that a dashboard stitches together.
Each service's own hosted Spec UI (`UseSpecUi()`) stays exactly what it already incidentally is —
optional, like a web service having its own Swagger page. Useful for a team looking at their own
service in isolation; **irrelevant to the platform-wide aggregation story**, which must be built
by fetching each service's machine-readable JSON (spec, health, and whatever else) and rendering
it centrally, once, in one dashboard codebase.

**3. The priority is platform-wide topic/transport visibility — and the thing to build on already
exists.** Checked `Benzene.Mesh.Aggregator`'s actual current behavior before writing this: it
*already* does exactly the pattern just described — polls every registered service's spec + health
JSON concurrently (`MeshAggregator.RunOnceAsync`, `IMeshServiceSource`), computes contract drift,
and — this is the part that matters most here — **already derives structural producer/consumer
topology edges from the fetched specs alone, with no live tracing or collector involved** (§7.3's
`topology.json`, `source: "structural"`). This means the highest-priority ask — "which services
create a topic and which consume it, across the whole platform" — is **already substantially
buildable as an Aggregator + Mesh Explorer enhancement**, not a new package:

- **§10.1 (transport-neutral `bindings`)** stays exactly as designed — extend what
  `Benzene.Schema.OpenApi` puts in the spec, which is exactly what `Benzene.Mesh.Aggregator`
  already fetches from every service. No new fetch path needed; the Aggregator picks up richer
  bindings for free once the spec carries them.
- **§10.3 (topic view), reframed:** build the producer/consumer/version breakdown primarily off
  **structural** data the Aggregator already derives from specs — "which services' specs still
  declare producing/handling `shipping:booked@v1`" answers most of the deprecation question
  (§10.3's original motivating example) with **zero live traffic dependency and zero collector
  security surface** — a structurally orphaned topic (nobody's spec claims it anymore) is a
  strong, low-risk deprecation signal on its own. The collector/trace-derived "is it *actually*
  still receiving traffic" signal (mesh.md §4's consumer-edges-from-trace-parentage mechanism,
  Fleet view) remains a valid **optional enhancement** layered on top later — a version can be
  structurally wired but genuinely idle, which only traces can tell you — but it is explicitly
  **not a prerequisite** for shipping the topic view. Build the structural version first.
- **§10.5, reframed: this is not a new "dashboard" package.** It's `Benzene.Mesh.Aggregator` +
  `Benzene.Mesh.Ui` (Mesh Explorer), extended: richer per-topic bindings in the rendered catalog,
  and the new topic-level drill-down (§10.3) added as a page within the *existing* Mesh Explorer
  rather than a new host. This is materially less new engineering than §10.5 originally proposed,
  and it matches the user's explicit direction: one central UI, driven off JSON, no per-service UI
  fragments.
- **§10.4 (cross-linking), reframed:** since there is only ever one centrally-hosted UI in this
  direction (Mesh Explorer, extended), "cross-linking" is internal navigation within that one
  page/app (service card → its topics → a topic's full producer/consumer breakdown → back), not
  links between independently-hosted pages on different services. Considerably simpler than
  originally scoped.

**Revised near-term sequencing:** §10.1 (bindings in the spec + Aggregator fetch) →
§10.3-structural (topic/producer/consumer view in Mesh Explorer, off Aggregator data only) →
internal navigation between the two. The collector/Fleet-view trace-derived enhancement, live
test-payload dispatch, and any standalone dashboard package are all explicitly deferred, not
cancelled — revisit only after the structural, pull-based visibility story above is shipped and
proven useful on its own.
