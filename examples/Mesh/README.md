# Benzene Service Mesh Example

Demonstrates Benzene's service-mesh visibility feature end to end: three small
demo services, a real Benzene app that aggregates their specs and health into
a catalog, and a dashboard that renders it.

## What this shows

A single `./run.sh` drives the dashboard into **every state, badge, stat, and
per-check status at once** - no manual steps needed to see them:

- `orders-api` **healthy**, with three real health checks
  (`PostgresDatabase`, `RedisCache`, `SqsQueue`), each carrying a
  `Benzene.HealthChecks.Core.HealthCheckDependency` that renders as a
  dependency chip in the expanded detail view.
- `payments-api` **unhealthy AND contract-drift** - its `PaymentsGateway`
  check reports **failed** (gateway down) by default, a `FraudEngine` check
  reports a **warning** (degraded, amber badge), and a `PostgresDatabase`
  check reports **ok** - so the drill-down shows all three per-check statuses
  and their dependency chips side by side. `run.sh` also restarts it with a
  changed spec between two aggregation runs, so it earns a **drift** badge.
- `shipping-api` **unreachable** - deliberately never started, so it shows an
  `error` line instead of health-check detail.

Behind the dashboard:

- `Benzene.Mesh.Aggregator.MeshAggregator` polls each demo service's `/spec`
  and `/healthcheck` endpoints, hashes each spec to detect **contract drift**
  against the previous run, and publishes `manifest.json`/`services/*.json`
  to disk (`Benzene.Mesh.Aggregator.FileSystemMeshArtifactStore`).
- The aggregator is a **real, dogfooded Benzene app** - triggering a run is
  `POST /mesh/aggregate`, a `[Message("mesh:aggregate")]`/`[HttpEndpoint("POST",
  "/mesh/aggregate")]` handler like any other, not a bespoke CLI tool.
- `Benzene.Mesh.Ui.UseMeshUi()` serves the dashboard directly from the
  aggregator host - the "aggregator self-serves its own dashboard" case
  described in `Benzene.Mesh.Ui`'s own `CLAUDE.md`.
- `Benzene.Mesh.Tracing.Tempo.AddTempoTopology()` queries a **bundled fake
  Prometheus endpoint** (`/fake-prometheus/api/v1/query`, implemented in
  `Benzene.Examples.Mesh.Aggregator/FakePrometheus.cs`) instead of a real
  Tempo/Prometheus stack, publishing `topology.json` alongside
  `manifest.json`/`services/*.json`. This is deliberate: a real Tempo +
  Prometheus stack needs Docker and network egress this environment doesn't
  reliably have (see `work/service-mesh-roadmap-1.0.md`'s Phase 3 notes), and
  it keeps `./run.sh` fully self-contained - the same reason the rest of this
  example already fakes health/spec data deterministically rather than
  calling real external services. The Mesh UI renders `topology.json` as a
  sortable edge table (client, server, source, req/min, error rate,
  p50/p95/p99 latency) - see `Benzene.Mesh.Ui/CLAUDE.md`.

  `FakePrometheus.cs` returns canned data for three edges, each illustrating
  something different:

  | Edge | Req/min | Error rate | Latency (p50/p95/p99) | What it shows |
  |---|---|---|---|---|
  | orders-api → payments-api | 86.4 | 18% | 45/420/890ms | High traffic, elevated errors and latency - **echoes payments-api's `unhealthy` badge**, the same story confirmed two different ways (health check + observed traffic). |
  | orders-api → shipping-api | 24.1 | 0.4% | 12/35/58ms | Healthy-looking traffic to a service that's **unreachable right now** (shipping-api isn't started by default) - topology data is an independent signal from live health, not a replacement for it. |
  | payments-api → shipping-api | 6.2 | 0% (no failed sample at all) | 8/15/22ms | Low, clean traffic. The failed-request sample is omitted entirely rather than reported as `0` - real Prometheus never emits a `rate()` sample for a metric that's never incremented. |

See [`work/service-mesh-roadmap-1.0.md`](../../work/service-mesh-roadmap-1.0.md)
for the full design.

## Run it

```bash
cd examples/Mesh
./run.sh
```

