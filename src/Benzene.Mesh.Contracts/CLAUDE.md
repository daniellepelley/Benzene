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
  `MeshTopicEntry` also carries the topic's **payload schema** — `RequestSchema`/`ResponseSchema`
  (from a consumer's spec) and `MessageSchema` (from a producer's `events`), each a
  `System.Text.Json.Nodes.JsonObject?` with `$ref`s already inlined by the aggregator so it renders
  standalone — plus `SchemaMismatch` (bool): true when two or more consumers of the *same*
  (topic, version) declare **different** inbound payloads, surfaced as a likely contract error
  (unlike the informational `Status`). All four are optional/additive (trailing constructor params
  defaulting to `null`/`false`), so old `topics.json` and any code constructing a `MeshTopicEntry`
  keep working.
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
  service (`Status`, `ContractDrift`, optional `OwningTeam`, `Transports`, `SnapshotAtUtc`) so a
  catalog view doesn't need to fetch every snapshot. `SnapshotAtUtc` (`DateTimeOffset?`, `null`
  default/trailing) is denormalized from that service's `MeshServiceSnapshot.FetchedAtUtc` so a
  catalog/issue view can judge **freshness** (a service that stopped self-reporting) from
  `manifest.json` alone — deliberately a raw timestamp, **not** a `Stale` status: staleness is
  orthogonal to health (a service can be healthy-as-last-heard *and* stale) and is a read-time
  derivation, so it's judged UI-side against a threshold rather than baked into the artifact (see
  `work/service-mesh-roadmap-1.0.md`'s 2026-07-20 staleness ruling). Distinct from
  `MeshManifest.GeneratedAtUtc` — in push/self-report mode a row's snapshot can be older than the run
  that emitted the manifest, which is what makes staleness detectable. `Transports` (`string[]`, empty default) is lifted from
  that service's spec's document-level `transports` field (`Benzene.Schema.OpenApi`'s
  `EventServiceDocument.Transports`) by `Benzene.Mesh.Aggregator`, the same "parse the spec,
  denormalize onto the manifest" treatment as `OwningTeam` - see
  `Benzene.Mesh.Aggregator/CLAUDE.md`.
- `MeshTopology`/`TopologyEdge`/`TopologyEdgeSource` - the `topology.json` shape: cross-service call
  edges (`Client`→`Server`, plus nullable `RequestsPerMinute`/`ErrorRate`/`P50`/`P95`/`P99LatencyMs`),
  each tagged with an origin (`TopologyEdgeSource.Tempo` for observed traffic, `.Structural` for the
  declared-contract derivation). Pure shapes only - populated by `Benzene.Mesh.Aggregator`
  (`BuildTopology`, structural producer→consumer edges every run, with `RequestsPerMinute`/`ErrorRate`
  filled from the usage feed where a topic's traffic attributes to a specific edge unambiguously -
  percentiles stay null, that feed has no latency) and `Benzene.Mesh.Tracing.Tempo` (observed traffic,
  including latency), not by anything in this package.
- `MeshAnnotation`/`MeshAnnotationLog`/`MeshAnnotationRequest`/`MeshAnnotationThread` (2026-07-22)
  - the discussion shapes: one note attached to one entity of the estate (`Entity` is
  `"service:<name>"`/`"topic:<topicId>"`, mirroring the explorer's own hash-entity model).
  `MeshAnnotationLog` is the `annotations.json` artifact (the READ path - static, zero backend);
  `MeshAnnotationRequest`/`MeshAnnotationThread` are the `mesh:annotations:add` payload/response
  (the WRITE path, `Benzene.Mesh.Aggregator.MeshAnnotationsMessageHandler`). `Author` is a
  **self-declared display name by design** - authenticating who may post belongs to the gateway
  in front of the endpoint (the `Benzene.RateLimiting` boundary ruling applied to writes); the
  mesh packages stay identity-free. `MeshAnnotationRequest` uses settable properties (wire input,
  the `MeshServiceReport` convention) - validation/bounds are the handler's job.
  contract-drift **substance** ("what changed since the previous aggregator run", not just a hash
  flip): `MeshTopicEntry` gained an optional trailing `Changes` array (kinds `topic-added`/
  `schema-changed`/`producers-changed`/`consumers-changed` - loose strings, the
  `MeshServiceStatus` convention, so an older reader renders an unknown kind's description) and
  `MeshTopicCatalog` an optional trailing `RemovedTopics` (declared in the previous run, declared
  nowhere now - it can't sit on a current entry because there isn't one). Both purely additive
  and computed by `Benzene.Mesh.Aggregator`'s run-over-run catalog diff; empty on a first run.
- `MeshUsage`/`MeshUsageEntry`/`IMeshUsageSource`/`MeshUsageSource` - the `usage.json` shape and
  its adapter port (`docs/mesh-usage-feed.md`): observed per-topic message counts at whatever
  dimensions the adapter's backend can supply — `Topic` required, `Version`/`Service`/`Transport`/
  `Status` nullable (**null = "backend doesn't have this dimension", never "all"**), each entry
  tagged with its own `Source` (the `TopologyEdge.Source` precedent, constants in
  `MeshUsageSource`). Granularity rule: an entry is a count at exactly the dimensions it states,
  entries from one source never overlap, consumers group over whichever stated dimensions they
  need. `IMeshUsageSource.FetchUsageAsync` returning `null` means "nothing to report this run" —
  distinct from an empty `Entries` array ("feed wired, no traffic observed"), which the
  aggregator still publishes. The port lives here (not in the aggregator) for the same reason as
  `IMeshReportPublisher`: an adapter depends on this package alone. First shipped implementation:
  `Benzene.Mesh.Collector.CollectorUsageSource`; metrics-backend adapters (App Insights,
  CloudWatch) ship as their own packages since they need their backend SDKs.
  `FetchUsageAsync` also takes an optional `MeshUsageWindow?` (2026-07-24) — a resolved absolute
  `[FromUtc,ToUtc]` a caller (the composite fleet reader, driven by the mesh UI's time-range picker)
  asks the source to scope its counts to; `null` = the source's own configured window (today's
  behavior, so the aggregator's `usage.json` path is unaffected). A source that can't honor an
  arbitrary window (a cumulative counter) ignores it and reports its own window, and the caller
  compares the returned window to the request to decide whether the counts were actually windowed
  (no source self-certifies). This package deliberately stays free of the relative-time grammar
  (`now-1h` etc.) — that lives with the read models; `MeshUsageWindow` is resolved-absolute only.
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
