# Benzene.Mesh.Tracing.Tempo

## What this package does
Queries Grafana Tempo's metrics-generator service-graph metrics via a Prometheus-compatible HTTP
API and publishes the result as `topology.json` - Phase 3 of `work/service-mesh-roadmap-1.0.md`
("live trace integration"). This is a PromQL client, **not** a Tempo trace-API client: Tempo does
not expose a "give me the service graph" REST endpoint itself. When its `metrics-generator`'s
`service-graphs` processor is enabled, it derives Prometheus metrics from spans as they're
ingested and remote-writes them to a Prometheus-compatible store - this package queries that store
(see §4.6.1 in the roadmap doc for the full reasoning). Ships as a genuine Benzene application, the
same dogfooding shape as `Benzene.Mesh.Aggregator`: `TempoTopologyMessageHandler` exposes a run as
a `[Message("mesh:topology")]`/`[HttpEndpoint("POST", "/mesh/topology")]` handler.

## Key types/interfaces
- `PrometheusQueryClient.QueryAsync(prometheusUrl, promQl)` - runs an instant PromQL query
  (`GET {prometheusUrl}?query=...`) and parses Prometheus's standard
  `{"status":"success","data":{"result":[{"metric":{...},"value":[ts,"val"]}]}}` vector response
  into `PrometheusSample`s. A `"status":"error"` body or malformed JSON surfaces as an empty result
  rather than throwing - matches `Benzene.Mesh.Aggregator.MeshAggregator`'s existing "one bad fetch
  shouldn't block the rest" philosophy. Only a genuine connection-level failure (DNS, refused
  connection, timeout) still throws.
- `TempoServiceGraphTopologyBuilder.BuildAsync()` - runs 5 PromQL queries against the 3 documented
  service-graph metrics (`traces_service_graph_request_total`,
  `traces_service_graph_request_failed_total`, `traces_service_graph_request_server_seconds_bucket`
  for p50/p95/p99 via `histogram_quantile`), joins the results by `(client, server)` label pair
  into `Benzene.Mesh.Contracts.TopologyEdge[]`. Error rate is computed **client-side**
  (`failedPerMinute / requestsPerMinute`, guarded to `0` on divide-by-zero) rather than as a 6th
  PromQL query, sidestepping Prometheus's own NaN-on-zero-division semantics.
- `TempoTopologyOptions(prometheusUrl, timeWindow = 5 minutes)` - where and over what `rate(...)`
  lookback window to query.
- `TempoTopologyMessageHandler` - thin `IMessageHandler<Void, MeshTopology>` wrapper resolving
  `TempoServiceGraphTopologyBuilder` + `IMeshArtifactStore` from DI, building, and publishing
  `topology.json`.
- `Extensions.AddTempoTopology(options)` - registers `TempoTopologyOptions`, `HttpClient`,
  `PrometheusQueryClient`, `TempoServiceGraphTopologyBuilder`. **Deliberately does not register an
  `IMeshArtifactStore`** - requires `Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator(...)` to
  already be registered in the same container, so `topology.json` lands in the same artifact
  directory as `manifest.json`/`services/*.json`. Calling `AddTempoTopology` without that
  prerequisite means `TempoTopologyMessageHandler` fails to resolve `IMeshArtifactStore` at
  DI-resolution time - the same way any Benzene wiring with a missing prerequisite registration
  behaves, not a special case worth guarding against here.

## Important semantics
- **An edge only appears if it has at least one matching timeseries in the queried window** - real
  Prometheus semantics, not a bug. A `client`/`server` pair with zero traffic in the window
  produces no timeseries at all (Prometheus doesn't emit "0" samples for label combinations that
  never occurred), so there is nothing to build an edge from. This means `topology.json`'s
  tempo-sourced edges are inherently "pairs that had traffic in the queried window" - a
  structural-only edge (once that source exists, see `TopologyEdgeSource.Structural`) with no
  matching tempo edge is itself a useful signal (dead code path, or just no traffic in-window).
- Adds p50/p99 latency alongside p95 - the roadmap's own `topology.json` JSON sample (§7.3) only
  shows `p95LatencyMs`, but all three come from the same histogram query for free, so this package
  reports all three as a deliberate, additive enrichment beyond that minimal sample.

## Verification status - real caveat, not just a formality
This package's PromQL construction, response parsing, and edge-joining logic are covered by
thorough mocked-HTTP tests (`test/Benzene.Mesh.Test/PrometheusQueryClientTest.cs`,
`TempoServiceGraphTopologyBuilderTest.cs`, `TempoTopologyMessageHandlerTest.cs`) against the
documented Prometheus API response shape and the 3 named service-graph metrics. **It has not been
verified against a real running Tempo instance** - that was attempted while building this package
but blocked by this dev environment's own network egress policy (Docker Hub image pulls and
direct GitHub release downloads both returned `403`/`Forbidden`), not a Benzene-side limitation.
The roadmap's own original caveat - "exact label/metric names should be confirmed against the
specific Tempo/Grafana version in use before implementation" - therefore still stands. Treat the
metric names (`traces_service_graph_request_total`/`..._failed_total`/
`..._request_server_seconds_bucket`) and label names (`client`/`server`) as "per Tempo's public
documentation, not independently confirmed" until someone verifies this against a live instance.

## When to use this package
- Any Benzene solution that already has a real Tempo deployment with the metrics-generator's
  `service-graphs` processor enabled and a Prometheus-compatible remote-write target - this is an
  operator/infra prerequisite Benzene's adapter cannot set up for them (see roadmap §4.6.1). Wire
  `AddMeshAggregator(...)` then `AddTempoTopology(...)` into the same host, and trigger
  `mesh:topology` however fits, the same way `mesh:aggregate` is triggered.

## Dependencies on other Benzene packages
- **Benzene.Mesh.Contracts** - `MeshTopology`/`TopologyEdge`/`TopologyEdgeSource` shapes.
- **Benzene.Mesh.Aggregator** - `IMeshArtifactStore`, so `topology.json` publishes to the same
  store `manifest.json`/`services/*.json` do. This is a deliberate deviation from the roadmap's
  original §8 package table (which had this package depending only on `Contracts`) - the port
  ended up living in `Aggregator` during Phase 1a rather than `Contracts`, and moving it now purely
  for dependency-graph tidiness wasn't judged worth the churn to already-shipped code.
- **Benzene.Abstractions.MessageHandlers**, **Benzene.Core.MessageHandlers**, **Benzene.Http** -
  for `IMessageHandler<,>`, `[Message]`, and `[HttpEndpoint]` respectively.

## Important conventions
- Uses `System.Text.Json` throughout, matching every other package in the `Benzene.Mesh.*` family.
- No Mesh UI rendering of `topology.json` yet - deliberately out of scope for this pass (matches
  how `Benzene.Mesh.Ui`'s own catalog-only build deferred all graph/topology rendering); the data
  is real and published, just not yet surfaced visually.
