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
- `MeshAggregator.RunOnceAsync(MeshServiceRegistry)` - the core, directly unit-testable logic:
  every registered service is polled concurrently (`Task.WhenAll`, mirroring
  `Benzene.HealthChecks.HealthCheckProcessor`'s pattern), and each service's spec/health fetch is
  independently bounded by a 10-second `PerServiceFetchTimeout` (matching
  `Benzene.HealthChecks.TimeOutHealthCheck`'s convention) - one slow/hung service can't stall the
  whole run, and one service's failure never prevents the rest from being published; determines `Healthy`/`Unhealthy`/
  `Unreachable` status (unreachable if the health document couldn't be fetched/deserialized,
  regardless of whether the spec endpoint responded - health is the primary "is this okay" signal);
  compares the new spec hash against the previous run's (read back via `IMeshArtifactStore`) to set
  `ContractDrift`. **The actual fetch is delegated to `IMeshServiceSource`** (see below) - the
  timeout, error-type-name-only recording, and status/drift logic all live in `MeshAggregator`
  itself, uniform across every source.
- `IMeshServiceSource` - the fetch port `MeshAggregator` depends on instead of an `HttpClient`
  directly: `FetchSpecAsync`/`FetchHealthAsync(MeshServiceRegistryEntry, CancellationToken)`, each
  returning raw JSON text (or throwing on failure - `MeshAggregator` catches and records the
  exception's type name, same as before). `MeshAggregator`'s constructor takes
  `IEnumerable<IMeshServiceSource>`, keyed by each source's `Key` (matched against
  `MeshServiceRegistryEntry.Source`, case-insensitively) - an entry whose `Source` has no matching
  registered source resolves to `UnknownMeshServiceSource` (internal), which throws from both fetch
  methods so a misconfigured single entry surfaces as that service's own `Unreachable`/error result
  rather than crashing the whole run.
  - `HttpMeshServiceSource` - the only source this package ships, `Key = MeshServiceSource.Http`
    (the default). This is `MeshAggregator`'s original (pre-`IMeshServiceSource`) HTTP behavior,
    moved here unchanged - including the "read the health response body regardless of status code"
    handling (`HttpClient.GetAsync` + `response.Content.ReadAsStringAsync()`, not `GetStringAsync`)
    needed because `Benzene.HealthChecks.HealthCheckProcessor` maps an unhealthy result to HTTP
    503, which `GetStringAsync` would otherwise treat as a fetch failure indistinguishable from a
    genuinely unreachable service.
  - Other transports (e.g. an AWS Lambda Invoke source) ship as their own adapter package
    registering an additional `IMeshServiceSource`, the same way `Benzene.Mesh.Tracing.Tempo` adds
    a topology source without `Benzene.Mesh.Aggregator` needing to know about it.
- `MeshSnapshotBuilder` (internal) - the shared hash-and-drift-diff step (`MeshHashing.ComputeHash`
  + comparing against the previous run's hash read back via `IMeshArtifactStore`) extracted out of
  `MeshAggregator.BuildSnapshotAsync` so a future push/self-report ingestion path can build a
  `MeshServiceSnapshot` from a pushed report identically, instead of a second copy of this logic.
- `MeshAggregateMessageHandler` - thin `IMessageHandler<Void, MeshManifest>` wrapper resolving
  `MeshAggregator`/`MeshServiceRegistry` from DI and calling `RunOnceAsync` - the "dogfooding" piece
  that makes the aggregator itself a real Benzene service rather than only in-process-callable.
  Its own delegation is unit-tested directly in `test/Benzene.Mesh.Test/MeshAggregateMessageHandlerTest.cs`
  (mirroring `MeshReportMessageHandlerTest.cs`'s style); `RunOnceAsync`'s own behavior (concurrency,
  per-service timeout, status/drift determination, unreachable/unknown-source handling) is covered
  exhaustively by `MeshAggregatorTest.cs` instead.
- `IMeshArtifactStore` - `PublishAsync`/`TryReadAsync` port; `FileSystemMeshArtifactStore` is the
  only implementation this package ships (local disk). A blob-storage adapter (S3/Azure Blob) is a
  natural follow-up package implementing the same interface, not built here.
- `Extensions.AddMeshAggregator(registry, artifactRootDirectory)` - registers the registry, store,
  `HttpClient`, the default `HttpMeshServiceSource` (as `IMeshServiceSource`), the default
  `ArtifactStoreMeshReportPublisher` (as `IMeshReportPublisher`), and `MeshAggregator` against an
  `IBenzeneServiceContainer`. Handler discovery for `MeshAggregateMessageHandler`/
  `MeshReportMessageHandler` itself is left to the consuming app's own `.AddMessageHandlers()` call,
  same as any other Benzene message handler.
- **Push/self-report ingestion (Phase C):** `ArtifactStoreMeshReportPublisher : IMeshReportPublisher`
  turns a self-reported `Benzene.Mesh.Contracts.MeshServiceReport` into a full `MeshServiceSnapshot`
  (via `MeshSnapshotBuilder`, same as a pulled fetch) and writes it straight into the shared
  `IMeshArtifactStore` - fits a reporter colocated with the aggregator's own storage (e.g. a shared
  mounted volume). `MeshReportMessageHandler` (`[HttpEndpoint("POST", "/mesh/report")]`/
  `[Message("mesh:report")]`) is the ingestion endpoint - a thin wrapper resolving whichever
  `IMeshReportPublisher` is registered and calling it, only reachable if the host's own
  `.AddMessageHandlers()` discovers it (opt-in, same as every other Benzene handler - an aggregator
  deployment that never wires this up has no write surface at all). A reporter that isn't colocated
  posts here via `Benzene.Mesh.Reporting.HttpMeshReportPublisher` instead of writing directly.

## Breaking change (pre-1.0, flagged per repo convention)
`MeshAggregator`'s constructor changed from `(HttpClient httpClient, IMeshArtifactStore store,
Func<DateTimeOffset>? clock = null)` to `(IEnumerable<IMeshServiceSource> sources, IMeshArtifactStore
store, Func<DateTimeOffset>? clock = null)`. A second overload keeping the old `HttpClient`
signature was deliberately **not** added - `MeshAggregator` is resolved via automatic
constructor-injection (`services.AddSingleton<MeshAggregator>()`), and two constructors both
satisfiable by registered services risks ambiguous/non-deterministic resolution depending on the
underlying DI container. Anyone constructing `MeshAggregator` directly (not through
`AddMeshAggregator`) needs to pass `new[] { new HttpMeshServiceSource(httpClient) }` instead of a
bare `HttpClient`. `AddMeshAggregator`'s own public signature is unchanged.

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
