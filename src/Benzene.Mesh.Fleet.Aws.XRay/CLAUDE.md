# Benzene.Mesh.Fleet.Aws.XRay

## What this package does
The AWS realisation of the **trace-backed fleet reader** scoped in `work/otel-fleet-adapter-scope.md`:
it answers the mesh's `mesh:query:trace`, `mesh:query:correlation`, and the fleet view's recent-flows
from AWS X-Ray instead of the in-memory push collector, so the fleet UI's trace waterfall, correlation
triage, and recent-flows/service list work over an observability backend a team already runs — no
`mesh:traces` exporter, no `MeshCollectorStore` ring. This is **Increments 1-3** of that scope (trace
lookup + correlation search + recent flows, composed with the CloudWatch usage feed for topic stats);
other backends (Tempo, inc 4) reuse the same `IMeshTraceSource`/`IMeshFleetReadModel` seam later.

## Key types
- `XRayTraceSource : Benzene.Mesh.Collector.IMeshTraceSource` — fetches a trace's segments with
  `IAmazonXRay.BatchGetTraces` and maps its topic-bearing spans into a `TraceView`. Returns null (→ the
  query handler answers `NotFound`) when X-Ray has no such trace **or** the trace carried no Benzene
  topic-bearing span (a real trace that isn't a mesh flow is not an empty waterfall). `GetCorrelationAsync`
  (inc 2) runs `GetTraceSummaries` filtered on `annotation.benzene_correlation_id = "…"` over the
  configured lookback (paging `NextToken` to the end), fetches the matching traces with `BatchGetTraces`
  (chunked to X-Ray's 5-ids-per-call limit), maps each to a `TraceView`, and groups them into a
  `CorrelationView` (traces earliest-first — the same ordering the in-memory collector uses, so the UI
  renders both identically). Only **annotations** are filterable in X-Ray, so the correlation id must land
  as one (see the prerequisite below).
  `GetRecentFlowsAsync` (inc 3) runs one unfiltered `GetTraceSummaries` over the recent-flows window and
  maps each X-Ray summary to a `TraceSummary` row (`Id`; `Duration`×1000 → ms; `HasError||HasFault` →
  `Failed`; `ServiceIds[].Name` → `Services`; start time parsed from the trace-id epoch prefix
  `1-{hex}-…`; `Events = 0` — a summary has no span count), newest first, capped at 20. **Zero
  `BatchGetTraces` per fleet load** by design (no per-row fan-out; the accurate event count is one
  `GetTraceAsync` away on drill-in).
- `XRayTraceSourceOptions` — `CorrelationLookback` (default 24h, the correlation search window) and
  `RecentFlowsLookback` (default 1h, the fleet recent-flows window — "what's flowing now" is a shorter
  horizon than "find the trace for this ticket"). Both feed X-Ray's `GetTraceSummaries`, which needs a
  time range; a trace lookup is by id (no window).
- `XRaySegmentMapper` — static `Map(meshTraceId, segmentDocuments)`: parses each X-Ray segment JSON
  document, walks segment + subsegments, and emits one `MeshTraceEvent` per node that carries a Benzene
  topic. Reads the `benzene.*` attributes (`topic`/`version`/`status`/`correlation-id`) from **either**
  `annotations` (X-Ray sanitises keys to underscores → `benzene_topic`) **or** `metadata` (dotted keys
  preserved → `benzene.topic`, at the top level or one namespace deep like `metadata.default`), because
  which of the two the OTel→X-Ray exporter uses is a deployment choice. A document that fails to parse is
  skipped (traces are read best-effort); events are returned in start order. The enclosing segment's
  `name` is the emitting `Service`; subsegments keep it (they're the same service's internal spans — a
  new service boundary is its own X-Ray segment).
- `Extensions.AddXRayFleetReadModel(options?)` — registers the `XRayTraceSourceOptions` (defaults if
  omitted), a default `IAmazonXRay` (region/credentials from the ambient AWS environment — on Lambda, the
  execution role) unless one is already registered, the `XRayTraceSource` as `IMeshTraceSource`, and
  `CompositeMeshFleetReadModel` as `IMeshFleetReadModel` (composed with whatever `IMeshUsageSource`s are
  registered — add `AddCloudWatchUsage` for topic stats). Wire the read side with
  `UseMessageHandlers(MeshCollectorHandlers.Queries)` (query-only — there is no ingestion) and the fleet
  UI with `UseMeshFleetUi()`. `examples/AwsMesh/Mesh/Startup.cs` shows the full wiring on an API Gateway
  Lambda (envelope endpoint via `UseBenzeneMessage` + `UseMeshFleetUi`).

## What it deliberately does NOT do
Per `IMeshTraceSource`, this carries **no** per-topic/service counts and **no** service health: X-Ray
traces are sampled (counts would be biased) and X-Ray has no heartbeat feed. Those come from an
`IMeshUsageSource` (CloudWatch — `Benzene.Mesh.Usage.CloudWatch`) and the heartbeat plane.
`CompositeMeshFleetReadModel` composes the two: topic stats from the usage feed, recent flows + the
anonymous-but-live service list from this trace source, per-service/single-topic pages omitted (no
descriptor feed). Genuinely-absent stats are marked (`MissingFeeds`), never shown as `0` — see that type
in `Benzene.Mesh.Collector`.

## Prerequisites it relies on
The pipeline must stamp `benzene.status`/`benzene.topic`/`benzene.version` — and, for correlation,
`benzene.correlation-id` — on the topic-bearing span. `Benzene.Diagnostics.ActivityMiddlewareDecorator`
does this (see `src/Benzene.Diagnostics/CLAUDE.md`); `benzene.correlation-id` is set only when the
message actually carried `x-correlation-id` (never a fabricated id, the same rule as
`MeshTraceEvent.CorrelationId`). Without those span tags an X-Ray trace has no mesh semantics to map. One
deployment step is **yours**: X-Ray only lets you *filter* on **annotations**, so the OTel→X-Ray exporter
must be configured to index `benzene.correlation-id` as an annotation (`benzene_correlation_id`) — a
metadata-only attribute is readable in a fetched trace but not searchable, so `mesh:query:correlation`
would find nothing.

## Verification caveat
The mapper, correlation search, and recent-flows mapping are unit-tested against representative X-Ray
JSON (`test/Benzene.Mesh.Test/XRayTraceSourceTest.cs`, mocked `IAmazonXRay`), covering both the
annotations and metadata attribute forms, non-Benzene-span filtering, correlation paging/grouping,
recent-flows ordering + zero-`BatchGetTraces`, and the null cases. The composite is covered by
`CompositeMeshFleetReadModelTest.cs`. None of it has been run against a **live** X-Ray/CloudWatch account
— the annotation-vs-metadata landing, key sanitisation, and `result`-tag names are read defensively /
documented convention for that reason; confirm against real data before relying on it in production.

## Dependencies
- **AWSSDK.XRay** — the X-Ray query client (`BatchGetTraces`).
- **Benzene.Mesh.Collector** — `IMeshTraceSource`/`IMeshFleetReadModel`/`CompositeMeshFleetReadModel`/
  `TraceView`/`MeshTraceEvent` (via `Benzene.Mesh.Wire`) and `MeshCollectorHandlers.Queries`.
- **Benzene.Abstractions** — `IBenzeneServiceContainer` for the DI extension.
