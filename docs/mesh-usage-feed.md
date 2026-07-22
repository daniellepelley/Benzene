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
| `result` | `success` / `failure` (the wire vocabulary's success class), `"<missing>"` when no result signal |

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
  observed" state, not a fabricated zero-row.
