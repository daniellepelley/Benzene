# Benzene OpenTelemetry Example

Demonstrates Benzene's OpenTelemetry-based tracing and metrics end-to-end:
send a message into a real Benzene pipeline from a simple web UI, then view
the resulting trace in Grafana.

## What this shows

- `Benzene.Diagnostics.AddDiagnostics()` wraps every middleware in the pipeline
  with an `Activity` span from the `"Benzene"` `ActivitySource` (tagged
  `benzene.transport`, `benzene.topic`, `benzene.version`, `benzene.handler`).
- `Benzene.OpenTelemetry.AddBenzeneInstrumentation()` wires that `ActivitySource`
  (and its companion `Meter`) into an OpenTelemetry `TracerProviderBuilder` /
  `MeterProviderBuilder`, so any OTel exporter picks it up.
- `UseBenzeneMetrics()` emits `benzene.messages.processed` (counter) and
  `benzene.message.duration` (histogram), tagged by topic/transport/result.
- `UseW3CTraceContext()` lets an inbound `traceparent` header continue a
  distributed trace instead of starting a disconnected one.

See [`docs/monitoring.md`](../../docs/monitoring.md) for the full write-up of
Benzene's tracing model.

## Run it

```bash
cd examples/OpenTelemetry
./run.sh
```

`run.sh` just runs `docker compose up -d` followed by
`dotnet run --project Benzene.Examples.OpenTelemetry` — equivalent to running
those two commands yourself:

```bash
cd examples/OpenTelemetry
docker compose up -d
dotnet run --project Benzene.Examples.OpenTelemetry
```

- Web UI: http://localhost:5000
- Grafana: http://localhost:3000 (no login required)

`docker compose up -d` starts a single container,
[`grafana/otel-lgtm`](https://github.com/grafana/docker-otel-lgtm), which
bundles an OTLP collector, Tempo (traces), Prometheus (metrics), Loki, and
Grafana. The app exports to it over OTLP gRPC on `localhost:4317`.

## Try it

The UI lists the available topics from a dropdown (populated from
`GET /api/topics`) and lets you edit the JSON body and JSON headers before
sending:

- **`greeting`** — trivial request/response, a single span.
- **`order_create`** — deeper trace: a `Payment.Charge` span followed by
  `Warehouse.ReserveStock` and `Warehouse.Dispatch` child spans from an
  injected service.
- **`order_fail`** — throws inside the handler. Benzene's framework catches
  it and returns `ServiceUnavailable` (the app keeps running); the
  `Payment.Charge` span is marked as an error span, useful for seeing what a
  failed trace looks like.

Each response includes a `traceId`. Copy it, open Grafana
(http://localhost:3000) → **Explore** → select the **Tempo** data source, and
either paste the trace ID directly or run a TraceQL query:

```
{resource.service.name="benzene-otel-example"}
```

## What to look for

- Root span `Send <topic>` (from the example app's own `ActivitySource`) →
  `W3CTraceContext.Root` → one span per Benzene middleware
  (`BenzeneEnrichment`, `BenzeneMetrics`, `MessageRouter`, ...), each tagged
  with `benzene.transport`, `benzene.topic`, `benzene.handler`.
- For `order_create`, the nested `Payment.Charge` / `Warehouse.*` spans under
  the handler span.
- Metrics: in Grafana Explore, switch to the Prometheus data source and query
  `benzene_messages_processed_total` or `benzene_message_duration_bucket`.

## Distributed-trace demo

Add a `traceparent` header to the headers JSON before sending, e.g.:

```json
{ "traceparent": "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01" }
```

`UseW3CTraceContext()` re-parents the Benzene spans under that remote trace
context instead of starting a new one — search Tempo for
`0123456789abcdef0123456789abcdef` instead of the `traceId` returned by the
API (the two will differ, since the root `Send <topic>` span from the
example app is a separate trace from the one the Benzene pipeline re-parents
into).

## Notes

- `SetSampler(new AlwaysOnSampler())` is set explicitly in `Program.cs`.
  Without it, no spans are sampled or exported under this OpenTelemetry SDK /
  .NET version combination — keep it if you copy this wiring elsewhere.
- `UseBenzeneMetrics()` tags `result` as `<missing>` for this transport
  (`BenzeneMessageContext` doesn't implement `IHasMessageResult`) — expected,
  not a bug.
