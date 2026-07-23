# Benzene Service Mesh Visibility — Rough Roadmap & Design (2026-07-14)

**Status:** IN PROGRESS.
> **2026-07-23 topology edges now carry usage-derived req/min + error rate (mesh-product-owner ruling).**
> Owner feedback: the main-page topology table (client/server/source/req·min/error/p50·95·99) looked
> "entirely empty" though the usage feed worked. Root cause: the edges *were* there (structural,
> published every run) but every metric column was hard-coded null. `MeshAggregator.BuildTopology` now
> attributes `RequestsPerMinute` + `ErrorRate` from the merged `usage.json` onto a structural edge
> **only where it can be done unambiguously** — the single-producer rule: one producer, and either the
> per-consumer `Service` dimension present or a single consumer; all-or-nothing per (deduped) edge;
> cross-source double-count guarded; latency percentiles never attributed (metrics feed has no spans).
> Error rate classified only against the metric standard's `success`/`failure` tokens (a `<missing>` or
> wire-vocabulary status leaves error rate blank, req/min still shows). **Honesty over fullness:** on
> today's `Service`-null demo feeds this lights up only the 1:1 command legs (2 of 9 AwsMesh edges);
> the fan-out rows stay blank until the per-consumer usage dimension (an adapter follow-up) is wired —
> the same code then fills them. See `Benzene.Mesh.Aggregator/CLAUDE.md` "Usage-derived edge metrics".
> **2026-07-22 FEEDBACK TRIAGE — send-demo-payloads ask lands on §10.2/§10.7 (mesh-product-owner).**
> A maintainer feedback batch (full triage + UI-side requirements F1/F2/F3 in
> `work/mesh-ui-product-vision.md`, dated block at top) includes: *"Should be able to send in demo
> payloads … feature toggleable … users will not want that on production … build them from topic,
> headers and payload fields, and construct those into an SQS … define custom payloads in the code
> … might be its own screen."* This splits against this roadmap as follows:
> - **Compose & copy a transport-dressed payload (F3a):** APPROVED, near-term, static-floor-safe.
>   This is exactly §10.2's "generate-and-copy, not live-dispatch" affordance and reuses machinery
>   that already exists — `LambdaTestFilesBuilder`'s `sns`/`sqs`/`api-gateway`/`benzene-message`
>   dressing over the deterministic `Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder`, and the
>   opt-in runtime endpoint already designed in `work/runtime-test-payloads-plan.md`
>   (`UseTestPayloads()`). "Supported payloads" = schema-derived defaults (already inlined per
>   topic in `MeshTopicEntry.RequestSchema`/`ResponseSchema`/`MessageSchema`) **plus** code-registered
>   custom examples, mapped to the existing BYO-schema seam (`SuppliedSchemaCatalog`/
>   `AddSuppliedSchemas`, see `work/complex-payloads-byo-schema-plan.md`). Envelope-dressing lives in
>   a runtime-clean core + opt-in `Benzene.*.TestPayloads.Aws` package (plan decision 1(c)) — never
>   in `Contracts`/`Ui`, never pulling AWS test-helpers into a service runtime. Azure dressing is a
>   documented follow-up, not silently AWS-only. **No Cloud Service spec widening** — topics/schemas/
>   transports/HTTP-mappings are all already in the spec and already fetched. Taut.
> - **Live dispatch (F3b-revised): §10.7 NOT reopened — candidate that MIGHT clear its bar,
>   EXPLORE-AND-DESIGN pending a maintainer ruling.** The maintainer (2026-07-22) did **not**
>   authorize the queue-injection version (§10.7 stands as-is for SQS/SNS/stream injection) and
>   steered to a narrower direction: *"the payloads would be sent straight to the consumer, such as
>   the Lambda and not to the SQS … A possible other solution for http is to provide a wired in
>   swagger interface."* This partitions by transport (full analysis in `mesh-ui-product-vision.md`
>   F3b-revised):
>   - **Direct-invokable transports (Lambda `Invoke` / HTTP / BenzeneMessage):** send straight to
>     the one target service, reusing the **access path the aggregator already has** (same
>     `lambda:InvokeFunction` / same HTTP POST it already uses for spec/health) — **no new credential
>     type**, bounded to one known service rather than fanning into shared infra. This plausibly
>     clears §10.7's "don't reach into *different systems* from the aggregated view" objection.
>     Residual risk is real but smaller: the handler runs for real (side-effects, possibly its own
>     downstream publishes), so it still needs opt-in + `AllowInProduction` gating and stays off
>     prod. Vessel for Lambda/BenzeneMessage: `deploy/Mesh/Benzene.Mesh.Host` (browser can't
>     `Invoke`). PO recommendation: clears the bar — but held for the maintainer's ruling; §10.7
>     stays NOT reopened until then.
>   - **HTTP "wired-in Swagger":** best served by **deep-linking each service's own `UseSpecUi()`
>     "Try it"** (same-origin, live) — which is *literally* the §10.7-sanctioned home for live
>     dispatch, so it needs **no exception at all**. A centralized cross-origin Swagger in the mesh
>     is the heavier alternative (needs CORS allow-listing per §10.5 + browser-carried auth) and is
>     separately decided.
>   - **Queue/stream transports (SQS/SNS/Event Hub/Kinesis/Event Grid):** out of scope for live
>     send — **F3a compose+copy only**, exactly as §10.7 left them.
>   - Data-layer/packages implications recorded here; each build case gets its own design doc only
>     if/when the maintainer approves. `runtime-test-payloads-plan.md` (opt-in `UseTestPayloads()`,
>     transport-dress package split, gate decision 3) is the reusable foundation for all of it.
>
> **2026-07-22 ownership merge:** `mesh-ui-product-owner` has been merged into
> `mesh-product-owner` — one owner for the whole mesh product (this doc and
> `work/mesh-ui-product-vision.md` now share that owner). Mentions of
> `mesh-ui-product-owner` in older blocks below are historical. The merged
> brief also makes the owner guardian of the Cloud Service spec's surface:
> coverage of the product's needs, kept taut and small.
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
>   **Update:** the shipped `ClientHealthCheck` (`Benzene.Clients.HealthChecks`, on the `contracts`
>   topic — see `work/client-health-checks-design.md` §7) now emits a `HealthCheckDependency("Service",
>   name)` per downstream, i.e. consumer→provider edges are becoming expressible in health output. The
>   *edge-derivation* step (joining those to provider identities) is still the unbuilt piece; the
>   resource-identity join key it needs is co-designed with the deferred §10.16/§10.18 per-topic binding.
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
> **2026-07-20 staleness-representation ruling (mesh-product-owner):** resolves the "Staleness
> representation" open item flagged by Phase C (the 2026-07-15 block above: "an opportunistically
> self-reporting service's entry just ages with no signal that it's stale - `MeshServiceStatus`
> has no `Stale` value yet") and the data requirement the `mesh-ui-product-owner` filed against
> the shipped issue inbox (`work/mesh-ui-product-vision.md`, 2026-07-20 block: the inbox renders
> staleness as an explicit "pending data" leg because nothing backs it). **Decision — staleness is
> a read-time derivation over a raw timestamp, not a baked status, on either plane:**
> - **It is not a `MeshServiceStatus` value.** Widening that three-constant set (`healthy`/
>   `unhealthy`/`unreachable`, switched on by `DetermineStatus` and the UI) would conflate two
>   orthogonal facts: *what a service last reported* vs. *how long ago it reported*. A service can
>   be healthy-as-last-heard **and** stale at once; one string field can't carry both. Kept
>   separate.
> - **The aggregator cannot honestly compute staleness, so it doesn't.** Staleness of a
>   statically-hosted artifact is relative to *when it is read*, not when it was generated - the
>   aggregator, at generation time, has just written every row and thinks all of it is fresh. Only
>   the reader knows "now." So on the static plane staleness is a **UI-side derivation**, and the
>   contract's job is to surface the raw per-row timestamp the UI needs, not a pre-computed boolean
>   that freezes a threshold policy into the artifact.
> - **The real gap is a missing per-service timestamp on the manifest, not a missing status.** In
>   pure *pull* mode staleness ≈ `unreachable` (the aggregator is the clock; every run stamps a
>   fresh `FetchedAtUtc`). The gap is specifically the *push* path (Phase C self-report): a
>   `services/{name}.json` written by `ArtifactStoreMeshReportPublisher` ages while `manifest.json`
>   keeps re-publishing, and `MeshManifestEntry` carries **no** timestamp - only
>   `MeshServiceSnapshot.FetchedAtUtc` does - so the issue inbox, which reads `manifest.json` alone,
>   literally cannot see the age without fetching every snapshot. Fix: denormalize the snapshot's
>   `FetchedAtUtc` up onto `MeshManifestEntry` as a new optional `SnapshotAtUtc` (nullable, trailing,
>   additive - same treatment as `OwningTeam`/`Transports`), distinct from the manifest-level
>   `GeneratedAtUtc` precisely because in push mode a single stale row's snapshot can be far older
>   than the run that emitted the manifest.
> - **The collector/fleet plane needs no contract change.** `ServiceSummary.LastSeen` (heartbeat
>   age) already exists; staleness there is a pure `mesh-fleet-ui.html` derivation nobody has
>   rendered yet - handed back to `mesh-ui-product-owner` as a UI-only follow-up, not a data item.
> - **Threshold lives in the UI, default 24h on the static plane.** "Too old" is a viewing-time
>   policy, so it belongs in the issue inbox (a JS knob), not in `Contracts` or the aggregator.
>   Recommended default 24h for the artifact plane (a normally-scheduled mesh refreshes far more
>   often; 24h won't false-positive but will catch a genuinely dead self-reporter). The collector
>   plane, when it renders staleness, wants a much shorter threshold (a few missed heartbeats) -
>   the UI PO's call. **Mesh-side build = the `SnapshotAtUtc` field + aggregator thread-through +
>   tests only; the Stale classification and threshold knob are the UI PO's package.**
>
> **Status:** APPROVED (scoped down from the UI PO's proposal - no `MeshServiceStatus.Stale`, no
> collector-plane contract change). Open item moves from "no signal exists" to "timestamp shipped on
> the manifest; UI derivation pending in `Benzene.Mesh.Ui`/`mesh-fleet-ui.html`."

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

### 10.8 2026-07-19 (dx-champion review) — the structural signal isn't fully there yet, before it ships

Requested review, framed around the actual problem this whole feature exists to solve: in a
system with many services and many events, change becomes hard because visibility is poor.
Findings below are verified directly against `MeshAggregator.cs`/`MeshTopicEntry.cs` (not just
taken on the reviewer's word) — the direction in §10.7 is right, but two of the gaps it found are
blockers for §10.3's own motivating example ("is `shipping:booked` v1 safe to retire"), not later
polish.

**Confirmed blocker 1 — a purely-outbound topic is invisible, not shown-with-zero-consumers.**
`MeshAggregator.BuildTopicCatalog` (`MeshAggregator.cs` line 144) only iterates
`results[i].Topics` — topics a service *handles* (spec `requests`, via `ParseTopics`). A topic a
service declares it *sends* (spec `events`, via `ParseOutboundTopics`) is used only inside
`BuildTopology` (line 114) to draw a structural edge, and only when some other service's handler
exists to draw the edge to. A topic with a producer and **zero** handlers anywhere in the fleet —
exactly the deprecation-candidate case this feature is meant to surface — produces no row in
`topics.json` and no edge in `topology.json`. It doesn't read as "orphaned"; it doesn't appear at
all, which is worse for the "is it safe to delete this" question than a loud zero-consumers flag
would be. `ServiceTopic`/`MeshTopicEntry` need a producers list built from `events`, not just the
existing handlers list built from `requests`, so a topic entry can exist from a producer
declaration alone.

**Confirmed blocker 2 — topic version is dropped at the aggregator boundary.** Verified:
`RequestResponse.Version` already exists on the spec (`Benzene.Schema.OpenApi`), but
`ServiceTopic` (`MeshAggregator.cs` line 212: `(string Topic, bool Reserved, MeshTopicHttpMapping[]
HttpMappings)`) never carries it through, and `BuildTopicCatalog` keys its dictionary by topic id
alone (line 146). Every version of a topic collapses into one row. §10.3's own worked example —
"is `shipping:booked@v1` safe to retire while v2 is alive" — cannot be answered with today's
structural data: the topic view would show "has producers/consumers," full stop, no way to tell
versions apart. This has to land as part of §10.3's scope, not a later refinement — thread
`Version` through `ServiceTopic`/`MeshTopicEntry`, key the catalog by `(topic, version)`.

**Missing signal — ownership/contact.** Confirmed: no `owningTeam`/contact field exists anywhere
in `Benzene.Mesh.Contracts` today (`MeshServiceRegistryEntry`'s constructors carry only
`name`/`specUrl`/`healthUrl`/`source` — this repo's own §7.4 sketch of `mesh.json` once proposed
`owningTeam` but it was never implemented). "Who do I talk to before I change this" is one of the
plainest questions a developer asks when weighing a breaking change, and nothing in the plan
answers it. Cheap to add without new infrastructure: `mesh.json` is already a human-edited,
per-service registry — an optional `owningTeam` string per entry, threaded through to the topic
view's producer/consumer list, is additive and config-only. Add to §10.3's scope.

**Don't let cross-linking (§10.4) slip.** A topic view listing producer/consumer service names by
text only half-answers the daily workflow — the very next question is always "is that consumer
even healthy/deployed right now," answered by one click into that service's card in the same Mesh
Explorer. Since §10.7 already reframed this as pure in-app navigation (no CORS, no separate
host), there's no reason to sequence it as a follow-on: producer/consumer names should link to
their service card from day one of §10.3, not arrive in a later pass.

**Deferred, not urgent, but worth naming so they don't become an invisible ceiling:**
- *Staleness/changelog:* `MeshServiceSnapshot` already tracks `SpecHash`/`PreviousSpecHash`
  this-run-vs-last, but nothing further back — "when did this topic's schema actually last
  change" isn't answerable beyond one run. A short rolling history of hash changes with
  timestamps is a natural next increment once §10.3 ships, not a blocker for it.
- *Discoverability at scale:* `mesh-ui.html`'s topic table has a reserved/domain toggle but no
  search — fine for a demo fleet, not for "hundreds of topics." Copy the search-box pattern Spec
  UI and Fleet view already have.
- *Dev-time/CI surfacing:* everything in §10.1–10.7 is a viewer someone has to remember to open.
  The problem bites hardest at the moment someone is about to delete a topic or a field — in a PR,
  not a browser tab. `Benzene.Schema.OpenApi`'s existing backward-compatibility gate
  (`SchemaCompatibility.EnsureBackwardCompatible`) only knows one service's own baseline today; a
  CI-time check that pulls the aggregator's last-published, producer/consumer/version-aware
  `topics.json` (once blockers 1–2 are fixed) and warns or fails a PR that removes a topic/version
  still consumed elsewhere turns a cross-team surprise into a named, addressed CI failure. Real,
  but explicitly a later phase — not part of the near-term sequencing below.

**Revised §10.3 scope (supersedes the "already substantially buildable" framing in §10.7):**
producer list (from `events`, not just handlers) + version-keyed topic catalog in
`Benzene.Mesh.Contracts`/`Benzene.Mesh.Aggregator`, with in-page links to service cards, and an
optional `owningTeam` field threaded through — all part of the same delivery, not a page bolted
onto today's `topics.json` as-is. Staleness history, topic search, and CI/dev-time surfacing
remain explicitly deferred phases after that ships.

### 10.9 2026-07-19 (same day) — a hard boundary: services self-describe, only the Aggregator judges; plus the reverse gap

Follow-up feedback draws a boundary that needs to be permanent, not implicit, before any of §10.1–
§10.8 gets built: **a service's own spec MAY carry more self-descriptive data (§10.1's `bindings`
is exactly this kind), but a service MUST NOT be asked to have an opinion about whether its own
topics/events are still in use fleet-wide, or whether they're safe to deprecate.** A single
service structurally cannot know that — it only knows what it itself produces and consumes.
Judgments like "safe to retire," "orphaned," or "gap" only make sense computed centrally, by the
Aggregator, looking holistically across every service's self-description at once. Concretely:

- **The Core Specification (`docs/specification/`, including `mesh.md`'s `ServiceDescriptor`) MUST
  stay purely self-descriptive** — a topic's entry there is "id, version, schema, bindings," never
  "used," "deprecated," or "orphaned." This is already true today (`ServiceDescriptor.topics` has
  no such field, and nothing in §10.1–§10.8 proposed adding one) — this section makes it an
  explicit, permanent rule so nobody adds a `deprecated: bool` to the wire contract later. A
  deprecation judgment is a fact about the *fleet*, not a fact about the *service*, and the wire
  contract is what any Benzene implementation in any language must honor — it has no business
  carrying a fleet-level opinion no single service is positioned to assert.
- **The aggregated, judged view (the extended `topics.json` from §10.8's revised §10.3 scope —
  producers, consumers, versions, "likely safe to deprecate," "gap") is entirely
  `Benzene.Mesh.Aggregator`-owned output, not part of `docs/specification/` at all.** This split
  already exists structurally today, just not stated as a rule: `topics.json`/`manifest.json`/
  `topology.json` are Aggregator artifacts documented in `Benzene.Mesh.Aggregator/CLAUDE.md`, a
  different thing entirely from `mesh.md`'s collector wire contract (mesh.md §9 already separates
  these as "two designs [that] solve the same problem from opposite ends"). Once §10.3 ships, its
  format should be documented as an Aggregator artifact (that package's own `CLAUDE.md`, plus a
  `docs/` page if it needs one) — explicitly **not** folded into `docs/specification/mesh.md` or
  the [Cloud Service Profile](../docs/specification/cloud-service-profile.md), which are the
  cross-language conformance surface every port has to implement identically. Keeping the judged
  view out of that surface means a port that never implements this aggregation is still fully
  Benzene-conformant — the aggregation is fleet tooling built *on* the spec, not part of it.

**The reverse gap, and it's mostly free.** §10.8's blocker 1 was "a topic produced somewhere but
consumed nowhere" (deprecation candidate). The mirror case matters equally: **a topic *consumed*
somewhere but *produced* nowhere in the fleet** — a service handles a topic that no service's
spec declares sending. This is explicitly **not** presented as a problem: the producer may
legitimately be a third party, or something writing directly to a queue/topic outside Benzene
entirely (the plan's own §10.1 already acknowledges topics don't have to be Benzene-native on
every side). It should be surfaced as an informational **gap**, not an error — "nothing in this
platform's own fleet produces what this service is listening for; either that's expected (external
source) or it's worth asking someone." Once §10.8's producer-list fix lands (`MeshTopicEntry`
carries producers from `events`, not just consumers from `requests`), this case falls out of the
same data with no further modeling work: a topic entry with a non-empty consumer list and an
*empty* producer list *is* the gap signal. The only net-new work is in the topic view itself:
label the two directions distinctly (empty consumers → "deprecation candidate"; empty producers →
"gap, possibly external source") rather than treating both as an undifferentiated warning, since
they carry different operational meaning and only one of them implies "someone should look at
this before it's deleted."

**Restated goal, unchanged in substance from §10.3/§10.8, now with the direction locked down:**
one aggregator-computed view of which services wire up to which other services and which topics/
versions are consumed by whom — producers, consumers, and both directions of gap — built entirely
from self-descriptive data services already publish, judged only by the thing looking at the
whole platform at once.

### 10.10 2026-07-19 (implementation) — §10.3-structural shipped

The revised §10.3 scope from §10.8/§10.9 is implemented and tested (1533+132 tests green across
`Benzene.Core.Test`/`Benzene.Mesh.Test`, full `Benzene.sln` build clean):

- **Fixed a real, previously-undetected spec bug found along the way**: `RequestResponse.Version`
  existed as a C# property (correctly populated from the handler's topic version) but was never
  actually written to the wire — `RequestResponse.SerializeAsV3` never emitted a `"version"`
  property at all. Every `benzene`-format spec ever served was silently dropping topic version
  from the JSON. Fixed, plus added the same field to `Event` (broadcast/sender topics), which
  never carried version at all before now — both omit `version` from the wire when empty, so an
  unversioned topic's JSON is unchanged (`Benzene.Schema.OpenApi`: `RequestResponse.cs`,
  `Event.cs`, `EventServiceDocumentBuilder.cs`, `SchemaDeserializer.cs`).
