# The Mesh Usage Feed

How the Benzene mesh learns **how often each topic is actually exercised, and over which
transports** — the signal the structural catalog can't give. `topics.json` can prove a topic is
*wired*; only observed traffic can prove it's *used* (or safe to deprecate).

Usage is deliberately an **observability concern, not a Cloud Service spec concern**: it is not
part of any service's request/response surface, it adds **no new required endpoints** to a
service, and the spec endpoints (`spec`, `healthcheck`, …) are untouched. The whole feed is built
from three loosely-coupled pieces:

```
service                      metrics backend                aggregator
UseBenzeneMetrics()  ──────▶ App Insights / CloudWatch ───▶ IMeshUsageSource adapter ──▶ usage.json ──▶ Mesh UI
(topic/transport/result      / OTel collector / …            (reads the backend,
 tags, per message)                                            reports MeshUsage)
```

## 1. The metric metadata standard (the load-bearing piece)

Every Benzene service can emit, **per handled message**, two instruments on the shared `"Benzene"`
`Meter` (see `Benzene.Diagnostics`):

| Instrument | Type | Meaning |
|---|---|---|
| `benzene.messages.processed` | `Counter<long>` | one increment per handled message |
| `benzene.message.duration` | `Histogram<double>` | handling duration, milliseconds |

both tagged with the **standard metadata set**:

| Tag | Value |
|---|---|
| `topic` | the message's topic id (`"<missing>"` when unresolvable) |
| `transport` | the transport the message arrived over (`"<missing>"` when unresolvable) |
| `result` | outcome of handling — **successes collapse to `success`**, **failures are itemized by root cause** (see below); `"<missing>"` when no result signal |

