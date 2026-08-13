# Observability Conventions

**Status: PROPOSED v0.1 — the port audit in §5 is verified against each port's `main` as of
2026-08-13; the convention in §2–§4 awaits ratification before the ports realign to it.**

Benzene emits OpenTelemetry traces and metrics from the middleware pipeline. *That* it emits them
is a per-port implementation matter; **what they are called** is not — a metric name or attribute
key that differs between ports fractures every tool built on top of them.

This document is that shared vocabulary. It is a **cross-port convention, not a wire contract**:
nothing here crosses a process boundary as a Benzene message, no conformance fixture pins it, and
a service that emits none of it is still fully conformant — at Core level *and* under the
[Cloud Service Profile](../specification/cloud-service-profile.md), which deliberately requires no
metrics. It lives here rather than in the specification because the specification stays taut
(it defines what a conforming service must do on the wire) — but it lives *somewhere*, because
the alternative is what §5 documents: four ports, three vocabularies, and tooling that silently
works for some of them.

## 1. Why a shared vocabulary is load-bearing

Four shipped things read these names back out of a backend, and none of them has Benzene running
when it does:

- **The mesh usage feed** — how often each topic is exercised **and over which transports**, which
  is requirement 3 of the [Mesh UI](mesh-ui.md#1-what-the-mesh-ui-is-for) and the evidence behind
  every value-vs-deprecation decision. It is derived from the metrics, because the structural
  catalog can prove a topic is *wired* but only observed traffic proves it is *used*.
- **Metrics-store usage sources** — the CloudWatch and Application Insights adapters query a
  counter *by name*, with dimensions *by key*.
- **Trace-backed mesh read models** — the Tempo, Jaeger, and X-Ray trace sources read Benzene's
  span attributes *by key* to recover topic, version, and status from a stored span.
- **Dashboards and third-party integrations** — Grafana dashboard packs, Datadog facets, and any
  vendor view built on [exported telemetry](exporting-telemetry.md).

Each is written once and pointed at an estate. If the estate is polyglot and the names differ per
port, each tool works for some services and silently under-reports the rest — the failure mode is
a *missing row*, not an error, which is the worst kind.

## 2. Metric instruments

Two instruments, recorded once per handled message, on a meter/scope named `Benzene`:

| Instrument | Kind | Unit | Meaning |
|---|---|---|---|
| `benzene.messages.processed` | counter (integer) | — | one increment per handled message |
| `benzene.message.duration` | histogram (floating) | `ms` | handling duration in milliseconds |

Emission SHOULD be explicit opt-in (a pipeline call the host adds), and MUST be free when nothing
is listening. Export is whatever OTel wiring the host already has — Benzene never talks to a
backend itself ([exporting telemetry](exporting-telemetry.md)).

## 3. Metric attributes

Both instruments carry the same three attributes, and **no others** — cardinality is the whole
budget here:

| Attribute | Value |
|---|---|
| `topic` | the message's topic id; `<missing>` when unresolvable |
| `transport` | the transport the message arrived over; `<missing>` when unresolvable |
| `result` | the outcome, per the collapse rule below; `<missing>` when no result was recorded |

Attribute keys are **unprefixed** here, unlike the span attributes of §4. This is deliberate and
is the one place the two vocabularies differ on purpose: metrics are already scoped by the meter
name (`Benzene`), so a `benzene.` prefix on the attribute key is redundant, whereas span
attributes land in a single flat per-span namespace shared with every other instrumentation
library and must self-namespace.

### The `result` collapse rule

`result` is **not** the raw Benzene status. It collapses successes and itemizes failures:

| Outcome | `result` value |
|---|---|
| Any successful result (the success *boolean*, not the status class) | `success` |
| An unsuccessful result | its Benzene **status verbatim** — `not-found`, `unauthorized`, `validation-error`, … |
| The pipeline threw | `exception` |
| No result signal recorded | `<missing>` |

Two consequences worth stating, because both have bitten:

- Success is decided by the result's success flag, **not** by its status class. A health check
  that reports `service-unavailable` while remaining a successful result (so the body renders) is
  `success`.
- `exception` is distinct from a handler that *returned* `unexpected-error`. A thrown-and-mapped
  failure and a returned failure are different operational events.

This keeps success cardinality at exactly 1 — you want the total, not `ok` versus `created` —
while leaving failures diagnosable, since a mostly-`not-found` failure mix reads very differently
from a mostly-`unauthorized` one. The failure vocabulary is the bounded status set of
[wire-contracts.md §3](../specification/wire-contracts.md), so total cardinality stays a small
constant. Cost-shaping, if an estate ever needs it, belongs backend-side as a metric filter —
never in the emitted vocabulary.

## 4. Span attributes

The pipeline stamps these on the **topic-bearing span** — the one span per dispatch that knows
which topic it is. Span attribute keys **are** `benzene.`-prefixed, per §3's rationale:

| Attribute | Value | Required |
|---|---|---|
| `benzene.topic` | topic id | yes — its presence is what marks the span as topic-bearing |
| `benzene.version` | topic version, when the topic is versioned | yes, when versioned |
| `benzene.status` | the Benzene status verbatim, or `exception` when the pipeline threw | yes |
| `benzene.transport` | the transport the message arrived over | recommended |
| `benzene.service` | the logical service name, matching the mesh descriptor's `service` | recommended |
| `benzene.correlation-id` | the correlation id, when one is present | optional |
| `benzene.exception.type` | language-native exception type name, on a thrown failure — never the message, stack, or payload (the [mesh.md §3](../specification/mesh.md) rule) | optional |
| `benzene.handler` | the handler identifier | optional |

`benzene.version` is the topic's version — **not** `benzene.topic.version`. The distinction is not
cosmetic: the shipped Tempo, Jaeger, and X-Ray trace sources read `benzene.version` by that exact
key, so a span carrying the other spelling loses its version on the way into a mesh view (§5).

Note also that `benzene.status` on a span carries the raw status, while `result` on a metric
carries the collapsed vocabulary of §3. Spans are read one at a time for diagnosis, where raw
detail is what you want; metrics are aggregated, where cardinality is the constraint.

## 5. Port conformance today *(informative — verified 2026-08-13)*

| | .NET | TypeScript | Go | Python |
|---|---|---|---|---|
| Counter | `benzene.messages.processed` | `benzene.messages.processed` | ⚠ `benzene.invocations` | ✗ none |
| Histogram | `benzene.message.duration` | `benzene.message.duration` | ⚠ `benzene.invocation.duration` | ✗ none |
| Metric attributes | `topic`, `transport`, `result` | `topic`, `transport`, `result` | ⚠ `benzene.topic`, `benzene.topic.version`, `benzene.status` | ✗ none |
| Span topic/status | `benzene.topic`, `benzene.status` | `benzene.topic`, `benzene.status` | `benzene.topic`, `benzene.status` | ✗ none |
| Span version key | `benzene.version` | `benzene.version` | ⚠ `benzene.topic.version` | ✗ none |
| Span transport | `benzene.transport` | `benzene.transport` | ⚠ absent | ✗ none |
| Emission | opt-in | opt-in | always-on in the diagnostics middleware | — |

Three of these are live defects rather than cosmetic drift:

1. **A Go service's topic version does not reach a .NET-hosted mesh.** The Tempo/Jaeger/X-Ray
   trace sources read `benzene.version`; Go stamps `benzene.topic.version`. Cross-language fleets
   are a shipped, exercised scenario ([mesh.md](../specification/mesh.md) preamble), so this is
   reachable today, and it fails silently as a blank version column.
2. **Go services cannot answer "over which transports".** Neither the metrics nor the spans carry
   a transport attribute, so Mesh UI requirement 3 is structurally unanswerable for them.
3. **Go's metric `result` dimension does not exist**; its counter is attributed by raw status
   instead, so a Go service and a .NET service cannot be summed into one usage series even after
   the instrument names are reconciled.

## 6. Alignment actions

The convention above adopts the .NET/TypeScript vocabulary wholesale, for one reason: it has the
install base. Two of four ports already emit it, both shipped metrics-store usage adapters query
it by default, and live dashboards and stored metric series already carry those names. Go's
`invocations` spelling is arguably the better English — [mesh.md §3](../specification/mesh.md)
does say "one TraceEvent per routed invocation" — but renaming three consumers to improve a noun
is a bad trade against realigning one emitter.

| Port | Action |
|---|---|
| .NET | None. Reference this page from `Benzene.Diagnostics` and retire the .NET-local copy of the standard in favour of it. |
| TypeScript | None. Same reference change. |
| Go | Rename both instruments; replace the metric attribute set with `topic`/`transport`/`result` and implement the §3 collapse rule; rename the span attribute `benzene.topic.version` → `benzene.version`; add `benzene.transport`. Emitting the old instrument names alongside the new for one release is a reasonable migration, but the span attribute rename should be immediate — it is fixing a silent data loss, not a preference. |
| Python | Implement to this page when it adds a diagnostics module; there is no legacy to carry. |

## 7. Stability

Pre-1.0, and additive-only once ratified: new attributes and new instruments may be added; an
existing instrument name, attribute key, or `result` token changes only with a migration note here
and in each port's release notes. Consumers MUST tolerate an unknown `result` token (treat it as a
failure class they don't recognise) and MUST tolerate a missing optional attribute — a backend's
rolling window will legitimately hold both vocabularies during any migration.

## See also

- [Exporting Telemetry](exporting-telemetry.md) — getting these signals into Datadog, Grafana, or
  any other backend
- [Mesh UI](mesh-ui.md) — the usage and transport views these metrics feed
- [Mesh Contracts](../specification/mesh.md) — the trace feed, whose `TraceEvent` is the
  spec-native (metrics-free) usage signal