- **`Benzene.Mesh.Aggregator`'s `topics.json`** (`MeshAggregator.BuildTopicCatalog`) now keys by
  **(topic, version)**, tracks **producers** (from `events`) alongside the existing **consumers**
  (from `requests`, renamed from `Services` — the plain rename `MeshTopicEntry.Consumers`, plus
  new `MeshTopicEntry.Producers: MeshTopicProducer[]`), and computes `MeshTopicEntry.Status`
  (`Benzene.Mesh.Contracts.MeshTopicStatus.DeprecationCandidate`/`.Gap`/`null`) per the rules
  worked out in §10.9 — including the HTTP-endpoint false-positive guard on `gap` (only flagged
  when every consumer of that topic has zero HTTP mappings, so an ordinary REST endpoint with no
  fleet-internal producer never misflags).
- **Ownership**: `MeshServiceRegistryEntry`/`MeshManifestEntry` gained an optional `OwningTeam`
  (additive, `null` default), round-tripping through `mesh.json` (`MeshRegistryJson`) to the
  manifest, per §10.8's finding that nothing answered "who do I talk to before I change this."
- **`Benzene.Mesh.Ui`'s Mesh Explorer** (`mesh-ui.html`) renders all of the above: topic table
  gained Version/Producers/Consumers/Status columns (with a tooltip explaining what each status
  means and that neither is an error), service cards show the owning team when set.
