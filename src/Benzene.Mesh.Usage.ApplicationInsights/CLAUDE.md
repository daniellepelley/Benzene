# Benzene.Mesh.Usage.ApplicationInsights

## What this package does
The **Application Insights / Azure Monitor usage adapter** for the mesh: an `IMeshUsageSource`
(`Benzene.Mesh.Contracts`) that reads the `benzene.messages.processed` counter back from an Application
Insights **Log Analytics workspace** and reports it as the mesh usage feed — a count per
**(topic, transport, status)** over a configured window. The aggregator merges it into `usage.json`, and
the Mesh UI renders per-topic request counts. The **Azure sibling of `Benzene.Mesh.Usage.CloudWatch`**.
Pins `Azure.Monitor.Query` + `Azure.Identity`.

The emit half is a service's OpenTelemetry pipeline: `UseBenzeneMetrics()` (`Benzene.Diagnostics`) emits
`benzene.messages.processed` tagged `topic`/`transport`/`result`, and the **Azure Monitor OpenTelemetry
exporter** (`Azure.Monitor.OpenTelemetry.Exporter`, wired in the examples) sends it to Application
Insights, where it lands in the Log Analytics `customMetrics` table (dimensions in `customDimensions`).
This package is the aggregator-side reader that turns that back into the `MeshUsage`/`MeshUsageEntry`
shape. See `docs/mesh-usage-feed.md`.

## Key types
- `ApplicationInsightsUsageSource : IMeshUsageSource` — `FetchUsageAsync` runs a grouped count query via
  the seam below and maps each row to a `MeshUsageEntry(topic, transport, status=result, count)`.
  `version`/`service`/`avgDurationMs` are left `null` (the counter doesn't carry them) — the
  missing-dimension degradation path. An empty result → empty `Entries` ("wired, no traffic"), never
  `null`; a genuine query failure propagates (the aggregator bounds and skips a throwing source).
- `IApplicationInsightsUsageQuery` + `UsageCount` — the query seam. `LogsQueryUsageQuery` (default) issues
  KQL against the workspace: `customMetrics | where name == … | extend …customDimensions[…] | summarize
  sum(valueSum) by topic, transport, result`. The seam exists because Azure's `LogsQueryClient` is a
  concrete class, not an interface — this is the mockable equivalent of the CloudWatch adapter's
  `IAmazonCloudWatch`, and it keeps the source's mapping/window/degradation logic unit-testable.
- `ApplicationInsightsUsageOptions` — `WorkspaceId` (the Log Analytics **workspace GUID**, not the
  instrumentation key), `MetricName` (`"benzene.messages.processed"`), `TimeWindow` (24h), and the three
  `customDimensions` keys (`topic`/`transport`/`result`).
- `Extensions.AddApplicationInsightsUsage(this IBenzeneServiceContainer, ApplicationInsightsUsageOptions)`
  — registers the options, a default `LogsQueryClient` (authenticated with `DefaultAzureCredential`, i.e.
  the ambient managed identity on Azure), the query seam, and the source as `IMeshUsageSource`. Requires
  `AddMeshAggregator(...)` in the same container. The querying identity needs the **Log Analytics Reader**
  role on the workspace (the examples' Terraform grants it).

## Critical assumption: delta temporality
A `sum(valueSum)` over the window equals the request count **only if** the counter was exported with
**delta** temporality — which the Azure Monitor OpenTelemetry exporter uses by default. A cumulative
export would over-count. (This is the Azure analogue of the CloudWatch adapter's EMF-delta requirement,
but here it's the exporter default, so nothing extra is needed on the emit side.)

## When to use this package
- On an Azure-hosted Benzene mesh (Web Apps / Functions) whose services export
  `benzene.messages.processed` to Application Insights, to light up the Mesh UI's usage surfaces.
  Wired end-to-end in `examples/AzureMesh` and `examples/AzureFunctionsMesh`.
- Coarse-grained by design (request counts over a window) — fine-grained analysis belongs in Application
  Insights / Grafana, not here.

## Deliberate boundaries (NOT shipped)
- **No per-service dimension** (`Service` is `null`) — the counter isn't tagged by service; promoting the
  OTel `service.name` resource attribute to a dimension is a documented follow-up.
- **No duration** (`AvgDurationMs` is `null`) — the `benzene.message.duration` histogram could fill it
  with a second query; a follow-up, kept out of v1 to stay focused on counts.
- **Single window** per aggregator run — UI-side window switching is a separate change.

## Dependencies
- `Azure.Monitor.Query` (`LogsQueryClient`) + `Azure.Identity` (`DefaultAzureCredential`).
- Benzene `Abstractions` (DI), `Mesh.Contracts` (`IMeshUsageSource`, `MeshUsage`, `MeshUsageEntry`,
  `MeshUsageSource.ApplicationInsights`).

## Conventions
- Report only the dimensions the metric actually carries; never guess a `null` dimension to "all".
- Never throw for an empty/absent metric — that's "no traffic", a real and useful signal.
