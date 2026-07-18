# Sampling Strategies

Benzene does not implement its own sampling logic. `AddBenzeneInstrumentation()`
(`Benzene.OpenTelemetry`) registers Benzene's `ActivitySource`/`Meter` with the standard
[OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet) — sampling is
entirely the SDK's `Sampler` configuration on `TracerProviderBuilder`, applied the same way it would
be for any other instrumented library. This page is a guide to that standard configuration in a
Benzene context, not a Benzene-specific feature.

## Why you need to set a sampler explicitly

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetSampler(new AlwaysOnSampler())
    .AddBenzeneInstrumentation()
    .AddOtlpExporter()
    .Build();
```

Under some OpenTelemetry SDK/host combinations, omitting `SetSampler(...)` results in no spans being
recorded at all — `Activity.StartActivity` silently returns `null` for every middleware span Benzene
creates, and nothing appears in your tracing backend, with no error or warning anywhere. This isn't a
Benzene bug: it's the OTel SDK's own sampling behavior when no explicit sampler is configured on some
combinations. Always set a sampler explicitly rather than relying on the SDK default — see
`examples/OpenTelemetry/Benzene.Examples.OpenTelemetry/Program.cs` for a complete working example.

## Choosing a sampler

### Development / debugging: `AlwaysOnSampler`

Records every span. Fine for local development and the OTel example project in this repo, but not
recommended for production — it means 100% of trace data is exported, which is expensive at real
traffic volumes and adds (small but nonzero) per-request overhead for every downstream exporter call.

```csharp
.SetSampler(new AlwaysOnSampler())
```

### Production: `TraceIdRatioBasedSampler`

Samples a fixed percentage of traces, decided deterministically from the trace ID (so a trace is
either fully sampled or fully dropped — you never get a partial trace with some spans missing).

```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1)) // sample ~10% of traces
```

Start conservative (1-10%) for high-traffic services and increase if trace volume/cost allows. There
is no Benzene-specific guidance on the right ratio — it depends entirely on your traffic volume and
what your tracing backend charges for ingestion.

### Respecting upstream sampling decisions: `ParentBasedSampler`

If a request already carries a `traceparent` header with a sampling decision (see
[W3C Trace Context](monitoring.md#w3c-trace-context) — Benzene propagates `traceparent`/`tracestate`
automatically via `UseW3CTraceContext()`), you generally want to honor that decision rather than
re-sample independently, so a trace stays complete end-to-end across services instead of having
gaps where an internal sampler disagreed with an upstream one:

```csharp
.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)))
```

This is usually the right choice for any service that isn't the first hop in a distributed trace
(i.e. most services behind a gateway or another Benzene service).

### `AlwaysOffSampler`

Disables tracing entirely while keeping the rest of the OTel pipeline (metrics, in particular) wired
up unchanged. Rarely what you want for troubleshooting, but useful if you need to turn tracing off
quickly (e.g. an incident where the exporter itself is the problem) without touching metrics.

## Sampling does not affect metrics

`UseBenzeneMetrics()`/`AddBenzeneInstrumentation()` on a `MeterProviderBuilder` records the
`benzene.messages.processed`/`benzene.message.duration` metrics unconditionally — sampling only
applies to the tracing pipeline (`Activity`/spans). Turning tracing sampling down to reduce cost does
not lose any metrics data.

## What Benzene does not provide

- **No per-transport or per-topic sampling overrides.** The sampler you configure applies uniformly
  to every `Activity` Benzene creates, across every transport and every message topic. If you need
  different sampling rates for different topics, that's standard OTel `Sampler` customization
  (implement `Sampler` yourself and inspect `SamplingParameters.Name`/tags) — nothing Benzene-specific
  is needed or provided to help with this.
- **No head-vs-tail sampling guidance beyond what's above.** Tail-based sampling (deciding whether to
  keep a trace after seeing all its spans, typically done in a collector like the OpenTelemetry
  Collector's `tailsamplingprocessor`) is a backend/collector-level concern, not something Benzene or
  its OTel integration participates in directly.

## See also

- [Monitoring & Diagnostics](monitoring.md) — the broader tracing/metrics/logging picture
- [OpenTelemetry sampler documentation](https://opentelemetry.io/docs/languages/net/sampling/) — the
  authoritative reference for `Sampler` implementations and configuration, since this is standard
  OTel behavior, not something Benzene wraps or changes