- **Confirmed staying out of `docs/specification/`**, per §10.9's rule: none of this touched the
  Core Specification or the Cloud Service Profile — `MeshTopicEntry`/`MeshTopicStatus` live only
  in `Benzene.Mesh.Contracts`/`Benzene.Mesh.Aggregator`, documented in those packages' own
  `CLAUDE.md` and in `docs/mesh-ui.md`, not in `docs/specification/mesh.md`.

**Explicitly not done in this pass — still open for a future iteration:**
- **§10.1 (transport-neutral `bindings`)** — deferred. Auto-deriving non-HTTP bindings (SQS queue
  name, Kafka topic, etc.) needs new per-transport declaration attributes/wiring-time capture
  across ~10 transport packages, a separate, larger design effort not yet scoped. §10.3 turned out
  not to depend on it — producer/consumer/version visibility works off `requests`/`events` alone.
- **§10.5 in full** (a genuinely centralized dashboard consolidating specs from multiple deployed
  fleets) — out of scope; §10.4's cross-linking below covers the single-page case.
- **§10.8's deferred items** (staleness/changelog history, topic search at fleet scale, CI/dev-time
  surfacing of the compatibility gate) — unchanged, still explicitly future phases.
- **§10.2 (transport-native test payloads)** — still de-scoped per the user's explicit instruction
  in §10.7, not revisited.

