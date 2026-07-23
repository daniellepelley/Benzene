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
- `IMeshFleetReadModel` (2026-07-23) - the **read seam** the five `mesh:query:*` handlers depend on
  (async: `FleetAsync`/`ServiceAsync`/`TopicAsync`/`TraceAsync`/`CorrelationAsync`), so the fleet UI's
  data source is swappable. `MeshCollectorStore` implements it (explicit-interface async wrappers over
  its sync read methods — the push-collector plane, unchanged). Register `IMeshFleetReadModel` alongside
  the store singleton (every host that wires the collector now does both). The other implementation,
  `TraceSourceFleetReadModel`, composes a pluggable `IMeshTraceSource` (OTel trace backend) — see
  `work/otel-fleet-adapter-scope.md`.
- `IMeshTraceSource` (2026-07-23) - the pluggable trace-shaped source (`GetTraceAsync`; correlation +
  recent-flows added in later increments) implemented per backend in a `Benzene.Mesh.Fleet.*` adapter
  (X-Ray first). Deliberately carries **no** stats/health (traces are sampled; no heartbeat feed) —
  those stay `IMeshUsageSource` + the heartbeat plane. `TraceSourceFleetReadModel` wraps it into an
  `IMeshFleetReadModel`, serving `mesh:query:trace` now and returning honest empty/absent shapes for the
  rest until the composing increments land.
- `MeshCollectorStore` - the in-memory state (singleton per collector): cumulative per-service
  and per-topic stats, latest heartbeat per instance, registered descriptors, and a bounded ring
  of recent trace events (default 4096). Consumer edges are derived **at query time** from ring
  parentage - an event whose parent span belongs to another service makes that service a
  consumer of the event's topic; who-calls-whom is observed, never declared. Re-registration
  replaces a service's provider edges wholesale (a redeploy that drops a topic drops the claim).
- `MeshCollectorHandlers.All` - the eight handlers to pass to `UseMessageHandlers`:
  `mesh:register`/`mesh:heartbeat`/`mesh:traces` ingest (service required → `BadRequest`; an
  empty trace batch is accepted) and `mesh:query:fleet`/`service`/`topic`/`trace`/`correlation`
  (missing params → `BadRequest`, unknown subjects → `NotFound`).
- `mesh:query:correlation` (`CorrelationQueryMessageHandler`, 2026-07-23) - cross-service failure
  triage from a **business correlation id** (a ticket/log id) rather than a trace id. A correlation
  id can span multiple traces, so `MeshCollectorStore.Correlation(id)` filters the ring by
  `MeshTraceEvent.CorrelationId` (already a shipped wire field, populated from `x-correlation-id`),
  groups by trace id, and returns `CorrelationView { CorrelationId; List<TraceView> Traces }` - one
  ordinary single-trace `TraceView` per matching trace (events in start order, traces ordered by
  earliest start), so the fleet UI renders each through the **same waterfall** as a normal trace.
  Events with a null correlation id never match (the mesh never fabricates one); empty id →
  `BadRequest`, nothing matched → `NotFound`. **Additive/read-model only** - no wire, ingestion, or
  spec change. Query surface is not yet conformance-pinned across languages (no Go-reference case
  yet); shipped .NET-side, a fixture case + Go collector are the fast-follow. Covered by
  `MeshCollectorStoreTest`'s correlation cases.
- View shapes (`FleetView`, `ServiceSummary`, `TopicSummary`, `ServiceView`, `InstanceView`,
  `TraceView`, `TraceSummary`, `CorrelationView`, `Ack`) - `missingFeeds` names what the collector hasn't received
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
