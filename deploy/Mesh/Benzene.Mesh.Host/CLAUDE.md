# Benzene.Mesh.Host

## What this package does
A config-driven, Docker/Compose-deployable Benzene Mesh Aggregator + UI - Phase D of
`work/service-mesh-roadmap-1.0.md`'s multi-transport data collection work. Lets a developer run the
mesh dashboard against their own real services during local development (`docker-compose up`
alongside their other infra), rather than only against `examples/Mesh/`'s demo/fake data. See
`../README.md` for the config shape, Docker/Compose usage, and publishing.

## Key types
- `Startup` - mirrors `examples/Mesh/Benzene.Examples.Mesh.Aggregator/Startup.cs`'s wiring shape
  (`AddMeshAggregator`, static-file mount at `/artifacts`, `UseMeshUi` + `UseMeshSpecUi` so the
  per-service "spec" drill-in resolves, `.UseMessageHandlers()`),
  but binds `MeshHostConfig` from `IConfiguration` (constructor-injected) instead of a hardcoded
  static registry, and additionally wires `AddMeshLambdaSource()` and a `MeshPollBackgroundService`
  hosted service.
- `MeshHostConfig`/`MeshHostServiceConfig` - the `mesh.json` binding shape (mutable properties, for
  `IConfiguration.Get<T>()` - this repo's first use of that pattern to bind a *list* of objects, see
  `work/service-mesh-roadmap-1.0.md`'s Phase D note). `MeshHostServiceConfig.ToEntry()` converts to
  the immutable `Benzene.Mesh.Contracts.MeshServiceRegistryEntry` the rest of the mesh feature uses.
- `MeshPollBackgroundService : BackgroundService` - runs `MeshAggregator.RunOnceAsync` on a timer
  (`MeshHostConfig.PollIntervalSeconds`) - new capability local to this Host app only, since a bare
  Compose deployment has no external scheduler the way a real deployment's `mesh:aggregate`
  invocation-trigger assumes. A failed pass is logged and does not stop future passes or crash the
  host.
- `Program.cs` - `Host.CreateDefaultBuilder(args)` with an extra `ConfigureAppConfiguration` step
  that loads `MESH_CONFIG_PATH` (if set) as an additional JSON config source, layered on top of
  `Host.CreateDefaultBuilder`'s own default sources (env vars, etc.).

## Deviations from the original design sketch
- **No `selfReportIngestion.enabled` config toggle.** The original design considered gating whether
  the push ingestion endpoint (`Benzene.Mesh.Aggregator.MeshReportMessageHandler`) is reachable via
  a config flag. In practice, Benzene's reflection-based `.UseMessageHandlers()` discovers every
  `[Message]`/`[HttpEndpoint]`-attributed handler in every *referenced* assembly - since this Host
  must reference `Benzene.Mesh.Aggregator` for its core aggregation functionality anyway, the
  ingestion endpoint is unavoidably discovered and reachable at `/mesh/report` the same way
  `/mesh/aggregate` always is. Gating it would need an explicit `.UseMessageHandlers(types: ...)`
  allow-list instead of the default assembly-scan, judged not worth the added complexity for v1 -
  flagged here rather than silently dropped.
- **No Tempo wiring.** `AddTempoTopology` isn't included in this pass's `Startup.cs` - a natural,
  separable follow-up (would need `tempo.prometheusUrl` in `MeshHostConfig` and a matching
  `ProjectReference` to `Benzene.Mesh.Tracing.Tempo`), not built here to keep this phase scoped.

## Opt-in live dispatch (off by default)
`MeshHostConfig.EnableDispatch` (default false) wires `Benzene.Mesh.Dispatch`'s `UseMeshDispatch()` +
`AddMeshLambdaDispatcher()` and registers the `MeshServiceRegistry` so the `mesh:dispatch` handler can
invoke a registered service's **real** handler with a chosen payload (the mesh UI composer's "send"
leg). It's a deliberate, non-default choice because real side-effects execute. Two gates, both must
pass: `EnableDispatch` (this wiring) **and** the runtime environment gate — dispatch is refused in a
Production environment unless `DispatchAllowInProduction` is *also* set (an unset environment counts as
Production). Because `MeshDispatchMessageHandler` carries no `[Message]` attribute, the default
`.UseMessageHandlers()` scan does **not** expose it — unlike `/mesh/report`, it is genuinely absent
until `EnableDispatch` is set.

## Dependencies on other Benzene packages
- **Benzene.AspNet.Core**, **Benzene.Microsoft.Dependencies** - the ASP.NET Core host wiring.
- **Benzene.Mesh.Aggregator** - `AddMeshAggregator`, `MeshAggregator`, `MeshServiceRegistry`.
- **Benzene.Mesh.Aws.Lambda** - `AddMeshLambdaSource`, for `AwsLambdaInvoke`-sourced entries.
- **Benzene.Mesh.Ui** - `UseMeshUi`, the dashboard itself.

## Why this isn't part of `Benzene.sln`/`Benzene.Examples.sln`
See `../README.md`'s own section on this - same reasoning as `templates/Benzene.Templates.sln`.
