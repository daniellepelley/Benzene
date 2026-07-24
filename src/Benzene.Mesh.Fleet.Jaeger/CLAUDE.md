# Benzene.Mesh.Fleet.Jaeger

## What this package does
A second **non-AWS reference** realisation of the trace-backed fleet reader scoped in
`work/otel-fleet-adapter-scope.md`: it answers the mesh's `mesh:query:trace`, `mesh:query:correlation`,
and the fleet view's recent-flows from a **Jaeger query service**, reusing the **same**
`CompositeMeshFleetReadModel`, `MeshCollectorHandlers.Queries`, and `UseMeshFleetUi` as the X-Ray and
Tempo adapters — a third backend on the `IMeshTraceSource` seam with, again, zero upstream change.

## Key types
- `JaegerTraceSource : Benzene.Mesh.Collector.IMeshTraceSource` —
  - `GetTraceAsync` → `GET /api/traces/{id}` → the trace's topic-bearing spans as a `TraceView`; null (→
    `NotFound`) when Jaeger has no such trace or it carried no Benzene span.
  - `GetCorrelationAsync` → a `benzene.correlation-id` **tag** search fanned across services, deduped by
    trace id, grouped earliest-first into a `CorrelationView` (same ordering as every other plane).
  - `GetRecentFlowsAsync` → a per-service recent search, deduped, mapped to `TraceSummary` rows. Because
    Jaeger search returns **full** traces (not summaries), these carry a **real** span count (`Events`),
    touched-service list, and `Failed` flag (via the collector's `BenzeneResultStatusExtensions.IsSuccess`
    success class) — richer than the Tempo/X-Ray summary path, with no second fetch.
  - Reachable-but-unsuccessful (HTTP error / malformed body) → null/empty (the topology adapter's rule); a
    connection failure throws and the composite's fetch-isolation degrades that slice.
- `JaegerTraceMapper.MapTraces(body)` → one `JaegerMappedTrace` (id + events) per trace in `data[]`. Maps
  Jaeger's own model, which differs from OTLP/Tempo: times are **microseconds** (`startTime`/`duration`),
  parentage is a `references` entry with `refType == "CHILD_OF"` (not `parentSpanId`), and the service is
  `processes[processID].serviceName` (not a resource attribute). Benzene tag keys are read by their dotted
  names verbatim (Jaeger preserves keys — no sanitising).
- `JaegerTraceSourceOptions(jaegerUrl)` — `Services` (the set to search; discovered via `GET /api/services`
  when null/empty), `CorrelationLookback` (24h), `RecentFlowsLookback` (1h), `SearchLimitPerService` (20).

## The Jaeger search constraint (why the fan-out)
Jaeger's search API **requires a `service`** — there is no "all services" query (unlike Tempo's TraceQL or
X-Ray's `GetTraceSummaries`). So a fleet-wide correlation or recent-flows search must **enumerate services**
and query each, then merge + dedupe by trace id (a cross-service trace comes back from each of its
services). The service set is the configured `Services` or, when unset, the ones `GET /api/services`
returns. This is a real per-service fan-out (N services → N searches, each capped at
`SearchLimitPerService`); pin `Services` to bound it or scope it to the mesh's own services.

## What it deliberately does NOT do
Per `IMeshTraceSource`, no per-topic/service counts and no service health (traces are sampled; Jaeger has
no heartbeat feed). Topic stats compose from an `IMeshUsageSource`; with none wired the composite serves
traces/correlation/recent-flows/services and reports honest-empty topic stats. Per-service and single-topic
pages stay omitted on this plane (no descriptor feed).

## Prerequisites it relies on
The pipeline must stamp `benzene.topic`/`benzene.version`/`benzene.status`/`benzene.correlation-id` on the
topic-bearing span (`Benzene.Diagnostics.ActivityMiddlewareDecorator`), and those spans must reach Jaeger.
Jaeger stores span tags searchably, so the correlation tag filter works without any indexing choice.

## Verification caveat
`JaegerTraceMapper`, the tag-search construction, the service fan-out/dedupe, and the response parsing are
unit-tested against Jaeger's **documented** API shapes (`test/Benzene.Mesh.Test/JaegerTraceSourceTest.cs`,
mocked `HttpClient`), covering trace mapping + non-Benzene-span filtering, correlation fan-out + dedupe +
ordering, recent-flows full-trace mapping (event count + failure), service discovery, and the null cases.
It has **not** been run against a **live** Jaeger instance — the same egress limitation as the other
adapters. Treat the API paths (`/api/traces/{id}`, `/api/traces?service=…&tags=…`, `/api/services`), the
Jaeger trace JSON model, and the microsecond time unit as "per Jaeger's public documentation, not
independently confirmed" until verified against a real instance.

## Dependencies
- **Benzene.Mesh.Collector** — `IMeshTraceSource`/`IMeshFleetReadModel`/`CompositeMeshFleetReadModel`/
  `TraceView`/`TraceSummary`/`CorrelationView`/`MeshTraceEvent` (via `Benzene.Mesh.Wire`), the
  `BenzeneResultStatusExtensions` success class, and `MeshCollectorHandlers.Queries`.
- **Benzene.Abstractions** — `IBenzeneServiceContainer` for the DI extension. Uses `System.Net.Http`'s
  `HttpClient` and `System.Text.Json` directly, matching the `Benzene.Mesh.*` family.