### 10.11 2026-07-19 (implementation) — §10.4 cross-linking shipped

Every producer/consumer name in the topic table (`mesh-ui.html`) is now a real, keyboard-accessible
`<button>`, not inert text. Clicking one clears an active search filter if it's hiding the target
(the filter only toggles `display`, so every card always exists in the DOM), scrolls that service's
card into view, opens it (dispatching a real click on its header, so it goes through the exact same
lazy-load path as a manual click — not a shortcut that skips `loadDetail`), and flashes an
accent-ring animation so the jump is visually obvious even when the target card was already on
screen. This directly answers the "producer/consumer names alone half-answer the question" gap
§10.8 flagged — "`legacy:refund` has one remaining consumer, `payments-api`" is only useful once
the very next click tells you whether that service is even healthy right now.

Verified end-to-end with a real headless-browser smoke test (Playwright against a local static
server serving `mesh-ui.html` plus fixture `manifest.json`/`topics.json`), not just code review:
confirmed the new table headers/status badges render, confirmed clicking a producer chip opens and
flashes the correct card and triggers its real detail fetch (a deliberately-unfixtured 404 proved
the real `loadDetail` code path ran, not a stub), and confirmed the filter-clearing behavior
(a card hidden by an active filter is correctly revealed when cross-linked to, and the filter input
itself is cleared).

