# Benzene.Mesh.Aggregator

## What this package does
Polls every service in a `Benzene.Mesh.Contracts.MeshServiceRegistry` for its spec and health
documents, computes contract-drift against the previous run, and publishes a catalog
(`manifest.json` + one `services/{name}.json` per service) - the aggregator half of the service
mesh visibility feature described in `work/service-mesh-roadmap-1.0.md`. Ships as a genuine Benzene
application: `MeshAggregateMessageHandler` exposes a run as a `[Message("mesh:aggregate")]`/
`[HttpEndpoint("POST", "/mesh/aggregate")]` handler, reachable on whatever transport the host
already runs, not a bespoke standalone tool.

## Key types/interfaces
- `MeshAggregator.RunOnceAsync(MeshServiceRegistry)` - the core, directly unit-testable logic: for
  each registered service, fetches its spec (hashes it) and health document independently, so one
  service's failure never prevents the rest from being published; determines `Healthy`/`Unhealthy`/
  `Unreachable` status (unreachable if the health document couldn't be fetched/deserialized,
  regardless of whether the spec endpoint responded - health is the primary "is this okay" signal);
  compares the new spec hash against the previous run's (read back via `IMeshArtifactStore`) to set
  `ContractDrift`.
- `MeshAggregateMessageHandler` - thin `IMessageHandler<Void, MeshManifest>` wrapper resolving
  `MeshAggregator`/`MeshServiceRegistry` from DI and calling `RunOnceAsync` - the "dogfooding" piece
  that makes the aggregator itself a real Benzene service rather than only in-process-callable.
- `IMeshArtifactStore` - `PublishAsync`/`TryReadAsync` port; `FileSystemMeshArtifactStore` is the
  only implementation this package ships (local disk). A blob-storage adapter (S3/Azure Blob) is a
  natural follow-up package implementing the same interface, not built here.
- `Extensions.AddMeshAggregator(registry, artifactRootDirectory)` - registers the registry, store,
  `HttpClient`, and `MeshAggregator` against an `IBenzeneServiceContainer`. Handler discovery for
  `MeshAggregateMessageHandler` itself is left to the consuming app's own `.AddMessageHandlers()`
  call, same as any other Benzene message handler.

## When to use this package
- Any Benzene solution that wants a generated catalog of its services' contracts and health,
  refreshed on a schedule or triggered post-deploy - wire `AddMeshAggregator(...)` into a host and
  trigger `mesh:aggregate` however fits (an HTTP call, a scheduled Lambda/Function invocation, a
  queue message).

## Dependencies on other Benzene packages
- **Benzene.Mesh.Contracts** - the data shapes this package fetches, hashes, and publishes.
- **Benzene.Abstractions.MessageHandlers**, **Benzene.Core.MessageHandlers**, **Benzene.Http** -
  for `IMessageHandler<,>`, `[Message]`, and `[HttpEndpoint]` respectively.

## Important conventions
- Uses `System.Text.Json`, not `Newtonsoft.Json` (avoids adding a new NuGet dependency to a
  brand-new package; matches the production `ISerializer`'s camelCase convention).
- Every fetch failure is recorded as the exception's *type name*, never its message - this
  artifact aggregates across services into something with broader visibility than one service's own
  health endpoint.
- No topology/edge derivation in this package yet - deliberately deferred (see
  `work/service-mesh-roadmap-1.0.md` and the plan history for why).
