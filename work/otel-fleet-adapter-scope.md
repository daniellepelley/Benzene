# Scope: OTel-backed Fleet read-model adapter (2026-07-23)

**Status:** SCOPED, not yet built. Owner: `mesh-product-owner` (product) + `observability-product-owner`
(the two span-attribute data requirements in §6).

## Goal
Let the **Fleet UI** (`mesh-fleet-ui.html`, the collector/live plane — traces, waterfall, correlation
lookup) read from an existing **OTel trace backend** (Grafana Tempo / Jaeger / AWS X-Ray) instead of
requiring services to push to the in-memory push-collector. The maintainer's ask: *"surface the trace
waterfall against my existing OTel store, without standing up a Benzene push-collector."*

## Today (for contrast)
The five `mesh:query:*` read-models (`fleet`/`service`/`topic`/`trace`/`correlation`) are answered by
`MeshCollectorStore` — an **in-memory** ring buffer (4096 events) + dicts, fed by services **pushing**
`MeshTraceEvent`s over the `mesh:traces` wire topic (`HttpMeshTraceExporter`; Benzene-native tracing,
not OTLP). Health comes from a separate `mesh:heartbeat` feed; descriptors from `mesh:register`. **None
of the heartbeat/descriptor data exists in a trace backend** — this single fact drives every coverage
verdict below.

Precedent: the mesh already reads observability backends for the *other* two signals —
`Benzene.Mesh.Tracing.Tempo` reads a **PromQL service-graph** for `topology.json`; the CloudWatch /
App Insights adapters read metrics backends for `usage.json`. Governing principle (from those): **read
what the backend actually has; degrade honestly on missing dimensions; never invent.**

## Architecture / the seam
The query handlers (`TraceQueryMessageHandler`, `FleetQueryMessageHandler`, …, `Handlers.cs`) currently
depend on the **concrete** `MeshCollectorStore` (read methods `Fleet()`/`Service(name)`/
`Topic(id,ver)`/`Trace(id)`/`Correlation(id)`). Make the read side backend-swappable:

1. Extract `IMeshFleetReadModel` over those five read methods (+ their nullable-return contract).
2. `MeshCollectorStore` implements it (no behaviour change — the push-collector deployment is unchanged).
3. The new adapter is a **second implementation** behind the **same** `mesh:query:*` topics and the
   **same** `TraceView`/`CorrelationView`/`FleetView` shapes. The Fleet UI is untouched.
4. The push-ingest handlers (`mesh:register`/`heartbeat`/`traces`) are simply **not registered** in an
   OTel-backed deployment — services export OTLP to the backend directly instead of pushing.

**No mesh wire-contract change, no Cloud Service spec change — purely a new read-side adapter.** The
existing `mesh-collector-cases.json` query shapes become a **forcing function** the adapter must satisfy.

## Per-read-model coverage (mesh-product-owner ruling)
A trace store holds spans, queryable by trace id and by attribute, with recent-trace search. It is not a
metrics store and has no heartbeat/descriptor feed.

| Read-model | Verdict | Basis |
|---|---|---|
| `mesh:query:trace` | **FULL** | trace-by-id → spans → `MeshTraceEvent` per span. The core; native shape of a trace store. |
| `mesh:query:correlation` | **FULL (conditional)** | attribute search by correlation id + client-side group-by-trace. Needs correlation id as a **searchable/indexed** span attribute (§6b). |
| `fleet` → recent flows (`Traces`) | **DEGRADED-feasible** | trace-search over a window → `TraceSummary` rows. Loses the exact "20 newest from the ring"; becomes "recent traces over a search window." |
| `fleet` → per-service/per-topic **stats** (invocations/errors/avg) | **OMIT → usage feed** | traces are **sampled**; aggregating counts is sampling-biased + expensive. `usage.json` already answers this honestly. Emit **absent, not 0**. |
| `fleet` → service **health** | **OMIT → `unknown`** | not in traces. `Health = unknown`, `MissingFeeds += "health"`. Never fabricated. |
| `fleet` → topic catalog | **DEGRADED (observed-only)** | consumers derivable from span parentage (same at-query-time derivation the collector does); **declared providers need the descriptor feed → absent**. |
| `mesh:query:service` | **OMIT (for now)** | descriptor/heartbeats/`hashMatches` absent — the view is gutted. Also in no current UI. |
| `mesh:query:topic` | **OMIT (for now)** | declared providers + stats absent. Also in no current UI. |

**Net:** a trace-backed adapter is a **trace / correlation / recent-flows reader**. Health and aggregate
stats are structurally out of reach and stay on the heartbeat plane and the usage feed respectively.

## Backend priority
1. **Grafana Tempo (trace API) — build first.** Consistency (we already target Tempo operators),
   TraceQL is a real query language (`/api/traces/{id}` + `/api/search` with attribute filters), OTLP-
   native reference store. **Distinct package `Benzene.Mesh.Fleet.Tempo`** — do NOT fold into
   `Benzene.Mesh.Tracing.Tempo`, which is a **PromQL service-graph** client; this is a **trace-API**
   client. Keeping them separate protects that "PromQL, not trace-API" invariant.
