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

## Structural topology (`topology.json`)
Each run also derives a **structural** ("designed to call") topology and publishes it as
`topology.json` (`MeshTopology`/`TopologyEdge` in `Benzene.Mesh.Contracts`, `Source =
TopologyEdgeSource.Structural`): an edge from each service that declares it **sends** a domain topic
(the spec's `events`) to each service that **handles** it (the spec's `requests`). No tracing backend
needed — it's read straight from the specs the aggregator already fetches. This fills the gap
`TopologyEdgeSource.Structural`'s doc-comment described as "not currently produced by any package".
Note: `Benzene.Mesh.Tracing.Tempo` publishes *observed* edges to the same `topology.json`; a
deployment using both currently has last-writer-wins (merging structural + observed is a future step).

## Aggregated topic catalog (`topics.json`)
Alongside `manifest.json`/`services/{name}.json`, each run also publishes `topics.json`
(`MeshTopicCatalog` in `Benzene.Mesh.Contracts`): every distinct **(topic, version)** pair across
the mesh, who **produces** it (`MeshTopicEntry.Producers`, from each service's spec `events`) and
who **consumes** it (`MeshTopicEntry.Consumers`, from `requests`, with HTTP mappings preserved),
plus a `reserved` (domain-vs-utility) flag lifted from `requests[].reserved`. Parsing is
best-effort per service — a missing/unparseable spec contributes no topics and never fails the
run. `Benzene.Mesh.Ui` renders it as a cross-service topic table with a "show utilities" toggle.

This artifact is entirely **aggregator-computed**, not a Benzene wire contract — see
`work/service-mesh-roadmap-1.0.md` §10.9. No individual service's spec is ever asked to say
whether its own topics are still in use; only looking across every service's self-description at
once can answer that, which is exactly what this file is for.

### Per-topic payload schema + cross-consumer mismatch
`ParseTopics`/`ParseOutboundTopics` also lift each topic's payload schema out of the spec they
already fetch: the `request`/`response` of a `requests` entry and the `message` of an `events`
entry. Each is **inlined** by `InlineSchema` — every `$ref` into the spec's `components.schemas` is
replaced by the referenced schema (tagged with a `title` of the ref name; recursion cut with a
`title`-only marker, bounded by `MaxSchemaDepth`) — producing a self-contained `JsonObject` on the
`MeshTopicEntry` (`RequestSchema`/`ResponseSchema`/`MessageSchema`), so a consumer (the UI) never
has to resolve a ref or carry a components catalog. Nodes are detached from the source
`JsonDocument` (via `JsonNode.Parse`) so they outlive its `using` scope. Schemas are inlined rather
than kept as `$ref`+catalog specifically to avoid cross-service component-name collisions (two
services defining a differently-shaped `Order`), which the ref/catalog approach can't disambiguate.

`MeshTopicEntry.SchemaMismatch` flags when the consumers of one exact (topic, version) don't all
declare the same inbound payload — a likely contract error (`BuildTopicEntry` compares the inlined
schemas via `Canonical`, a key-order-normalized serialization, over only the consumers that
declared a schema; never set for a reserved utility topic). `Benzene.Mesh.Ui` renders the schema as
a payload panel on the topic page and highlights a mismatch with a badge + banner.

**`MeshTopicEntry.Status`** (`Benzene.Mesh.Contracts.MeshTopicStatus`) is an informational signal
computed from `Producers`/`Consumers`, never present on a reserved topic:
- `deprecation-candidate` — produced somewhere in the fleet, but nothing consumes it anymore. A
  candidate for retiring, not proof it's already safe to delete.
- `gap` — consumed somewhere, entirely through non-HTTP bindings, but produced nowhere in the
  fleet. **Not necessarily a problem** — the producer may be a third party or a system outside
  this Benzene fleet (e.g. something writing straight to a queue) — but worth surfacing so someone
  can confirm that's expected. Deliberately scoped to non-HTTP-only consumers: an HTTP-invoked
  topic's "producer" is inherently an external caller (a browser, a third party), never a
  fleet-internal spec declaration, so without this carve-out nearly every ordinary REST endpoint
  would flag as a false-positive gap.
- `null` — either both sides are present, or there's no reliable signal to flag (e.g. an
  HTTP-invoked topic with no consumers-side data to reason about).

These are structural signals only — computed from what services declare in their specs, not from
observed traffic. A topic that's structurally wired but genuinely idle, or one consumed outside
this fleet's own registry, can't be distinguished from here; that stronger signal is what the
collector/trace path (`docs/specification/mesh.md`, `Benzene.Mesh.Collector`'s Fleet view) adds
when it's wired up, deliberately deferred rather than required for this artifact to be useful.

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

**2026-07-19, `topics.json` shape:** `MeshTopicEntry` gained `Version`/`Producers`/`Status` and
renamed `Services` to `Consumers` (a plain rename means the same thing, clearer now that there
are two categories). `MeshTopicEntry`'s constructor signature changed accordingly - any code
constructing one directly (not just reading the JSON, which is unaffected beyond the added/renamed
fields) needs updating. `MeshServiceRegistryEntry`/`MeshManifestEntry` also gained an optional,
purely additive `OwningTeam` (trailing, defaults to `null`) - source-compatible.

**2026-07-19, `manifest.json` gained `transports`:** `MeshManifestEntry` gained an optional,
purely additive `Transports` (`string[]`, trailing, defaults to `Array.Empty<string>()` when
omitted) - source-compatible. A new `ParseTransports` (private, mirrors `ParseTopics`/
`ParseOutboundTopics`'s best-effort JSON parsing) reads each service's spec's document-level
`transports` field (`Benzene.Schema.OpenApi.EventService.EventServiceDocument.Transports`, see
`work/service-mesh-roadmap-1.0.md` §10.16-§10.17) during `BuildServiceAsync` and threads it
through `ServiceResult` into the manifest entry, same denormalization treatment as `OwningTeam`. A
missing/unparseable spec, or one predating this field, contributes an empty list rather than
failing the run.

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
- Derives **structural** topology edges from the specs (see "Structural topology" above); *observed*
  (traffic) edges remain `Benzene.Mesh.Tracing.Tempo`'s job.
