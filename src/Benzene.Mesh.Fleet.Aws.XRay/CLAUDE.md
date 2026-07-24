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
  `GetRecentFlowsAsync` (inc 3) runs one unfiltered `GetTraceSummaries` over the recent-flows window,
  selects the newest N by the trace-id epoch, and **enriches** those rows with real span data via a
  **bounded `BatchGetTraces`** (2026-07-25 — see the invariant-reversal note below): each row's `Services`
  = distinct `benzene.service` from the mapped events (not `ServiceIds[].Name`, X-Ray's own — on Lambda
  infra/handler names), `StartedAt` = the earliest mapped event's real **millisecond** start (not the
  second-granularity id epoch), `Events` = the real span count, and `Topic` (2026-07-25) = the earliest
  mapped event's topic (null on a fallback row — which the UI reads as the "uninstrumented failure"
  signal). `Failed` (`HasError||HasFault`) and
  `Duration`×1000 → ms stay from the summary (authoritative + free). A row whose trace can't be fetched or
  carries no Benzene span falls back **per-row** to the summary plane (`ServiceIds`/id-epoch/`Events=0`) —
  fetch isolation, so one bad batch degrades ≤5 rows, never the list. Final order is
  `(StartedAt desc, TraceId desc)` — the trace-id tiebreaker makes both the enriched (ms) and fallback
  (second) rows deterministic. ≤4 `BatchGetTraces` calls per load (20 rows ÷ 5 ids/call), run in parallel.
- **Recent-flows enrichment (2026-07-25 — reverses the earlier "zero `BatchGetTraces` per fleet load"
  invariant).** The original inc-3 design read only the summary plane to avoid any per-row fan-out, but
  that left the fleet's recent-flows `Services` column **and** the composite service list
  (`CompositeMeshFleetReadModel.ServicesFromFlows` derives from `TraceSummary.Services`) showing X-Ray's
  backend names — infra/handler names on Lambda (`ApiGatewayLambdaHandler`, `EventBridgeEventHandler`),
  the very thing the `benzene.service` drill-in fix removed everywhere else. Enrichment closes that gap on
  the primary "what's flowing now" plane at a bounded, batched cost. It's **on by default**, tunable via
  `XRayTraceSourceOptions.RecentFlowsServiceEnrichmentMax` (0 = opt out → the old pure-summary behavior,
  for a deployment that polls the fleet aggressively and wants to minimise `BatchGetTraces` TPS). Ordering
  stays fixed even with enrichment off (the `TraceId` tiebreaker on the id-epoch path). Tempo does **not**
  do this yet — its enrichment would be 20 **unbatched** `GET /api/traces/{id}` calls, a heavier fan-out,
  so its summary-plane caveat stands (deferred follow-up).
- `XRayTraceSourceOptions` — `CorrelationLookback` (default 24h, the correlation search window),
  `RecentFlowsLookback` (default 1h, the fleet recent-flows window — "what's flowing now" is a shorter
  horizon than "find the trace for this ticket"), and `RecentFlowsServiceEnrichmentMax` (default 20 =
  the fleet cap; 0 = opt out of recent-flows span enrichment). The lookbacks feed X-Ray's
  `GetTraceSummaries`, which needs a time range; a trace lookup is by id (no window).
- `XRaySegmentMapper` — static `Map(meshTraceId, segmentDocuments)`: parses each X-Ray segment JSON
  document, walks segment + subsegments, and emits one `MeshTraceEvent` per node that carries a Benzene
  topic. **The emitting service is `benzene.service` when the span carries it (2026-07-24), falling back
  to the enclosing segment's `name`.** This is the fix for `orders-api → ApiGatewayLambdaHandler`: on
  Lambda the segment `name` is the ADOT/handler name, not the service, so reading the pipeline-stamped
  `benzene.service` attribute (via the same annotation/metadata reader as the other `benzene.*` tags) is
  authoritative; the segment name is only the fallback for a span that predates the tag. Reads the `benzene.*` attributes (`topic`/`version`/`status`/`correlation-id`) from **either**
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
  `UseMessageHandlers(MeshCollectorHandlers.Queries)` (query-only — there is no ingestion) and point the
  mesh UI's live Fleet plane at it with `UseMeshUi(..., envelopeUrl: "/benzene/invoke")`.
  `examples/AwsMesh/Mesh/Startup.cs` shows the full wiring on an API Gateway Lambda (envelope endpoint via
  `UseBenzeneMessage` + `UseMeshUi(..., envelopeUrl)`).

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

## Recent-flows service names (enriched — 2026-07-25)
`GetRecentFlowsAsync` now **enriches** the recent-flows rows with real span data via a bounded, batched
`BatchGetTraces` (see the enrichment note above), so a row's "services touched" shows the mesh's
`benzene.service` names, not X-Ray's own `ServiceIds` (which on Lambda are infra names like
`ApiGatewayLambdaHandler`/`EventBridgeEventHandler`). This closes the earlier summary-plane gap that also
poisoned the composite service list (`ServicesFromFlows` derives from `TraceSummary.Services`). A row that
can't be enriched (trace aged out / no Benzene span / batch failed) falls back **per-row** to the
summary-plane `ServiceIds` — so the backend name can still appear on an un-enriched row, but never in place
of a name the enrichment could read. Enrichment is on by default; set
`XRayTraceSourceOptions.RecentFlowsServiceEnrichmentMax = 0` to restore the pure-summary behavior (backend
names, `Events = 0`) for a poll-heavy, cost-sensitive deployment. The **drill-in waterfall and correlation
view** map full segments and have always shown real names. (**Tempo** still shares the old summary-plane
caveat — enriching it means 20 unbatched trace GETs, a heavier fan-out, deferred; **Jaeger** never had the
caveat — its recent-flows returns full traces already.)

## Verification caveat
The mapper, correlation search, and recent-flows mapping are unit-tested against representative X-Ray
JSON (`test/Benzene.Mesh.Test/XRayTraceSourceTest.cs`, mocked `IAmazonXRay`), covering both the
annotations and metadata attribute forms, non-Benzene-span filtering, correlation paging/grouping,
recent-flows enrichment (`benzene.service` over `ServiceIds`, ms-precision ordering within one epoch
second, per-row summary-plane fallback, real event count, and the `…EnrichmentMax = 0` opt-out), and the
null cases. The composite is covered by
`CompositeMeshFleetReadModelTest.cs`. None of it has been run against a **live** X-Ray/CloudWatch account
— the annotation-vs-metadata landing, key sanitisation, and `result`-tag names are read defensively /
documented convention for that reason; confirm against real data before relying on it in production.

## Dependencies
- **AWSSDK.XRay** — the X-Ray query client (`BatchGetTraces`).
- **Benzene.Mesh.Collector** — `IMeshTraceSource`/`IMeshFleetReadModel`/`CompositeMeshFleetReadModel`/
  `TraceView`/`MeshTraceEvent` (via `Benzene.Mesh.Wire`) and `MeshCollectorHandlers.Queries`.
- **Benzene.Abstractions** — `IBenzeneServiceContainer` for the DI extension.
