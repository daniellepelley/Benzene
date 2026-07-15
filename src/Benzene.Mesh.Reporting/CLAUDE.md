# Benzene.Mesh.Reporting

## What this package does
The push/self-report half of the mesh's multi-transport data collection work (Phase C of
`work/service-mesh-roadmap-1.0.md`'s 2026-07-15 updates) - for services with **no** synchronous
entry point an `IMeshServiceSource` (`Benzene.Mesh.Aggregator`) could poll at all, e.g. an AWS
Lambda whose only event source is SQS/SNS/EventBridge. Such a service instead reports its own
spec/health, opportunistically, as a side effect of real traffic.

## Key types/interfaces
- `HttpMeshReportPublisher : Benzene.Mesh.Contracts.IMeshReportPublisher` - POSTs a
  `MeshServiceReport` as JSON to `MeshReportingOptions.IngestionUrl` (an aggregator's
  `Benzene.Mesh.Aggregator.MeshReportMessageHandler` endpoint). Use when the reporter isn't
  colocated with the aggregator's own artifact storage.
- `MeshSelfReportMiddleware<TContext> : IMiddleware<TContext>` - transport-agnostic; after a real
  request/message completes (`await next()`), opportunistically fires a background publish via
  whichever `IMeshReportPublisher` is registered, throttled by `MeshSelfReportOptions.MinimumInterval`
  (default 5 minutes) via the singleton `MeshSelfReportState`. Fully best-effort: never blocks the
  response, never propagates a publish/provider failure - this is side-channel telemetry, not part
  of the primary request/message path.
- `MeshSelfReportOptions(serviceName, specProvider, healthProvider, minimumInterval?)` - deliberately
  takes spec/health as `Func<Task<string?>>`/`Func<Task<HealthCheckResponse?>>` delegates rather
  than this package generating them itself. A self-reporting service already knows how to produce
  its own spec (`Benzene.Schema.OpenApi`) and health (`Benzene.HealthChecks`) - depending on either
  package here would work against the goal of staying light enough to link into an arbitrary
  monitored service.
- `Extensions.AddMeshHttpReporting(options)` - registers `HttpMeshReportPublisher` as the
  `IMeshReportPublisher`. `Extensions.AddMeshSelfReport(options)` - registers
  `MeshSelfReportOptions`/`MeshSelfReportState`. `Extensions.UseMeshSelfReport<TContext>()` - adds
  the middleware to a pipeline. All three are separate calls (matching the established
  `Add*`-registers/`Use*`-wires split elsewhere in Benzene, e.g. `UseHealthCheck`) - none of them
  registers an `IMeshReportPublisher` implicitly for you if you didn't call `AddMeshHttpReporting`
  or already have `Benzene.Mesh.Aggregator`'s default (`ArtifactStoreMeshReportPublisher`)
  registered; resolving the middleware without one fails at DI-resolution time.

## No scheduled/cron reporting - deliberate, not an oversight
The maintainer's explicit call: a Lambda "only really needs to report if it's running," and a
scheduled/keep-warm reporter (e.g. a cron Lambda invoking this one just to make it report) would
defeat the cost benefit of on-demand billing. So v1 is opportunistic-only - report as a side effect
of real traffic, never proactively. A scheduled option is parked as a documented future possibility
(`work/service-mesh-roadmap-1.0.md`), not built here.

## Known gap - staleness has no representation yet
An idle self-reporting service's mesh entry just gets older with no signal that it's stale -
`Benzene.Mesh.Contracts.MeshServiceStatus` has no `Stale` value today (only
`Healthy`/`Unhealthy`/`Unreachable`). This is the accepted tradeoff of opportunistic-only reporting,
flagged explicitly rather than silently accepted - a real follow-up, not solved by this package.

## When to use this package
- A monitored service has **no** synchronous entry point at all (SQS/SNS/EventBridge-only Lambda,
  or any other genuinely response-less transport) - `Benzene.Mesh.Aws.Lambda`'s pull-based source
  cannot reach it under any circumstances, since there is no `Invoke` that would return anything.
  Wire `AddMeshHttpReporting(...)` (or point directly at a colocated
  `Benzene.Mesh.Aggregator.ArtifactStoreMeshReportPublisher`), `AddMeshSelfReport(...)`, and
  `.UseMeshSelfReport()` into the service's own pipeline.
- Not for services that DO have some synchronous entry point (HTTP, API Gateway, Function URL, or a
  direct Lambda `Invoke`) - those should be pulled via `HttpMeshServiceSource`/
  `Benzene.Mesh.Aws.Lambda.LambdaMeshServiceSource` instead; invoking a Lambda synchronously causes
  a cold start rather than requiring one already be warm, so "it's on-demand" doesn't by itself
  justify push over pull (see the roadmap's 2026-07-15 design note).

## Dependencies on other Benzene packages
- **Benzene.Mesh.Contracts** - `MeshServiceReport`/`IMeshReportPublisher`.
- **Benzene.Abstractions** - `IBenzeneServiceContainer`/`IServiceResolver`.
- **Benzene.Abstractions.Middleware** - `IMiddleware<TContext>`/`IMiddlewarePipelineBuilder<TContext>`.
- Deliberately **not** `Benzene.Mesh.Aggregator` (no `IMeshArtifactStore`/HTTP-transport/message-handler
  infrastructure needed here) and **not** `Benzene.Schema.OpenApi`/`Benzene.HealthChecks` (spec/health
  come in via delegates, not generated by this package).
