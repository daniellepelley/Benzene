# Scope: OTel-backed Fleet read-model adapter (2026-07-23)

**Status:** SCOPED, not yet built. Owner: `mesh-product-owner` (product) + `observability-product-owner`
(the two span-attribute data requirements in §6).

## Goal
Let the **Fleet UI** (`mesh-fleet-ui.html`, the collector/live plane — traces, waterfall, correlation
lookup) read from **various existing observability sources**, composed per-signal, instead of requiring
services to push to the in-memory push-collector. The maintainer's ask: *"work with various sources; for
the AWS demo it would likely be X-Ray and CloudWatch"* — i.e. **traces from X-Ray, stats from CloudWatch**
(the existing usage adapter), health absent, all behind the same `mesh:query:*` topics. Non-AWS targets
(Grafana Tempo, Jaeger) plug into the same trace-source seam.

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

## Architecture / the seam — MULTI-SOURCE by design (maintainer direction, 2026-07-23)
The maintainer wants this to work with **various sources**, composing per-signal (not one backend doing
everything). This matches the "read what each backend has" principle exactly: **traces** come from a
trace store, **stats** from a metrics store, **health** from a heartbeat feed — each pluggable.

The query handlers (`TraceQueryMessageHandler`, `FleetQueryMessageHandler`, …, `Handlers.cs`) currently
depend on the **concrete** `MeshCollectorStore` (read methods `Fleet()`/`Service(name)`/
`Topic(id,ver)`/`Trace(id)`/`Correlation(id)`). Make the read side backend-swappable **and composable**:

1. Extract `IMeshFleetReadModel` over those five read methods (nullable-return contract preserved).
2. `MeshCollectorStore` implements it (no behaviour change — the push-collector deployment is unchanged;
   it stays the all-in-one "everything from the ring" implementation).
3. Add a **composing** implementation, `CompositeMeshFleetReadModel`, that fans each read-model out to
   the right per-signal source:
   - **`IMeshTraceSource`** — the trace-shaped read-models (`Trace`, `Correlation`, recent-flows +
     observed topic consumers from span parentage). Pluggable implementations: **AWS X-Ray**, **Grafana
     Tempo**, **Jaeger** (and the in-memory ring is trivially one too).
   - **`IMeshUsageSource`** — *already exists* (`Benzene.Mesh.Contracts`, shipped **CloudWatch** + **App
     Insights** adapters). Reuse it for per-topic/per-service **stats** in the `FleetView` — do **not**
     re-derive counts from sampled traces. This is the whole reason stats are omitted from the trace
     source.
   - **health** — from the heartbeat feed if present; absent on a trace/metrics-only backend →
     `Health = unknown`, `MissingFeeds += "health"`.
4. The query handlers depend on `IMeshFleetReadModel` (the composite or the in-memory store, by DI).
5. The push-ingest handlers (`mesh:register`/`heartbeat`/`traces`) are **not registered** in a
   backend-composed deployment — services export to X-Ray/Tempo/CloudWatch directly, not to a collector.

**No mesh wire-contract change, no Cloud Service spec change — purely new read-side adapters behind the
same `mesh:query:*` topics/shapes.** The existing `mesh-collector-cases.json` query shapes are a
**forcing function** the composite must still satisfy. The Fleet UI is untouched (same topics).

### Why this unlocks the AwsMesh plane
AwsMesh already exports traces to **X-Ray** and metrics to **CloudWatch**, and its mesh Lambda already
serves `mesh-ui.html`. Adding `.UseMeshFleetUi(...)` on that same Lambda, wired to a
`CompositeMeshFleetReadModel(XRayTraceSource, CloudWatchUsageSource, health=absent)`, brings the **Fleet
UI + trace waterfall onto the AwsMesh plane the maintainer actually uses** — closing the earlier
"traces on AwsMesh" gap **without** standing up a push-collector (which Lambda's in-memory ring can't host).

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

