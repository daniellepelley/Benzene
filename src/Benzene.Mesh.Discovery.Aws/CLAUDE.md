# Benzene.Mesh.Discovery.Aws

## What this package does
The **AWS** half of mesh self-discovery (see `work/mesh-self-discovery-design.md`). Enumerates the
Lambda functions in an AWS account, filtered by tag, and emits `MeshServiceRegistryEntry` records so
the mesh aggregator discovers services instead of being hand-fed a `mesh.json`. Discovered entries are
bound to the existing AWS-Lambda-Invoke interrogation source (`Benzene.Mesh.Aws.Lambda`'s
`LambdaMeshServiceSource`), so a discovered function is interrogated by direct `Invoke` with no HTTP
surface needed.

## The seam it implements (lives in `Benzene.Mesh.Contracts`)
- `IMeshDiscoveryProvider` — `{ Key; DiscoverAsync(MeshDiscoveryFilter) → MeshServiceRegistryEntry[] }`.
  The "which services exist" seam, parallel to the aggregator's "how to reach one" `IMeshServiceSource`.
- `MeshDiscoveryFilter` — tag/label filter; defaults to "carries the `benzene` tag" (`DefaultTagKey`),
  fully overridable (`Matches(tags)` = every required key present, non-null values matched exactly).
- `MeshDiscoveryRunner` — runs all providers, unions with an optional hand-written static seed
  (**seed wins on a name clash** — a human pin is an intentional override), dedups by name → a
  `MeshServiceRegistry`.
- `MeshRegistryJson` — serializes a `MeshServiceRegistry` to the `mesh.json` `{ "services": [...] }`
  shape the aggregator host reads. **This is the discovery↔runtime seam**: discovery *writes* this
  document, runtime monitoring *reads* it (a drop-in for a hand-written `mesh.json`).

## Key types (this package)
- `AwsLambdaDiscoveryProvider` — `IMeshDiscoveryProvider` over `IAmazonLambda`. Paginated
  `ListFunctions` + per-function `ListTags`; keeps functions matching the filter; emits entries with
  `Source = AwsLambdaInvoke` and `SourceOptions["functionName"]`. An optional `benzene:mesh-path` tag
  is carried into `SourceOptions["meshPath"]` for services serving the descriptor at a non-default path.
- `Extensions.AddMeshAwsLambdaDiscovery()` — registers a default-credential `AmazonLambdaClient`, the
  provider (as an additional `IMeshDiscoveryProvider`), and a `MeshDiscoveryRunner` over all providers.

## Design decisions (from `work/mesh-self-discovery-design.md` §0.1)
- **`AWSSDK.Lambda` `ListFunctions`+`ListTags` only** — no `ResourceGroupsTaggingAPI` (zero new AWS
  dependency; `AWSSDK.Lambda` is already approved). The tagging-API option may be added later as an
  alternative for large accounts (N+1 `ListTags` calls is the trade-off here).
- **Discovery creates config; the aggregator consumes it at runtime** — a hard seam via
  `MeshRegistryJson`, decoupling discovery from the poll loop so they can be hosted/scheduled
  independently and the generated document is inspectable.
- **Union, seed wins** — discovered services union with a hand-written static registry; a name clash
  keeps the human-pinned entry.
- **Reuses the existing interrogation** — discovery only produces the list; `LambdaMeshServiceSource`
  (already shipped) does the Invoke interrogation. Nothing about interrogation changes.

## IAM (least privilege)
`lambda:ListFunctions`, `lambda:ListTags` (discovery), and `lambda:InvokeFunction` (interrogation,
scoped to the tagged resource group). Read + describe-invoke only; discovery never mutates.

## Dependencies
- **AWSSDK.Lambda** (already approved elsewhere) — `ListFunctions`/`ListTags`.
- **Benzene.Mesh.Contracts** — the discovery seam + registry shapes + JSON.
- **Benzene.Abstractions** — DI (`IBenzeneServiceContainer`).

## Tests
- `test/Benzene.Mesh.Test/Discovery/AwsLambdaDiscoveryProviderTest.cs` — tagged-only emission,
  pagination-marker following, `benzene:mesh-path` carry-through, valued-tag filtering (mocked
  `IAmazonLambda`).
- `test/Benzene.Mesh.Test/Discovery/MeshDiscoveryRunnerTest.cs` — union with seed, seed-wins-on-clash,
  empty, `MeshRegistryJson` round-trip through the `mesh.json` shape, filter `Matches`.

## Not yet built (next increments, per the design doc)
- Wiring discovery into a runnable **AWS mesh host** (a scheduled Lambda that runs discovery → writes
  the registry JSON to S3 → the aggregator reads it). This package is the discovery engine + AWS
  adapter; the host/deploy piece and the end-to-end AWS test come later (E2E is delivered last).
- The optional P0 to make invoke-only services answer the richer `mesh` descriptor natively (today
  `LambdaMeshServiceSource` interrogates via the already-transport-native `spec`/`healthcheck` topics).