`run.sh` starts `Benzene.Examples.Mesh.OrdersService` (port 5310),
`Benzene.Examples.Mesh.PaymentsService` (port 5311, **unhealthy by default**),
and `Benzene.Examples.Mesh.Aggregator` (port 5300) in the background, waits for
them to come up, then runs **two** aggregation passes to make contract drift
visible automatically (see below). It prints the URLs at the end.
`Benzene.Examples.Mesh.ShippingService` (port 5312) is **deliberately not
started** - see [Try it](#try-it).

- Mesh Explorer dashboard: http://localhost:5300/mesh-ui
- Raw manifest: http://localhost:5300/artifacts/manifest.json
- Raw topology: http://localhost:5300/artifacts/topology.json
- Orders spec: http://localhost:5310/spec?type=benzene
- Payments spec: http://localhost:5311/spec?type=benzene

Readiness polling targets `/spec?type=benzene` (which always returns 200), not
`/healthcheck` - payments-api is unhealthy by default and its `/healthcheck`
returns HTTP 503, which `curl -f` treats as a failure.

**How the two-run drift automation works:** `run.sh` aggregates once for a
baseline, then kills payments-api, waits for its port to stop responding,
restarts it with `DEMO_ADD_ENDPOINT=true` (which adds a `GET
/payments/{id}/refunds` operation to its spec), waits for it to come back, and
aggregates a second time. The spec's hash now differs from the baseline run's,
so `payments-api` earns a genuine - not simulated - drift badge on that second
pass. Drift always compares against the immediately preceding run.

Press Ctrl+C to stop everything `run.sh` started (including the restarted
payments-api process).

## Try it

Open the dashboard (http://localhost:5300/mesh-ui) after running `./run.sh`.
Out of the box you'll see `orders-api` healthy, `payments-api` unhealthy with a
drift badge, and `shipping-api` unreachable. Expand each card to see its
health-check detail. From there:

**See "unreachable" become "healthy"** - start the missing service, then
re-trigger a run:

```bash
dotnet run --project Benzene.Examples.Mesh.ShippingService --urls http://localhost:5312
curl -X POST http://localhost:5300/mesh/aggregate
```

Reload the dashboard (or click into the manifest URL again) to see
`shipping-api` flip to healthy, with `CarrierApi` and `SqsQueue` checks and
their `fedex-api`/`shipment-events` dependency chips.

**See "unhealthy" become "healthy"** - stop Payments (Ctrl+C in whichever
terminal it's running in, or kill its process), then restart it with
`DEMO_PAYMENTS_HEALTHY=true`:

```bash
DEMO_PAYMENTS_HEALTHY=true dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311
curl -X POST http://localhost:5300/mesh/aggregate
```

`payments-api` now reports healthy - the `PaymentsGateway` check flips to ok,
the `FraudEngine` warning and `PostgresDatabase` ok checks stay as they were,
and the `stripe-gateway` dependency chip is visible in its expanded detail view
either way (see `PaymentsGatewayHealthCheck`).

**Re-run the drift demo by hand** - stop Payments and restart it *without*
`DEMO_ADD_ENDPOINT` and re-aggregate: the spec changes back, so drift reappears
for that one run, then clears again on the next unchanged run. Drift always
compares against the immediately preceding run, so re-aggregating without
restarting Payments (spec unchanged since the last run) clears the badge.

## What to look for

- `manifest.json`'s `services[].status` / `contractDrift` fields, and each
  `services/{name}.json`'s `health.healthChecks` map (with `dependencies`) -
  this is exactly what `Benzene.Mesh.Ui`'s `mesh-ui.html` fetches and renders,
  nothing hidden behind extra transformation.
- `topology.json`'s `edges[]` (client, server, source, requestsPerMinute,
  errorRate, p50/p95/p99LatencyMs) - published by `TempoTopologyMessageHandler`
  from the canned data in `Benzene.Examples.Mesh.Aggregator/FakePrometheus.cs`,
  rendered as the Mesh UI's sortable Topology table.
- `Benzene.Examples.Mesh.OrdersService/HealthChecks/OrdersHealthChecks.cs` for
  three healthy `IHealthCheck`s, each reporting a distinct dependency kind
  (`Database`/`Cache`/`Queue`).
- `Benzene.Examples.Mesh.PaymentsService/HealthChecks/PaymentsGatewayHealthCheck.cs`
  (failed by default, ok when `DEMO_PAYMENTS_HEALTHY=true`) and
  `.../HealthChecks/PaymentsHealthChecks.cs` (`PaymentsDatabaseHealthCheck` ok,
  `FraudEngineHealthCheck` a `CreateWarning` degraded check) - together they
  exercise all three per-check statuses in one service.
  `Benzene.Examples.Mesh.PaymentsService/Startup.cs` for the manual,
  env-var-gated `IHttpEndpointDefinition` registration (`DEMO_ADD_ENDPOINT`)
  that drives the contract-drift demo (mirrors how `SpecMessageHandler` itself
  is registered, since reflection-based handler discovery can't be toggled off
  at runtime).
- `Benzene.Examples.Mesh.ShippingService/HealthChecks/ShippingHealthChecks.cs`
  for the checks it would report if you started it manually.
- `Benzene.Examples.Mesh.Aggregator/Startup.cs` for the whole wiring: three
  lines (`AddMeshAggregator`, a static-file mount at `/artifacts`, and
  `UseMeshUi`) turn a plain ASP.NET Core app into a self-serving mesh
  dashboard.

## Notes

- Every demo service's `/spec` and `/healthcheck` are registered manually via
  `IHttpEndpointDefinition`/`IMessageHandlerDefinition` (the same pattern
  `examples/Asp/Benzene.Example.Asp/Startup.cs` uses for `/spec`) - neither
  `SpecMessageHandler` nor the health-check middleware carry `[HttpEndpoint]`
  attributes, so they aren't picked up by reflection-based discovery on their
  own.
- The aggregator's artifact directory (`mesh-artifacts/`, next to its build
  output) is created on first run and persists between runs, which is what
  makes contract-drift detection meaningful - it's always comparing against
  the previous run's actual output, not a clean slate.