## `IMeshTraceSource` backends (build order — maintainer's AWS demo first)
Build the **`IMeshTraceSource` abstraction** once, then implementations in this order:
1. **AWS X-Ray — build first (the maintainer's AWS demo target).** `AwsMesh` already exports to X-Ray +
   CloudWatch, so an X-Ray trace source + the existing `CloudWatchUsageSource` composes the whole AWS
   fleet read-model and lights up the Fleet UI on the AwsMesh plane. `GetTraceSummaries` ≈ recent-flows +
   correlation (annotation filter); `BatchGetTraces` ≈ the waterfall. Package `Benzene.Mesh.Fleet.Aws.XRay`,
   `AWSSDK.XRay` only. Caveat: X-Ray's data model is segments/subsegments (not raw OTLP spans) — the
   span→`MeshTraceEvent` mapping reads X-Ray segment fields + `annotations` (topic/status/correlation as
   **annotations**, since only annotations are filterable — see §6b).
2. **Grafana Tempo (trace API).** The OTLP-native reference target for non-AWS. TraceQL is a real query
   language (`/api/traces/{id}` + `/api/search` with attribute filters). Package `Benzene.Mesh.Fleet.Tempo`
   — **distinct** from `Benzene.Mesh.Tracing.Tempo` (a **PromQL service-graph** client); this is a
   **trace-API** client. Keeping them separate protects the "PromQL, not trace-API" invariant.
3. **Jaeger** (query API) — lower priority, no existing Benzene footprint.
- **"OTLP-generic" target: NO.** OTLP is a push/export protocol with **no query API** — you cannot read
  from OTLP. "OTel-backed" means "an OTLP-fed store," queried via its own API, never "query OTLP."
- **The stats source is not a choice to build** — it's the **existing** `IMeshUsageSource`
  (`CloudWatchUsageSource` for AWS, `ApplicationInsightsUsageSource` for Azure), composed in for
  `FleetView` counts. Reuse, don't rebuild.

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

- **(a) Fine-grained status as a span tag.** ✅ **DONE (2026-07-23).** `ActivityMiddlewareDecorator`
  now tags **`benzene.status`** (the full kebab wire status, e.g. `not-found`, or `exception` on throw)
  on the topic-bearing span, read from `IHasMessageResult.MessageResult` after `next()`. The
  trace-backed `Status` is now the real wire status, not ok/error only.
- **(b) Correlation id as a *searchable* span attribute.** ✅ **DONE (2026-07-23).**
  `ActivityMiddlewareDecorator` now tags **`benzene.correlation-id`** on the topic-bearing span, only
  when the message carried `x-correlation-id` (never the auto-generated GUID). Benzene-side is complete;
  the remaining deployment step is exporter config — X-Ray only filters on **annotations**, so
  `benzene.correlation-id` must be indexed as an annotation (`benzene_correlation_id`) for
  `mesh:query:correlation` to find matches.

**Granularity nuance:** Benzene emits a span **per middleware**, but a `MeshTraceEvent` is **per handled
message**. The adapter must select the message-representative span (the one carrying `benzene.topic` /
the message-handler span), not every middleware span — else the waterfall shows one bar per middleware.

## Phasing (keep increment 1 small; AWS demo is the target)
- **Increment 0 — the seams (tiny, no backend).** Extract `IMeshFleetReadModel` (query handlers depend
  on it; `MeshCollectorStore` implements it, unchanged) and declare `IMeshTraceSource`. Pure refactor,
  fully covered by the existing collector tests + `mesh-collector-cases.json`.
- **Increment 1 — `mesh:query:trace` against X-Ray.** ✅ **DONE (2026-07-23).** `BatchGetTraces` →
  segment tree → `TraceView` waterfall, in `Benzene.Mesh.Fleet.Aws.XRay`, behind `IMeshTraceSource`.
  Mocked-`IAmazonXRay` tests. Full wire-status colouring (§6a landed alongside).
- **Increment 2 — `mesh:query:correlation` (X-Ray).** ✅ **DONE (2026-07-23).** `GetTraceSummaries`
  filtered on `annotation.benzene_correlation_id` (paged) → `BatchGetTraces` (chunked to 5) →
  group-by-trace `CorrelationView`, reusing increment-1's mapping. §6b landed alongside. Runtime caveat:
  the exporter must index the correlation id as an X-Ray annotation (deployment config, not code).
- **Increment 3 — `CompositeMeshFleetReadModel` + recent-flows `FleetView`.** ✅ **DONE (2026-07-23).**
  Compose `IMeshTraceSource` (recent flows + anonymous services) with the registered `IMeshUsageSource`s
  (CloudWatch per-topic stats); `.UseMeshFleetUi(...)` + the composite wired onto the AwsMesh mesh Lambda
  → **Fleet UI on the AwsMesh plane.** The four locked rulings (mesh-product-owner, 2026-07-23):
  - **Topic stats** — one `TopicSummary` per (topic, version=null) from usage entries: `Invocations`=Σcount,
    `Errors`=Σcount where the `result` tag is an error, `StatusCounts`=raw `result` token→Σ. The error rule
    is the **metric `result` vocabulary** (`docs/mesh-usage-feed.md` §1), i.e. `status is not null &&
    != "success" && != "<missing>"` — **NOT** `BenzeneResultStatus.IsFailure`/`!IsSuccess`, which read the
    wire vocabulary and would miscount `exception`/`failure` as non-errors and `success` as an error.
  - **Services** — name-only from the recent flows' `ServiceIds` (no extra API call): `Invocations`/`Errors`
    absent, `Health=unknown`, `MissingFeeds=[descriptor,health,stats]` — the push collector's shipped
    "anonymous-but-live from traffic" rule, sourced from X-Ray. (Refinement: the earlier shorthand
    "`MissingFeeds=[health,descriptor]` NOT stats" conflated **topic** stats (present from CloudWatch) with
    **per-service** stats (absent — no service dimension). Topics carry no `stats` marker; **service rows do**.)
  - **absent ≠ zero** — non-nullable fields + markers (nullable would break the fixtures' subset match).
    Reuse `ServiceSummary.MissingFeeds`; **add `TopicSummary.MissingFeeds`** (`[descriptor,duration]` on
    this plane). UI renders "—" when the row's `MissingFeeds` names the dimension.
  - **recent flows** — new `IMeshTraceSource.GetRecentFlowsAsync(limit=20)`; X-Ray = one unfiltered
    `GetTraceSummaries` over a 1h window mapped summary→`TraceSummary` (`Events=0`, no span count),
    **zero `BatchGetTraces` on fleet load** (the accurate count is one `GetTraceAsync` away on drill-in).
- **Increment 4 — Tempo `IMeshTraceSource`** (`Benzene.Mesh.Fleet.Tempo`), the non-AWS reference target,
  reusing increments 0–3's handlers/composite unchanged (that's the payoff of the abstraction). Jaeger later.
- **Not in scope:** deriving health or per-topic **counts** from a trace source. Health stays the
  heartbeat feed; counts stay `IMeshUsageSource`. The trace source is a trace/correlation/recent-flows reader.

## Verification caveat (state every time)
Like the Tempo PromQL adapter, this adapter's TraceQL construction, response parsing, and span→event
mapping are covered by **mocked-HTTP tests against documented API shapes** — **shipped-but-unverified
against a live Tempo trace API** until run against a real instance (the egress limitation that blocked
live-verifying the PromQL adapter applies). Attribute-name mapping is documented convention, not confirmed.

## Recommendation
Build the **multi-source composite** (maintainer direction): a pluggable `IMeshTraceSource` for the
trace-shaped read-models + the existing `IMeshUsageSource` for stats + heartbeat (or absent) for health,
behind `IMeshFleetReadModel`. **Reject any framing where the trace source reports health or aggregate
counts** — those come from their own sources; that line keeps it honest. **Build order = the maintainer's
AWS demo first:** X-Ray trace source + the shipped `CloudWatchUsageSource`, which composes the whole AWS
fleet read-model and brings the Fleet UI + waterfall onto the AwsMesh plane. Tempo/Jaeger reuse the same
handlers/composite afterward. First code: Increment 0 (the two seams) + Increment 1 (`mesh:query:trace`
against X-Ray).
