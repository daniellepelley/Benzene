# Benzene.Mesh.Contracts

## What this package does
Shared data shapes for the Benzene service mesh visibility feature (see
`work/service-mesh-roadmap-1.0.md`): the human-maintained service registry config, and the
generated per-service snapshot / manifest artifacts a `Benzene.Mesh.Aggregator` publishes. Pure
data types - no HTTP, no file I/O, no execution logic.

## Key types/interfaces
- `MeshServiceRegistryEntry`/`MeshServiceRegistry` - the `mesh.json` shape: `Name`, `SpecUrl`,
  `HealthUrl`, plus additive `Source`/`SourceOptions` per service. Human-edited, not generated.
  `Source` (defaults to `MeshServiceSource.Http` via the original 3-arg constructor, still present
  and unchanged) selects which `Benzene.Mesh.Aggregator.IMeshServiceSource` fetches this entry;
  `SourceOptions` is an untyped `IReadOnlyDictionary<string, string>?` for source-specific config
  (e.g. an AWS Lambda function name/region) - deliberately untyped so this package doesn't need to
  know what every adapter package requires, keeping it dependency-light (see Important conventions).
  `OwningTeam` (optional, `null` default) is purely informational - the team/individual to contact
  about this service, threaded through to `MeshManifestEntry` so "who do I talk to before I change
  this" has an answer in the manifest without a separate lookup.
- `MeshTopicEntry`/`MeshTopicService`/`MeshTopicProducer`/`MeshTopicHttpMapping`/`MeshTopicStatus`
  - the `topics.json` shapes (see `Benzene.Mesh.Aggregator/CLAUDE.md`'s "Aggregated topic catalog"
  section for the full computation). One `MeshTopicEntry` per **(topic, version)** pair seen
  anywhere in the fleet: `Producers` (from each service's spec `events`) and `Consumers` (from
  `requests`, each carrying its `HttpMappings`), plus a `Status` (`MeshTopicStatus.DeprecationCandidate`/
  `.Gap`/`null`) computed by `Benzene.Mesh.Aggregator` from the producer/consumer shape - never a
  claim any single service makes about its own topics (work/service-mesh-roadmap-1.0.md §10.9).
- `MeshServiceSource` - string constants for known `Source` values (`Http`, `AwsLambdaInvoke`);
  adapter packages' constants get added here too, matching `TopologyEdgeSource`'s existing "known
  names live in Contracts" convention.
- `MeshServiceReport`/`IMeshReportPublisher` - the push/self-report shapes (Phase C). `MeshServiceReport`
  is a narrower cousin of `MeshServiceSnapshot` (no `SpecHash`/`PreviousSpecHash`/`ContractDrift` -
  a reporter shouldn't compute those itself, whatever receives the report does, so pulled and
  pushed snapshots compute drift identically). `IMeshReportPublisher` is a **zero-I/O port**
  (`PublishAsync(MeshServiceReport)`) - a deliberate, small widening of this package's role beyond
  "pure data shapes," so a lightweight reporting client (`Benzene.Mesh.Reporting`) depends on just
  this package, not the whole `Benzene.Mesh.Aggregator`. Two implementations ship elsewhere:
  `Benzene.Mesh.Aggregator.ArtifactStoreMeshReportPublisher` (direct store write) and
  `Benzene.Mesh.Reporting.HttpMeshReportPublisher` (HTTP ingestion) - both swappable behind this one
  port, per an explicit maintainer request for both to exist rather than just one.
- `MeshServiceSnapshot` - the full per-service artifact (`services/{name}.json`): raw spec JSON
  (opaque, not deserialized), its hash, the previous run's hash, a `ContractDrift` flag, the
  service's `HealthCheckResponse` (from `Benzene.HealthChecks.Core`, reused as-is - no parallel type),
  and an `Error` (exception type name only, never a message).
- `MeshManifestEntry`/`MeshManifest` - the top-level `manifest.json` index: one denormalized row per
  service (`Status`, `ContractDrift`, optional `OwningTeam`, `Transports`) so a catalog view
  doesn't need to fetch every snapshot. `Transports` (`string[]`, empty default) is lifted from
  that service's spec's document-level `transports` field (`Benzene.Schema.OpenApi`'s
  `EventServiceDocument.Transports`) by `Benzene.Mesh.Aggregator`, the same "parse the spec,
  denormalize onto the manifest" treatment as `OwningTeam` - see
  `Benzene.Mesh.Aggregator/CLAUDE.md`.
- `MeshTopology`/`TopologyEdge`/`TopologyEdgeSource` - the `topology.json` shape: cross-service call
  edges (`Client`→`Server`, plus nullable `RequestsPerMinute`/`ErrorRate`/`P50`/`P95`/`P99LatencyMs`),
  each tagged with an origin (`TopologyEdgeSource.Tempo` for observed traffic, `.Structural` for a
  future "designed to call" derivation). Pure shapes only - actually populated by
  `Benzene.Mesh.Tracing.Tempo`, not by anything in this package.
- `MeshServiceStatus` - string constants `Healthy`/`Unhealthy`/`Unreachable`, mirroring
  `HealthCheckStatus`'s loose-string convention (not an enum).
- `MeshHashing.ComputeHash(string json)` - the contract-drift hash. Deliberately reimplements
  `Benzene.CodeGen.Core.CodeGenHelpers.GenerateHash`'s exact algorithm (HMAC-SHA256, empty key,
  lowercase hex) rather than referencing that package, to keep this package's dependency graph
  limited to what a runtime aggregator needs - see `test/Benzene.Core.Test/Mesh/MeshHashingTest.cs`
  for the cross-check that keeps the two from silently drifting apart.

## When to use this package
- Consumed by `Benzene.Mesh.Aggregator` and (in a future phase) a Mesh UI reading its published JSON.
- Not typically referenced directly by application code outside the mesh feature.

## Dependencies on other Benzene packages
- **Benzene.HealthChecks.Core** - reused for `HealthCheckResponse`/`HealthCheckResult`/
  `HealthCheckDependency` rather than duplicating them.

## Important conventions
- All types are plain classes with a single public constructor and get-only properties - binds
  cleanly with `System.Text.Json`'s constructor-parameter matching with no `[JsonPropertyName]`
  decoration needed (casing is left to the caller's `JsonSerializerOptions`).
- `MeshServiceSnapshot.SpecJson` is stored verbatim, never deserialized into a typed spec document -
  this pass only needs a hash of it, not its structure.
