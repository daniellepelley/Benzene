# Exporting Telemetry

Benzene services are **OpenTelemetry-native and exporter-agnostic**. The pipeline emits spans and
metrics ([observability conventions](observability-conventions.md)); it never talks to a vendor
backend itself. Getting those signals into Datadog, Grafana, Honeycomb, New Relic, Dynatrace,
Azure Monitor, or anything else is therefore **configuration of the OTel wiring your host already
has**, not a Benzene integration you have to wait for.

This is a deliberate position, and it is the answer to "is there a Benzene plugin for *X*?" for
essentially every *X*:

> **OTLP is the integration.** Benzene ships no vendor-specific exporters and intends to ship
> none. A vendor that ingests OTLP already supports Benzene, completely, today.

What that buys: every backend is supported on the day it supports OTLP, none of them can rot in a
Benzene release cycle, and swapping vendors is an endpoint change rather than a code change. What
it costs: Benzene has no opinion about your backend's proprietary features, which is the right
trade for a framework.

## What gets exported

| Signal | What it carries | Defined by |
|---|---|---|
| **Traces** | one topic-bearing span per dispatch, attributed with topic, version, status, transport, service, and correlation id | [observability conventions §4](observability-conventions.md#4-span-attributes) |
| **Metrics** | a processed-message counter and a duration histogram, attributed by topic, transport, and result | [observability conventions §2–§3](observability-conventions.md#2-metric-instruments) |
| **Logs** | whatever your host's logging stack does; Benzene writes through the platform logger rather than owning one | per-port |

Trace context is W3C-standard and joined from inbound headers
([cloud-service-profile.md](../specification/cloud-service-profile.md) R8), so spans stitch
together across services — including across languages, and including across a hop that leaves
Benzene entirely and comes back.

Note the relationship to the [mesh](../specification/mesh.md): the mesh's own trace feed is
**independent** of this export path. A fleet with no observability vendor at all still gets
topology, health, and catalog from the mesh feeds; a fleet with a vendor gets both, and the mesh
can additionally *read back* from that vendor through a trace or usage source. Neither substitutes
for the other.

## Datadog — the worked example

Datadog ingests OTLP by several supported routes. All of them work with an unmodified Benzene
service; pick by what your platform already runs.

| Route | Use when | How |
|---|---|---|
| **OTLP ingest in the Datadog Agent** | you already run the Datadog Agent | enable the Agent's [OTLP receiver](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest_in_the_agent/) and point the service's OTLP exporter at it |
| **Datadog Distribution of the OTel Collector (DDOT)** | you want Collector processing plus Agent features | deploy [DDOT](https://docs.datadoghq.com/opentelemetry/setup/agent/) |
| **Upstream OTel Collector + Datadog exporter** | you already run a vendor-neutral Collector | add the Datadog exporter to your existing pipeline |
| **Direct OTLP intake** | no Agent or Collector is feasible | export straight to Datadog's [OTLP intake endpoint](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/) |

The service-side configuration is the same in every case — an OTLP endpoint and headers, supplied
the standard OTel way (typically `OTEL_EXPORTER_OTLP_ENDPOINT` and friends, or your port's
equivalent wiring). Nothing in the service knows it is talking to Datadog.

**What to do once data lands.** The value is in Benzene's semantic attributes, so facet on them:

- group traces by `benzene.topic` to get per-topic latency and error rate — the unit of work that
  actually matters, rather than an HTTP route that may serve dozens of topics;
- facet by `benzene.status` to separate business failures (`not-found`, `validation-error`) from
  infrastructure failures (`service-unavailable`, `timeout`) — a distinction transport status
  codes flatten away;
- facet by `benzene.transport` to see the same topic's behaviour over HTTP versus a queue;
- split the `benzene.messages.processed` counter by `result` for a per-topic success/failure mix,
  and by `transport` for the "is this topic actually used, and how?" question.

Because Benzene spans are `SpanKind.Server` and joined to inbound trace context, Datadog's service
map and dependency views populate with no extra work.

## Every other backend

The same recipe, with the endpoint swapped:

| Backend | Route |
|---|---|
| **Grafana stack** (Tempo/Mimir/Loki) | OTLP to the Grafana Agent, Alloy, or an OTel Collector |
| **Prometheus** | a Prometheus exporter or the Collector's Prometheus remote-write |
| **Honeycomb, New Relic, Dynatrace, Grafana Cloud** | OTLP direct or via Collector |
| **Azure Monitor / Application Insights** | the Azure Monitor exporter, or OTLP via Collector |
| **AWS X-Ray** | the ADOT Collector |
| **Jaeger** | OTLP direct |

Several of these double as **mesh data sources** — the mesh can read Tempo, Jaeger, X-Ray,
CloudWatch, or Application Insights back to build fleet topology and usage. Exporting telemetry
and powering the mesh from that telemetry are two uses of the same export.

## Where the language-specific parts live

Which package to reference, and the exact call that registers Benzene's instrumentation with your
OTel provider, is idiomatic per language and documented in each port's repo:

- **.NET** — [benzene-dotnet](https://github.com/daniellepelley/benzene-dotnet)
- **Go** — [benzene-go](https://github.com/daniellepelley/benzene-go)
- **TypeScript** — [benzene-typescript](https://github.com/daniellepelley/benzene-typescript)
- **Python** — [benzene-python](https://github.com/daniellepelley/benzene-python)

## See also

- [Observability Conventions](observability-conventions.md) — the metric and span vocabulary this
  page exports, and what each attribute means
- [Mesh Contracts](../specification/mesh.md) — the vendor-independent trace feed and the fleet
  view built from it
- [Cloud Service Profile](../specification/cloud-service-profile.md) — R8, trace-context
  propagation, which is what makes cross-service traces stitch