> **The `result` collapse rule is part of the standard** (a 2026-07-23 change to the value set —
> keys and instrument names are unchanged): emit **`success`** for any *successful* outcome
> (`IHasMessageResult.MessageResult.IsSuccessful` — the bool, so a result that stays successful while
> carrying a failure-class status like a health check's `service-unavailable` is still `success`); the
> failure's **status string verbatim** (`not-found`, `unauthorized`, `conflict`, `validation-error`, …)
> for an unsuccessful result; **`exception`** when the pipeline threw (distinct from a handler that
> *returned* `unexpected-error`); and `"<missing>"` when no result was recorded. This keeps success
> cardinality at 1 (you want the total, not `ok` vs `created`) while making failures diagnosable — a
> mostly-`not-found` failure mix reads very differently from a mostly-`unauthorized` one. The failure
> vocabulary is a **bounded** set (`BenzeneResultStatus`), so cardinality stays a small constant;
> cost-shaping, if ever needed, belongs backend-side (a metric filter), never in the emitted standard.
> Pre-1.0, flagged per convention: a backend's rolling window may briefly hold both the old
> `success`/`failure` values and the new ones — both consumers (the two usage adapters, the Mesh UI's
> "By status" panel, the aggregator's edge error-rate classifier) tolerate the overlap.

Emission is explicit opt-in — add `UseBenzeneMetrics()` to the pipeline — and export is whatever
OTel wiring the host already has (`Benzene.OpenTelemetry`'s
`AddBenzeneInstrumentation(MeterProviderBuilder)`). This tag set is the standard: it's what lets
*different* backends yield the *same* usage signal. An adapter never needs Benzene running to
query it — it just needs these tags to have reached its backend.

## 2. Adapters: `IMeshUsageSource` → `usage.json`

An adapter implements `Benzene.Mesh.Contracts.IMeshUsageSource` (a zero-I/O port, same dependency
footprint as `IMeshReportPublisher`): query your backend, translate the result into a
`MeshUsage` report. Register any number of them in the aggregator's host;
`MeshAggregator.RunOnceAsync` polls them all (each bounded by the same 10-second per-fetch
timeout as a service poll — a throwing/hung adapter contributes nothing and never fails the run),
merges the reports, and publishes **`usage.json`** next to `manifest.json`/`topics.json`/
`topology.json`.

`MeshUsage`/`MeshUsageEntry` rules:

- **An entry is a count at exactly the dimensions it states** — `topic` (required) plus nullable
  `version`/`service`/`transport`/`status`. Entries from one source never overlap; consumers
  aggregate by grouping over whichever stated dimensions they need.
- **`null` means "my backend doesn't have this dimension", never "all"** — report exactly what
  you can prove, the UI surfaces the gap (see §3).
- **Every entry names its `source`** (the `TopologyEdge.Source` precedent), so one `usage.json`
  can merge a CloudWatch adapter and the collector bridge without losing attribution.
- **Artifact absence vs. empty entries are different statements**: no `usage.json` = no usage
  feed wired (the UI hides its usage surfaces entirely); an empty `entries` array = the feed is
  wired and nothing was observed — which for a deprecation decision is exactly the evidence you
  wanted.

### The shipped adapter: the collector bridge

`Benzene.Mesh.Collector.CollectorUsageSource` reports the collector's cumulative per-topic stats
as entries per (topic, version, status) — for hosts that run the collector's handlers alongside
the aggregator. The trace wire shape carries no transport and the per-status counts aren't
attributed per handling service, so those dimensions are reported as absent — honestly exercising
the degradation path rather than guessing. A metrics-backend adapter (reading the §1 tags from
Application Insights or CloudWatch) is the intended way to fill them in; those adapters need
their backend SDKs and therefore ship as their own packages, not from the core mesh packages.

### The shipped adapter: CloudWatch

`Benzene.Mesh.Usage.CloudWatch.CloudWatchUsageSource` (own package, pins `AWSSDK.CloudWatch`) is the
first metrics-backend adapter. It reads the §1 `benzene.messages.processed` counter back from CloudWatch —
where a service's OpenTelemetry pipeline exported it, e.g. via the ADOT collector's `awsemf` (EMF) exporter
— and reports counts per **(topic, transport, status)** over a configured `TimeWindow` (default 24h, the
window the UI shows). It carries the `transport` and outcome dimensions the collector bridge can't, but no
per-service dimension (the counter isn't tagged by service), so it honestly reports `service` as absent.
Register it with `AddCloudWatchUsage(new CloudWatchUsageOptions(...))` alongside `AddMeshAggregator(...)`.
It lists the metric's live dimension combinations then sums each with one `GetMetricData` query, so every
entry's dimensions are known exactly. **It assumes delta temporality** on the exported counter (the EMF
default) so a CloudWatch `Sum` over the window equals the request count; a cumulative export would
over-count. Wired end-to-end in `examples/AwsMesh` (see its README). Coarse counts by design — fine-grained
analysis stays in CloudWatch/Grafana.

### The shipped adapter: Application Insights (Azure)

`Benzene.Mesh.Usage.ApplicationInsights.ApplicationInsightsUsageSource` (own package, pins
`Azure.Monitor.Query` + `Azure.Identity`) is the Azure sibling of the CloudWatch adapter. The counter is
exported to Application Insights by the **Azure Monitor OpenTelemetry exporter**
(`Azure.Monitor.OpenTelemetry.Exporter`), landing in the Log Analytics `customMetrics` table with the tags
in `customDimensions`. This source reads it back with a KQL query
(`customMetrics | where name == "benzene.messages.processed" | summarize sum(valueSum) by topic, transport,
result`) over a configured `TimeWindow`, reporting counts per **(topic, transport, status)** — same
dimensions and same missing-`service` degradation as CloudWatch. Register it with
`AddApplicationInsightsUsage(new ApplicationInsightsUsageOptions(workspaceId, ...))` alongside
`AddMeshAggregator(...)`; the querying identity needs the **Log Analytics Reader** role on the workspace.
The Azure Monitor exporter uses **delta** temporality by default, so `sum(valueSum)` = the request count.
Wired end-to-end in `examples/AzureMesh` and `examples/AzureFunctionsMesh` (see their READMEs).

## 3. Degradation (normative, consumer side)

The Mesh UI (`mesh-ui.html`) renders usage on all three entity pages — a Usage column on the
estate's topic table, a usage panel on the topic page, and a usage section on the service page
(service-attributed entries when the feed has them; clearly-labeled fleet-wide counts for the
service's topics when it doesn't). Every panel:

- renders **only the dimensions present** (transport chips only if some entry carries a
  transport, and so on);
- surfaces a missing dimension as a **data-quality footnote inside the panel** — findable, but
  off the primary screen, per the product ruling — naming what the feed doesn't carry and that
  the fix is adapter-side, not a UI setting;
- never invents a number: an unexercised topic shows the explicit "feed is wired, no traffic
  observed" state, not a fabricated zero-row;
- **reconciles the "By status" breakdown with the total.** The `result=<missing>` sentinel (a
  message counted with no recorded success/failure outcome) is never rendered as if it were a real
  status, but its **count is still accounted for**: when a "By status" row is shown, the unrecorded
  count is folded into a neutral **`(no outcome recorded)`** bucket so the status chips sum to the
  same total as the "By transport" row. This supersedes an earlier "just hide `<missing>`" approach,
  which dropped the count and left the two breakdowns silently disagreeing (the same messages,
  different totals). Eliminating `<missing>` is a **backend** fix — the pipeline recording a
  `MessageResult` (see `docs/message-result.md` / `Benzene.Core.MessageHandlers`'
  `MessageResultRecorder`) — but the panel stays honest whatever the rolling metric window still
  holds. When a feed carries **no** real status at all (only `<missing>`/null), no "By status" row is
  shown and the missing-`status` data-quality footnote covers it instead.
