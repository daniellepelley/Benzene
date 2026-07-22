# Benzene.Mesh.Collector

## What this package does
The spec collector of `docs/specification/mesh.md` §4-§6 - an ordinary Benzene service
(dogfooded message handlers) that ingests the three mesh wire topics and answers the
`mesh:query:*` read models over an in-memory store. Together with `Benzene.Mesh.Wire` this makes
the .NET implementation cover the **full** mesh contract: it passes all three conformance
fixture files (`test/Benzene.Conformance.Test`), including `mesh-collector-cases.json`, and has
hosted a live cross-language fleet (Go and C# services in one view - see the roadmap's
2026-07-16 updates).

## Key types/interfaces
- `MeshCollectorStore` - the in-memory state (singleton per collector): cumulative per-service
  and per-topic stats, latest heartbeat per instance, registered descriptors, and a bounded ring
  of recent trace events (default 4096). Consumer edges are derived **at query time** from ring
  parentage - an event whose parent span belongs to another service makes that service a
  consumer of the event's topic; who-calls-whom is observed, never declared. Re-registration
  replaces a service's provider edges wholesale (a redeploy that drops a topic drops the claim).
- `MeshCollectorHandlers.All` - the seven handlers to pass to `UseMessageHandlers`:
  `mesh:register`/`mesh:heartbeat`/`mesh:traces` ingest (service required → `BadRequest`; an
  empty trace batch is accepted) and `mesh:query:fleet`/`service`/`topic`/`trace` (missing
  params → `BadRequest`, unknown subjects → `NotFound`).
- View shapes (`FleetView`, `ServiceSummary`, `TopicSummary`, `ServiceView`, `InstanceView`,
  `TraceView`, `TraceSummary`, `Ack`) - `missingFeeds` names what the collector hasn't received
  per service ("descriptor"/"health"/"traces"); `hashMatches` surfaces an instance running a
  different contract than its registration; health is `healthy`/`degraded`/`unknown` from the
  latest heartbeats.
- `CollectorUsageSource : Benzene.Mesh.Contracts.IMeshUsageSource` (2026-07-22) - the
  collector→aggregator usage bridge, the "`IMeshArtifactStore` bridge to the aggregator pipeline"
  extension point below made real for the usage feed (`docs/mesh-usage-feed.md`). Reports the
  store's cumulative per-topic stats as one `MeshUsageEntry` per (topic, version, status), window
  = since `MeshCollectorStore.StartedAtUtc` (in-memory stats are cumulative since process start).
  `Transport`/`Service` are deliberately `null` - the trace wire shape carries no transport, and
  per-status counts aren't attributed per handling service - so a collector-fed `usage.json`
  exercises the UI's missing-dimension degradation path honestly rather than guessing. Register
  it alongside `AddMeshAggregator` in a host that also runs the collector's handlers (they share
  the singleton store). Never returns `null`: an idle collector is a wired feed with an empty
  entries array, not an absent one. This added the package's `Benzene.Mesh.Contracts` reference.

## Important conventions
- **Degradation is normative (spec §6, collector side)**: partial fleets are accepted and
  rendered as reduced - traces from an unregistered service create an anonymous-but-live row,
  a registered service with no traffic is a catalog entry with no stats, and no missing feed
  ever fails ingestion or a query.
- The query shapes are asserted by `mesh-collector-cases.json` as the observable surface for
  the ingest/derivation rules; treat them as fixture-pinned even though the spec doesn't promote
  them as cross-port contracts yet (spec §4's note).
- Cumulative stats deliberately outlive the trace ring window; the fleet flow list caps at 20,
  newest first (`test/Benzene.Mesh.Test/MeshCollectorStoreTest.cs` pins these).
- Storage is in-memory by design for this tier (a restart re-learns the fleet from the next
  heartbeats/traces). A durable store or an `IMeshArtifactStore` bridge to the
  `Benzene.Mesh.Aggregator` pipeline is the natural extension point, not a rewrite.

## Dependencies on other Benzene packages
- **Benzene.Mesh.Wire** - the wire shapes it ingests.
- **Benzene.Mesh.Contracts** - `IMeshUsageSource`/`MeshUsage` for `CollectorUsageSource`.
- **Benzene.Core.MessageHandlers** / **Abstractions.MessageHandlers** - handler idiom.
- **Benzene.Results** - statuses (the wire-contracts §3 success class drives error counting).
