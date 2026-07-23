# Benzene.Mesh.Fleet.Aws.XRay

## What this package does
The AWS realisation of the **trace-backed fleet reader** scoped in `work/otel-fleet-adapter-scope.md`:
it answers the mesh's `mesh:query:trace` from AWS X-Ray instead of the in-memory push collector, so
the fleet UI's trace waterfall works over an observability backend a team already runs — no
`mesh:traces` exporter, no `MeshCollectorStore` ring. This is **Increment 1** of that scope (trace
lookup only); correlation (inc 2), recent-flows + usage-fed stats (inc 3), and other backends (Tempo,
inc 4) compose on the same `IMeshTraceSource`/`IMeshFleetReadModel` seam later.

## Key types
- `XRayTraceSource : Benzene.Mesh.Collector.IMeshTraceSource` — fetches a trace's segments with
  `IAmazonXRay.BatchGetTraces` and maps its topic-bearing spans into a `TraceView`. Returns null (→ the
  query handler answers `NotFound`) when X-Ray has no such trace **or** the trace carried no Benzene
  topic-bearing span (a real trace that isn't a mesh flow is not an empty waterfall).
- `XRaySegmentMapper` — static `Map(meshTraceId, segmentDocuments)`: parses each X-Ray segment JSON
  document, walks segment + subsegments, and emits one `MeshTraceEvent` per node that carries a Benzene
  topic. Reads the `benzene.*` attributes (`topic`/`version`/`status`/`correlation-id`) from **either**
  `annotations` (X-Ray sanitises keys to underscores → `benzene_topic`) **or** `metadata` (dotted keys
  preserved → `benzene.topic`, at the top level or one namespace deep like `metadata.default`), because
  which of the two the OTel→X-Ray exporter uses is a deployment choice. A document that fails to parse is
  skipped (traces are read best-effort); events are returned in start order. The enclosing segment's
  `name` is the emitting `Service`; subsegments keep it (they're the same service's internal spans — a
  new service boundary is its own X-Ray segment).
- `Extensions.AddXRayFleetReadModel()` — registers a default `IAmazonXRay` (region/credentials from the
  ambient AWS environment — on Lambda, the execution role) unless one is already registered, the
  `XRayTraceSource` as `IMeshTraceSource`, and `TraceSourceFleetReadModel` as `IMeshFleetReadModel`.
  Wire the read side with `UseMessageHandlers(MeshCollectorHandlers.Queries)` (query-only — there is no
  ingestion) and the fleet UI with `UseMeshFleetUi()`.

## What it deliberately does NOT do
Per `IMeshTraceSource`, this carries **no** per-topic/service counts and **no** service health: X-Ray
traces are sampled (counts would be biased) and X-Ray has no heartbeat feed. Those come from an
`IMeshUsageSource` (CloudWatch — `Benzene.Mesh.Usage.CloudWatch`) and the heartbeat plane, composed in a
later increment. Until then a trace-only deployment answers trace lookups and reports honest
empty/absent shapes for fleet/service/topic/correlation (`TraceSourceFleetReadModel`).

## Prerequisite it relies on
The pipeline must stamp `benzene.status` (and `benzene.topic`/`benzene.version`) on the topic-bearing
span — `Benzene.Diagnostics.ActivityMiddlewareDecorator` does this (see `src/Benzene.Diagnostics/
CLAUDE.md`). Without those span tags an X-Ray trace has no mesh semantics to map.

## Verification caveat
The mapper is unit-tested against representative X-Ray segment JSON (`test/Benzene.Mesh.Test/
XRayTraceSourceTest.cs`, mocked `IAmazonXRay`), covering both the annotations and metadata attribute
forms, non-Benzene-span filtering, and the null cases. It has **not** been run against a live X-Ray
account — the exact annotation-vs-metadata landing and key sanitisation are read defensively for that
reason; confirm against a real trace before relying on it in production.

## Dependencies
- **AWSSDK.XRay** — the X-Ray query client (`BatchGetTraces`).
- **Benzene.Mesh.Collector** — `IMeshTraceSource`/`IMeshFleetReadModel`/`TraceSourceFleetReadModel`/
  `TraceView`/`MeshTraceEvent` (via `Benzene.Mesh.Wire`) and `MeshCollectorHandlers.Queries`.
- **Benzene.Abstractions** — `IBenzeneServiceContainer` for the DI extension.