Still not done: linking the *other* direction (a service card → the topics it's involved in) and
linking out to `Benzene.Spec.Ui`/topic-level detail beyond what's already in the table — both
reasonable next increments, not started here.

### 10.12 2026-07-19 (implementation) — the topic view itself

The flat topics table is one row per (topic, version); the actual "a topic, its versions, and who
produces/consumes each" page requested is now a dialog opened by clicking a topic id
(`mesh-ui.html`'s new `#topic-view`). It groups every version of the clicked topic id together —
one block per version, each with its own producers/consumers lists (with HTTP mappings shown) and
status badge — so "is anything still consuming `shipping:booked` v1 while v2 is live" is answered
in one view instead of scanning the flat table for every row sharing that topic id. Producer/
consumer names inside the dialog reuse the same `svcChip`/`goToService` jump-to-card links as the
table (closing the dialog first, so the scroll/flash lands on a visible page). Kept as a dialog
within the existing single-page Mesh Explorer rather than a new hosted page or route, consistent
with §10.7/§10.9's "one centralized UI" direction — no new hosting, no CORS story, no micro-frontend.

Verified end-to-end with the same real-browser (Playwright) approach as §10.10/§10.11: opened the
dialog for a two-version topic, confirmed both version blocks rendered with correct producer/
consumer counts and the right one's status badge, confirmed a chip clicked *inside* the dialog
closes it and correctly jumps to/opens/flashes the target service card, and confirmed both the
close button and native Escape-key dismissal work. The test caught a real bug before it shipped:
the version label concatenated a hardcoded `"v "` prefix onto version strings that already start
with `v` (e.g. `v1` rendered as `v v1`) — versions are arbitrary strings, not guaranteed to follow
that convention, so the prefix was dropped and the raw version string is shown as-is.

Not done: no deep-linkable URL for a specific topic (e.g. a `#topic-<id>` hash) — opening a topic
view is currently session-local, not shareable via URL. A reasonable next increment, not started
here since it wasn't part of what was asked.

### 10.13 2026-07-19 (implementation) — topic search, and the reverse cross-link for free

Two of §10.8/§10.11's remaining deferred items, done together since they turned out to be nearly
the same piece of work:

- **Topic search at fleet scale** (§10.8's "discoverability at scale" finding): the topics table
  gained its own search box, matching against the topic id *or* any producer/consumer service
  name — not just topic id, so it doubles as a lookup by service. An empty-result state shows a
  clear "no topics match" message rather than a silently blank table.
- **The reverse cross-link** (§10.11's "still not done: a service card → the topics it's involved
  in"): each service card gained a **topics** action that pre-fills the new search box with that
  service's name, re-renders the table, and scrolls to it. Deliberately not a new view — it reuses
  the search box directly, since "which topics does `orders-api` touch" and "find topics matching
  `orders-api`" are the same question asked from two directions.

Verified end-to-end with the same real-browser (Playwright) approach as the prior three
increments: confirmed search matches by topic id and by service name independently, confirmed the
empty-state message, confirmed clearing the filter restores every row, and confirmed clicking a
service card's "topics" link correctly fills the search and shows exactly the topics that service
produces or consumes (including topics where it's a producer on one version and absent on another
— proving the match is per-topic-entry, not per-topic-id).

Still not done: the deep-linkable topic URL from §10.12, and §10.8's remaining staleness/changelog
history and CI/dev-time surfacing items — all still open, none blocking on what's shipped so far.

### 10.14 2026-07-19 (implementation) — deep-linkable topic URLs

Closes §10.12's remaining "not done": opening a topic view now sets `location.hash` to
`#topic:<encoded-topic-id>` via `history.replaceState` (no new history entries per open/close, so
back/forward isn't cluttered). A page loaded with that hash already in the URL reopens the same
topic once `topics.json` has resolved — the check runs at the end of `renderTopics()`, after
`currentTopics` is populated, plus on `hashchange` for a link followed while the page is already
open. Closing the dialog clears the hash again, hooked off `<dialog>`'s native `close` event so
every close path is covered in one place — the close button, Escape, and a producer/consumer chip
inside the dialog navigating elsewhere via `goToService` (which explicitly closes the topic view
first) all fire it identically, confirmed in the verification below. An unknown/stale topic id in
the hash is a safe no-op (`openTopicView` already early-returns when nothing matches), so a bad or
outdated link never errors.

Verified end-to-end with the same real-browser approach as the prior increments: confirmed the
hash appears correctly (URL-encoded) after clicking a topic, confirmed it clears on close via the
button, confirmed direct navigation to a URL carrying the hash opens the correct topic on load,
confirmed Escape also clears the hash (proving the single `close`-event hook covers non-button
dismissal too), and confirmed an unknown topic id in the hash leaves the dialog closed rather than
erroring.

All four of §10.8's originally-deferred items are now shipped except CI/dev-time surfacing and
staleness/changelog history over time — both real, both still open, neither blocking anything
built so far.

### 10.15 2026-07-19 (implementation) — the topic view becomes a page, not a popup

Direct feedback on §10.12: a small modal dialog was the wrong shape. What was actually wanted is
"a whole new page around that topic, much like the pages for a service, with sections for each
service so you can drill back into the service from there." Reworked accordingly, replacing the
`<dialog>` entirely:

- **A real view swap, not an overlay.** `#main-view` (the service list, topic table, topology —
  everything Mesh Explorer normally shows) and `#topic-page` are mutually exclusive; opening a
  topic hides one and shows the other, with a **Back** button to return. `location.hash` is the
  single source of truth for which is showing (`syncTopicPageFromHash`, reacting to `hashchange`),
  so the browser's own Back/Forward buttons, a bookmarked link, and the in-page Back button/Escape
  all converge on the same behavior instead of three different code paths doing similar things
  slightly differently.
- **"Sections for each service" means the real service card, not a name.** Each version's
  Producers/Consumers sections now embed the *actual* `buildServiceCard(svc)` used in the main
  list — same accordion, status/drift badges, owning team, spec/health/spec-ui links, lazy
  health-check detail — looked up from the manifest by name. This is what makes "all the details
  about that topic" literal rather than aspirational, and it's what makes "drill back into the
  service... from that page" possible: the embedded card's own **topics** link (§10.13) still
  works from inside the topic page, so a producer/consumer's own topics are one more click away
  without leaving. A producer/consumer name absent from the manifest (legitimate for a `gap`
  topic's external producer, or a since-deregistered service) renders as a plain placeholder
  instead of a broken card, rather than silently dropping it.
- **Navigation, not a dialog dismissal.** Opening a topic sets `location.hash` directly (a real,
  navigable history entry — clicking through several topics and hitting Back moves through them
  the way normal links do); leaving clears it via `history.replaceState` rather than
  `location.hash = ""`, specifically to avoid the bare trailing `#` the latter leaves in the
  address bar - caught by the same real-browser verification below, not by inspection.

Verified end-to-end with the same real-browser (Playwright) approach as every prior increment in
this arc, this time against a fixture with a service *outside* the manifest as a topic's producer:
confirmed the view swap (main-view hidden, topic-page shown) and the URL on open, confirmed real
embedded service cards render with correct status/drift/owning-team data and that expanding one
triggers its actual (unfixtured, 404ing) health-check fetch — not a stub, confirmed the unknown-
service placeholder renders instead of a broken card, confirmed the Back button and Escape both
return to the main view with a fully clean URL (no trailing `#`), confirmed direct navigation to a
`#topic:` URL opens the right page on load, and confirmed clicking an embedded card's own
**topics** link from inside the topic page correctly exits back to the main view and filters the
topic table for that service — the "drill into the service, then into what else it touches" loop
the feedback asked for.

### 10.16 2026-07-19 (plan) — §10.1 transport-neutral `bindings`, revised and scoped down

§10.10's "explicitly not done" note deferred §10.1 because auto-deriving per-topic transport
bindings (queue name, Kafka topic, EventBridge bus, …) looked like it needed ~10 new per-transport
declaration attributes — a separate, unscoped design effort. Asked to plan it now, with a specific
steer: there's already transport-tagging machinery (`TransportMiddlewarePipeline`) — reuse or adapt
it instead of inventing new attributes, and stop hardcoding/duplicating the transport name string.
Investigating that machinery changes the shape of the plan substantially — it's smaller than §10.1
originally sketched, and it surfaces two real bugs worth fixing as part of the same change.

**What already exists (two separate mechanisms, easy to conflate — confirmed by reading both in
full):**

1. `TransportMiddlewarePipeline<TContext>` (`Benzene.Core.MessageHandlers.Info`) — a **runtime,
   per-invocation** decorator. Every transport `Application` class wraps its pipeline in one,
   passing a literal transport-name string (`"sqs"`, `"kafka"`, `"rabbitmq"`, …); at invocation
   time it calls `ISetCurrentTransport.SetTransport(name)` so `ICurrentTransport` (log
   enrichment) reports the right transport for *that message*. ~20 call sites, one per transport
   `Application` class.
2. `ITransportInfo`/`ITransportsInfo` (`Benzene.Abstractions.MessageHandlers.Info`,
   implementations in `Benzene.Core.MessageHandlers.Info`) — a **startup-time DI registry**,
   entirely separate from (1). Each transport package's own `DependencyInjectionExtensions.cs`
   registers `services.AddSingleton<ITransportInfo>(_ => new TransportInfo("<name>"))` as a side
   effect of that transport being configured; `TransportsInfo` aggregates every registered
   `ITransportInfo.Name` into a deduplicated `string[] Transports`. This is available at
   DI-container-build time — i.e. at spec-build time — with **no invocation needed**, which is
   exactly the shape a spec builder wants (specs are built once at startup, not per-message).

**Why the per-topic attribute design isn't needed.** Read every `UseMessageHandlers<TContext>(...)`
overload in `Benzene.Core.MessageHandlers/Extensions.cs` (7 overloads, lines 56–255) end to end:
none of them filter the handler set by transport. Whatever handlers are discovered/registered for
a pipeline are reachable by whatever transport that pipeline is wired to — transport reachability
is a **host-level** fact ("this process is wired to SQS and HTTP"), not a per-topic one. The one
genuine exception is HTTP, which already requires an explicit `[HttpEndpoint(method, path)]`
attribute per handler to get a route — and that's already captured correctly as per-topic
`httpMappings`. So the missing piece isn't "which topics does which transport carry" (no topic
opts out of a wired non-HTTP transport), it's just "which non-HTTP transports is this host wired
to at all" — a single document-level fact, sourced straight from `ITransportsInfo`, not a new
per-topic-per-transport declaration mechanism.

**Two real bugs found while tracing the ~20 call sites** (each transport package's own
`DependencyInjectionExtensions.cs`, grepped in full — `new TransportInfo(` and
`TransportMiddlewarePipeline<` side by side):

- **Several transports tag messages at runtime but never register `ITransportInfo` at startup**,
  so `ITransportsInfo.Transports` silently omits them today: `Benzene.RabbitMq` (tags
  `"rabbitmq"`), `Benzene.Azure.Function.BlobStorage` (`"blob-storage"`), `Benzene.Azure.CosmosDb`
  (`"cosmos-db"`), `Benzene.Azure.EventHub`'s consumer (`"event-hub"`), `Benzene.Azure.ServiceBus`'s
  consumer (`"service-bus"` — note `Benzene.Azure.Function.ServiceBus`, a *different* package, does
  register it), `Benzene.Aws.Sqs`'s self-host consumer (`"sqs"` — again, `Benzene.Aws.Lambda.Sqs`,
  a different package, does register it). A host built on any of these alone would build a
  `transports` list missing its own transport.
- **`"direct"` vs `"benzene"` drift**: `Benzene.Core.MessageHandlers/DI/Extensions.cs`'s
  `AddBenzeneMessage()` registers the startup `ITransportInfo` as `"direct"`, but
  `BenzeneMessageApplication` tags every runtime invocation as `"benzene"`
  (`TransportMiddlewarePipeline<BenzeneMessageContext>("benzene", pipeline)`). Same transport, two
  different names depending which mechanism you ask — exactly the kind of drift a shared constant
  would have caught at compile time.

**Revised plan:**

1. **Shared transport-name constants.** Add `TransportNames` (static class, one `const string` per
   transport — `Sqs = "sqs"`, `Kafka = "kafka"`, `RabbitMq = "rabbitmq"`, `Benzene = "benzene"`,
   …) to `Benzene.Abstractions.MessageHandlers.Info`, alongside `ITransportInfo`/`ITransportsInfo`
   — every package that has either call site already references this project (directly or
   transitively through `Benzene.Core.MessageHandlers`). Replace every literal transport-name
   string at both the `TransportMiddlewarePipeline<TContext>("...", ...)` call site and the
   `new TransportInfo("...")` call site with the matching constant, in the same ~20 packages. This
   is the concrete fix for "no longer hardcoded" / "shared" from the ask — one source of truth per
   transport name, referenced (not retyped) at every site that needs it, so the two mechanisms
   can never drift apart again the way `"direct"`/`"benzene"` did.
2. **Fix the coverage gap.** Add the missing `ITransportInfo` registrations found above (RabbitMq,
   BlobStorage, CosmosDb, Azure.EventHub consumer, Azure.ServiceBus consumer, Aws.Sqs self-host
   consumer) to each package's `DependencyInjectionExtensions.cs`, using the new constants. Resolve
   the `"direct"`/`"benzene"` drift by renaming `AddBenzeneMessage()`'s registration to
   `TransportNames.Benzene` (matching the runtime tag, which is the name that actually shows up in
   logs today — `"direct"` was never observable at runtime, so nothing depends on that string).
   Without this step, the new spec field in (3) would be built on the same incomplete data these
   bugs already cause — worth fixing regardless of (3), but load-bearing for it.
3. **Document-level `transports` field on the `benzene` spec.** Add `string[] Transports` to
   `EventServiceDocument` (top-level, sibling to the existing `messageEndpoint` — not per-topic;
   §10.1's per-topic shape isn't needed per the reasoning above), written only when non-empty
   (same "omit rather than emit an empty array" convention `Version`/`httpMappings` already
   follow). Add a new `IConsumesTransportsInfo<TBuilder>` seam to `Benzene.Schema.OpenApi.Abstractions`
   (same shape as `IConsumesApplicationInfo` etc.), implemented by `EventServiceDocumentBuilder` via
   a new `AddTransportsInfo(ITransportsInfo)` method, and wired into `SpecBuilder` alongside the
   other `IConsumes*` resolutions. `EventServiceDocumentDeserializer` reads it back, matching every
   other field's round-trip test pattern (see `EventServiceDocumentBuilderTest`'s existing
   `Version`/`reserved` round-trip tests for the pattern to follow).
4. **Per-topic reachability stays derived, not duplicated.** A topic's *effective* non-HTTP
   reachability is "every entry in the document-level `transports` list" (any wired transport can
   reach any registered handler, per the `UseMessageHandlers` finding above); its HTTP reachability
   is its own `httpMappings`, unchanged. Nothing new is needed per-topic — this is a reading, not a
   schema change, so it's computed wherever it's displayed (mesh-ui.html, spec-ui.html) rather than
   stored redundantly on every topic.
5. **UI.** `mesh-ui.html`: render the service's `transports` list as a chip row on its service
   card (replacing nothing — this is new information, additive to the existing status/drift
   badges), and on the topic page (§10.15) show, per producer/consumer service section, that
   service's transport chips inline — answering "how would I actually reach this topic on this
   service" without inventing a new per-topic UI element. `spec-ui.html` gets the same chip row
   wherever it already surfaces service-level info (near `messageEndpoint`), replacing the
   HTTP-verb-badge-as-topic-decoration pattern the original §10.1 complaint was about — HTTP
   becomes one chip among several instead of the implicit default.
6. **Docs.** `docs/specification/wire-contracts.md` (or wherever the `benzene` EventServiceDocument
   shape is documented) gains the `transports` field; `docs/mesh-ui.md` gains a line on the chip
   row, matching the existing pattern for every other field documented there.

**Explicitly not doing (still correctly out of scope):** per-topic per-transport binding detail
(queue name, Kafka topic, EventBridge bus/detail-type, ARN, …) — the original §10.1 sketch's
`{ "transport": "sqs", "queue": "orders-queue" }` shape. Nothing in this plan captures *which*
queue/topic/bus a transport uses, only *that* the host is wired to it. That remains a real gap for
someone trying to go find the actual queue, but it's a materially different, larger effort (new
capture points inside each transport's own configuration, not just a name) and nothing in §10.3's
shipped producer/consumer/version work or this plan depends on it — deferring it again is a clean
cut, not a compromise forced by running out of design space.

**Sequencing:** (1) and (2) first — pure C#, no wire-format change, immediately testable, and (2)'s
bug fixes are worth doing on their own merits. Then (3)+(4) (spec field + builder seam + round-trip
test). Then (5) (UI) verified the same real-browser way as every other UI increment this arc. (6)
last, once the shape is settled.

### 10.17 2026-07-19 (implementation) — §10.16 steps 1–2 shipped

Both pure-C# steps, no wire-format change:

- **`TransportNames`** (`Benzene.Abstractions.MessageHandlers.Info`) — one `const string` per
  transport. Every `TransportMiddlewarePipeline<TContext>("...", ...)` call site and every
  `new TransportInfo("...")` DI registration across all ~20 transport packages now references the
  constant instead of retyping the literal — the two mechanisms can no longer drift apart into two
  names for the same transport.
- **Closed the `ITransportInfo` coverage gap** found while tracing those call sites: `Benzene.RabbitMq`,
  `Benzene.Azure.ServiceBus` (consumer), `Benzene.Azure.EventHub` (consumer),
  `Benzene.Azure.Function.EventHub`, `Benzene.Azure.Function.BlobStorage`, `Benzene.Azure.CosmosDb`,
  and `Benzene.Aws.Sqs` (self-host consumer) all tagged messages at runtime but never registered a
  startup `ITransportInfo` — so `ITransportsInfo.Transports` silently omitted them. Each now
  registers one, via a new `AddAzureBlobStorage()`/`AddCosmosDbChangeFeed()` DI method for the two
  packages (`Benzene.Azure.Function.BlobStorage`, `Benzene.Azure.CosmosDb`) that had no DI
  registration point of their own to add it to, called automatically by `UseBlobStorage`/
  `UseCosmosDbChangeFeed` respectively.
- **Fixed the `"direct"`/`"benzene"` drift**: `AddBenzeneMessage()`'s `ITransportInfo` registration
  now says `TransportNames.Benzene`, matching what `BenzeneMessageApplication` actually tags at
  runtime.

Full solution build (0 errors) and the complete `Benzene.Core.Test` suite (1530 tests) pass.
Step 3 (the document-level `transports` spec field) and onward are still open, per §10.16's
sequencing.

### 10.18 2026-07-19 (implementation) — §10.16 steps 3-6 shipped: `transports` end to end

The document-level `transports` field is now live from spec build through to both UIs:

- **Spec (`Benzene.Schema.OpenApi`).** `EventServiceDocument.Transports` (`string[]`, written only
  when non-empty, same "omit rather than emit empty" convention as `Version`/`httpMappings`). A new
  `IConsumesTransportsInfo<TBuilder>` seam (mirroring `IConsumesMessageEndpoint`) is implemented by
  `EventServiceDocumentBuilder.AddTransportsInfo(ITransportsInfo)` and wired into `SpecBuilder`
  alongside the other `IConsumes*` resolutions, resolving `ITransportsInfo` from DI - the registry
  §10.17 just finished closing the coverage gap on. `EventServiceDocumentDeserializer` round-trips
  it. Documented in `docs/spec.md`'s new "Transport advertisement" section, right after the
  existing "Message endpoint advertisement" one it mirrors.
- **Aggregator (`Benzene.Mesh.Aggregator`/`Benzene.Mesh.Contracts`).** `MeshManifestEntry` gained
  a trailing, additive `Transports` (empty default - source-compatible). A new `ParseTransports`
  (mirrors `ParseTopics`/`ParseOutboundTopics`'s best-effort JSON parsing, never fails the run on a
  missing/unparseable spec) reads it out of each service's spec during `BuildServiceAsync` and
  denormalizes it onto the manifest entry, the same treatment §10.10 gave `OwningTeam` - so
  `mesh-ui.html` can render it without an extra per-service fetch.
- **UI.** `mesh-ui.html`: each service card gets a `.svc-transports` chip row (hidden entirely when
  empty - most of a fixture's cards in local testing correctly showed none). Because the topic
  page (§10.15) embeds the *real* `buildServiceCard(svc)` for every producer/consumer, the same
  chip row appears there automatically, no separate code path - verified end to end with a
  two-service Playwright fixture (one with three transports, one with none): main-list chips
  correct, no-transports card correctly renders no row, and the topic page's embedded cards showed
  exactly the expected chips. `spec-ui.html` gets a parallel `#lede-transports` chip row under the
  title/description, replacing nothing - verified the same way against the embedded sample spec.
  Both embedded fallback samples (`mesh-ui.html`'s sample manifest, `spec-ui.html`'s sample spec)
  now include example `transports` data so the standalone/offline demo shows the feature without
  needing a live backend.
- **Docs.** `docs/spec.md` (the actual home of the `benzene` `EventServiceDocument` field
  reference - not `docs/specification/wire-contracts.md`, which turned out not to document this
  format at all) gains "Transport advertisement"; `docs/mesh-ui.md` gains the chip row in both the
  main service-card description and the topic-page producer/consumer section. Every touched
  package's own `CLAUDE.md` (`Benzene.Schema.OpenApi`, `Benzene.Mesh.Contracts`,
  `Benzene.Mesh.Aggregator`, `Benzene.Mesh.Ui`, `Benzene.Spec.Ui`) updated to match, per this
  repo's per-package doc-truth convention.

Full solution build (0 errors) and the complete `Benzene.Core.Test` (1531 tests) and
`Benzene.Mesh.Test` (134 tests) suites pass. §10.16's plan is now fully implemented - the only
remaining, deliberately out-of-scope item is per-topic per-transport binding detail (queue names,
Kafka topics, etc.), unchanged from §10.16's "explicitly not doing" call.
