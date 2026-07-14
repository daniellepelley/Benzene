# Benzene Service Mesh Example

Demonstrates Benzene's service-mesh visibility feature end to end: three small
demo services, a real Benzene app that aggregates their specs and health into
a catalog, and a dashboard that renders it.

## What this shows

- `Benzene.Mesh.Aggregator.MeshAggregator` polling each demo service's `/spec`
  and `/healthcheck` endpoints, hashing each spec to detect **contract drift**
  against the previous run, and publishing `manifest.json`/`services/*.json`
  to disk (`Benzene.Mesh.Aggregator.FileSystemMeshArtifactStore`).
- The aggregator is a **real, dogfooded Benzene app** - triggering a run is
  `POST /mesh/aggregate`, a `[Message("mesh:aggregate")]`/`[HttpEndpoint("POST",
  "/mesh/aggregate")]` handler like any other, not a bespoke CLI tool.
- `Benzene.Mesh.Ui.UseMeshUi()` serving the dashboard directly from the
  aggregator host - the "aggregator self-serves its own dashboard" case
  described in `Benzene.Mesh.Ui`'s own `CLAUDE.md`.
- The four states the dashboard renders: **healthy**, **unhealthy**,
  **unreachable**, and **contract drift** - all reachable from this example
  without faking any data.
- `Benzene.HealthChecks.Core.HealthCheckDependency` (health-check result
  metadata) rendering as a dependency chip in the dashboard's expanded
  per-service detail view.

See [`work/service-mesh-roadmap-1.0.md`](../../work/service-mesh-roadmap-1.0.md)
for the full design.

## Run it

```bash
cd examples/Mesh
./run.sh
```

`run.sh` starts `Benzene.Examples.Mesh.OrdersService` (port 5310),
`Benzene.Examples.Mesh.PaymentsService` (port 5311), and
`Benzene.Examples.Mesh.Aggregator` (port 5300) in the background, waits for
them to come up, triggers one aggregation run, and prints the URLs below.
`Benzene.Examples.Mesh.ShippingService` (port 5312) is **deliberately not
started** - see [Try it](#try-it).

- Mesh Explorer dashboard: http://localhost:5300/mesh-ui
- Raw manifest: http://localhost:5300/artifacts/manifest.json
- Orders spec: http://localhost:5310/spec?type=benzene
- Payments spec: http://localhost:5311/spec?type=benzene

Press Ctrl+C to stop everything `run.sh` started.

## Try it

Open the dashboard (http://localhost:5300/mesh-ui) after running `./run.sh`.
You'll see `orders-api` and `payments-api` healthy, and `shipping-api`
unreachable (nothing is listening on port 5312 yet). From there:

**See "unreachable" become "healthy"** - start the missing service, then
re-trigger a run:

```bash
dotnet run --project Benzene.Examples.Mesh.ShippingService --urls http://localhost:5312
curl -X POST http://localhost:5300/mesh/aggregate
```

Reload the dashboard (or click into the manifest URL again) to see
`shipping-api` flip to healthy.

**See "unhealthy"** - stop Payments (Ctrl+C in whichever terminal it's
running in, or kill its process), then restart it with `DEMO_UNHEALTHY=true`:

```bash
DEMO_UNHEALTHY=true dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311
curl -X POST http://localhost:5300/mesh/aggregate
```

`payments-api` now reports unhealthy, with a `payments-gateway` dependency
chip visible in its expanded detail view either way (see
`PaymentsGatewayHealthCheck`).

**See "contract drift"** - stop Payments again and restart it with
`DEMO_ADD_ENDPOINT=true` instead, which adds a `GET /payments/{id}/refunds`
operation to its spec:

```bash
DEMO_ADD_ENDPOINT=true dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311
curl -X POST http://localhost:5300/mesh/aggregate
```

The spec's hash now differs from the previous run's, so `payments-api` shows
a "drift" badge - a genuine, not simulated, contract change. Drift always
compares against the immediately preceding run, so:
- Re-aggregate again *without* restarting Payments (spec unchanged since the
  last run) and the badge clears.
- Restart Payments again *without* `DEMO_ADD_ENDPOINT` and re-aggregate: the
  spec changes back, so drift reappears for that one run, then clears again
  on the next unchanged run.

## What to look for

- `manifest.json`'s `services[].status` / `contractDrift` fields, and each
  `services/{name}.json`'s `health.healthChecks` map (with `dependencies`) -
  this is exactly what `Benzene.Mesh.Ui`'s `mesh-ui.html` fetches and renders,
  nothing hidden behind extra transformation.
- `Benzene.Examples.Mesh.PaymentsService/HealthChecks/PaymentsGatewayHealthCheck.cs`
  for a minimal real `IHealthCheck` reporting a dependency.
  `Benzene.Examples.Mesh.PaymentsService/Startup.cs` for the manual,
  env-var-gated `IHttpEndpointDefinition` registration that drives the
  contract-drift demo (mirrors how `SpecMessageHandler` itself is registered,
  since reflection-based handler discovery can't be toggled off at runtime).
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