2. **AWS X-Ray** — natural for `AwsMesh` (already exports to X-Ray). `GetTraceSummaries` ≈ recent-flows +
   correlation (annotation filter); `BatchGetTraces` ≈ the waterfall.
3. **Jaeger** (query API) — lower priority, no existing Benzene footprint.
- **"OTLP-generic" target: NO.** OTLP is a push/export protocol with **no query API** — you cannot read
  from OTLP. "OTel-backed" means "an OTLP-fed store," queried via its own API, never "query OTLP."

## Honest degradation / UI framing
When the Fleet UI is trace-backed:
> Traces, waterfalls, and correlation lookup are live against your OTel store. **Service health is
> unknown** — no heartbeat feed on a trace backend. **Per-topic/per-service counts come from the usage
> feed if wired, otherwise unavailable — never estimated from sampled traces.** Topic consumers are
> observed from trace parentage; **declared providers require the descriptor feed and are absent.**

Reuse the existing degradation vessel, add one banner:
1. `MissingFeeds` already renders `◌ unknown` + reduced rows — a trace-backed `FleetView` sets
   `MissingFeeds = ["health","descriptor","stats"]` and `Health = "unknown"`. No new mechanism.
2. **Backing banner** ("Backed by: Grafana Tempo — traces only"), mirroring the static-vs-live *plane*
   language already in the vision doc. Small UI affordance, not a contract change.
3. **Absent ≠ zero (honesty gap to close).** `Invocations`/`Errors`/`AvgDurationMs` are non-nullable and
   default to `0`; a trace-backed view must not render "0 invocations" for "no stats feed." The UI keys
   off `MissingFeeds.contains("stats")` to render "—" instead of `0`. (Cheaper than making the fields
   nullable; reuses the existing degradation contract.)

## Prerequisites / data requirements (route to observability-product-owner)
These are span-attribute conventions on **already-emitted** Benzene spans — they do **not** widen the
Cloud Service spec; they're the trace-store analogue of the Tempo metric/label convention. Verified:
`ActivityMiddlewareDecorator.cs` already tags `benzene.topic`/`benzene.version`/`benzene.transport`/
`benzene.handler`, and `service.name`/`service.instance.id` come from OTel resource attrs — so the
span→event mapping is **mostly already emitted**. Two gaps:

- **(a) Fine-grained status as a span tag.** Today the span only sets `ActivityStatusCode.Error` on
  exception. `MeshTraceEvent.Status` is the full Benzene wire status (the metric path already itemizes
  it — `MetricsExtensions.cs`). Without a `benzene.status` (kebab wire status, e.g. `not-found`) span
  tag, the trace-backed `Status` **degrades to ok/error only**. Decide: add the tag (cheap, additive to
  `Benzene.Diagnostics`) or accept ok/error and document it. *Blocks increment-1 completeness of the
  waterfall's success/failure colouring beyond ok/error.*
- **(b) Correlation id as a *searchable* span attribute.** Correlation flows through log scope/
  enrichment today; `mesh:query:correlation` needs it as an **indexed, filterable** span attribute
  (X-Ray *annotation*, not metadata). *Blocks increment 2.*

**Granularity nuance:** Benzene emits a span **per middleware**, but a `MeshTraceEvent` is **per handled
message**. The adapter must select the message-representative span (the one carrying `benzene.topic` /
the message-handler span), not every middleware span — else the waterfall shows one bar per middleware.

## Phasing (keep increment 1 small)
- **Increment 1 — `mesh:query:trace` against Tempo.** trace-by-id → span list → `TraceView` waterfall.
  One backend, one handler, mocked-HTTP tests to the documented TraceQL/attribute convention. Delivers
  exactly the ask. Shippable alone. (Status colouring is ok/error until §6a lands.)
- **Increment 2 — `mesh:query:correlation`.** attribute search by correlation id + group-by-trace;
  reuses increment-1's mapping. Gated on §6b.
- **Increment 3 — recent-flows only (`FleetView.Traces`).** search over a window → `TraceSummary` rows;
  `Services` observed-only, `Topics` observed-consumers-only, every row
  `MissingFeeds=[health,descriptor,stats]`, `Health=unknown`. Requires the UI absent-≠-zero fix.
- **Increment 4 — decision gate (NOT pre-committed): NO.** Fleet-aggregate/health does not belong on a
  trace-backed adapter. Health stays heartbeat/collector-plane; aggregate stats stay the usage feed.
  Revisit only if a compelling merged-plane story emerges.

## Verification caveat (state every time)
Like the Tempo PromQL adapter, this adapter's TraceQL construction, response parsing, and span→event
mapping are covered by **mocked-HTTP tests against documented API shapes** — **shipped-but-unverified
against a live Tempo trace API** until run against a real instance (the egress limitation that blocked
live-verifying the PromQL adapter applies). Attribute-name mapping is documented convention, not confirmed.

## Recommendation
Approve the concept; build to the phasing above. **Reject any framing where a trace-backed Fleet reports
health or aggregate stats** — that line keeps it honest. First increment: `Benzene.Mesh.Fleet.Tempo`'s
`mesh:query:trace` handler.
