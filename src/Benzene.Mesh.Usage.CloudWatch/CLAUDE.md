# Benzene.Mesh.Usage.CloudWatch

## What this package does
The **CloudWatch usage adapter** for the mesh: an `IMeshUsageSource` (`Benzene.Mesh.Contracts`) that reads
the `benzene.messages.processed` counter back from **CloudWatch** and reports it as the mesh usage feed —
a count per **(topic, transport, status)** over a configured window. The aggregator merges it into
`usage.json`, and the Mesh UI renders per-topic request counts. Pins **only** `AWSSDK.CloudWatch`.

This is the aggregator-side reader half of the usage story. The emit half already exists everywhere:
`UseBenzeneMetrics()` (`Benzene.Diagnostics`) emits `benzene.messages.processed` tagged
`topic`/`transport`/`result`, and `Benzene.OpenTelemetry` exports the `Benzene` meter over OTLP. This
package assumes those metrics reached CloudWatch (e.g. via the ADOT collector's `awsemf` exporter) and
turns them back into the `MeshUsage`/`MeshUsageEntry` shape. See `docs/mesh-usage-feed.md` for the
end-to-end feed and the metric-metadata standard.

It is the CloudWatch sibling of `Benzene.Mesh.Tracing.Tempo` (which reads a Prometheus-compatible backend
for topology) — the same "query a metrics backend, aggregate, feed an artifact" shape, but plugged into the
aggregator's per-run `IMeshUsageSource` loop rather than a separate handler.

## Key types
- `CloudWatchUsageSource : IMeshUsageSource` — `FetchUsageAsync` lists the metric's live dimension
  combinations (`ListMetrics`), then sums each with one `GetMetricData` `Sum` query over the window, and
  maps each to a `MeshUsageEntry(topic, transport, status=result, count)`. `version`/`service`/
  `avgDurationMs` are left `null` (the counter doesn't carry them) — the missing-dimension degradation
  path. Listing then summing (rather than a grouped Metrics-Insights query) keeps every entry's dimensions
  known exactly instead of parsed from a label. An empty result → empty `Entries` ("wired, no traffic"),
  never `null`; genuine connection failures propagate (the aggregator bounds and skips a throwing source).
- `CloudWatchUsageOptions` — `Namespace` (default `"Benzene/Mesh"`), `MetricName` (default
  `"benzene.messages.processed"`), `TimeWindow` (default 24h — the window the UI shows), the three
  dimension names (default `topic`/`transport`/`result`), and `PeriodSeconds` (default 60).
- `Extensions.AddCloudWatchUsage(this IBenzeneServiceContainer, CloudWatchUsageOptions)` — registers the
  options, a default `IAmazonCloudWatch`, and the source as `IMeshUsageSource`. Requires
  `AddMeshAggregator(...)` in the same container (the aggregator resolves every `IMeshUsageSource`).

## Critical assumption: delta temporality
A CloudWatch `Sum` over the window equals the request count **only if** the counter was exported with
**delta** temporality (each export is that interval's delta) — the ADOT `awsemf` exporter's default. A
**cumulative** export would make `Sum` over-count badly. When wiring the collector, keep the counter delta
(the `awsemf` exporter does this by default; don't force cumulative temporality on the metrics pipeline).

## When to use this package
- On an AWS-hosted Benzene mesh whose services export `benzene.messages.processed` to CloudWatch, to light
  up the Mesh UI's usage surfaces (per-topic counts, by-transport / by-status breakdowns, the window).
- It is coarse-grained by design (request counts over a window) — fine-grained analysis belongs in
  CloudWatch/Grafana directly, not here.

## Deliberate boundaries (NOT shipped)
- **No per-service dimension.** The counter isn't tagged by service, so `MeshUsageEntry.Service` is `null`.
  Promoting the OTel `service.name` resource attribute to a CloudWatch dimension (via the EMF exporter) and
  reading it here is a documented follow-up; it raises metric cardinality/cost, so it's opt-in, not default.
- **No duration.** `AvgDurationMs` is `null`; the `benzene.message.duration` histogram could fill it with a
  second `Average` query — a follow-up, kept out of v1 to stay focused on counts.
- **Configured `TimeWindow` is the default, not the only window (2026-07-24).** `FetchUsageAsync` now takes an
  optional `MeshUsageWindow?`: when the composite fleet reader passes one (driven by the mesh UI's time-range
  picker), the `GetMetricData` query runs over exactly those bounds and the returned `MeshUsage` echoes them; with
  none (the aggregator's `usage.json` path) it falls back to the configured `TimeWindow` — unchanged. Billing is
  per metric requested, not per datapoint, so a wider picked window barely moves this adapter's cost.

## Dependencies
- `AWSSDK.CloudWatch` — `ListMetrics`/`GetMetricData`.
- Benzene `Abstractions` (DI), `Mesh.Contracts` (`IMeshUsageSource`, `MeshUsage`, `MeshUsageEntry`,
  `MeshUsageSource.CloudWatch`).

## Conventions
- Report only the dimensions the metric actually carries; never guess a `null` dimension to "all".
- Never throw for an empty/absent metric — that's "no traffic", a real and useful signal.
