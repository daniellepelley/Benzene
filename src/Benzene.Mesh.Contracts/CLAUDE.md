# Benzene.Mesh.Contracts

## What this package does
Shared data shapes for the Benzene service mesh visibility feature (see
`work/service-mesh-roadmap-1.0.md`): the human-maintained service registry config, and the
generated per-service snapshot / manifest artifacts a `Benzene.Mesh.Aggregator` publishes. Pure
data types - no HTTP, no file I/O, no execution logic.

## Key types/interfaces
- `MeshServiceRegistryEntry`/`MeshServiceRegistry` - the `mesh.json` shape: `Name`, `SpecUrl`,
  `HealthUrl` per service. Human-edited, not generated.
- `MeshServiceSnapshot` - the full per-service artifact (`services/{name}.json`): raw spec JSON
  (opaque, not deserialized), its hash, the previous run's hash, a `ContractDrift` flag, the
  service's `HealthCheckResponse` (from `Benzene.HealthChecks.Core`, reused as-is - no parallel type),
  and an `Error` (exception type name only, never a message).
- `MeshManifestEntry`/`MeshManifest` - the top-level `manifest.json` index: one denormalized row per
  service (`Status`, `ContractDrift`) so a catalog view doesn't need to fetch every snapshot.
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
