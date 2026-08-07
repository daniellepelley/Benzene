# Reference Solution: A Real-Time Risk & Trading Platform

**Status: DRAFT v0.1 — a worked reference solution in the [Patterns](index.md) "at enterprise scale"
set.**

The individual patterns show one technique each. This document assembles them into **one named
system of the kind a large financial enterprise actually asks to have designed** — a platform that
ingests live market data, revalues a large trading book in real time and in batch, serves fast
cross-cutting queries, keeps an immutable audit trail, and offers low-latency pricing to other
desks. It is a worked example, not a product: every box is a Benzene service, and every arrow is one
of the patterns.

The point is to show how the pieces **compose** — that these are not competing patterns but layers
of one system.

---

## The system

```
  MARKET DATA ─►┌───────────────────┐  bar:closed  ┌───────────────────┐
  (Kinesis)     │  Market-Data      │─────────────►│  Valuation        │  position:revalued
   ticks, by    │  Aggregator       │   (event)    │  Service          │─────────────┐
   symbol/shard │  [stream]         │              │  [choreography]   │             │
               └───────────────────┘              └───────────────────┘             ▼
                                                            ▲              ┌───────────────────┐
  desks ──gRPC──►┌───────────────────┐                      │              │  Risk Read Models │
  (low-latency)  │  Pricing Service  │                      │              │  [CQRS]           │
                 │  [gRPC streaming] │                      │              │  positions, P&L,  │
                 └───────────────────┘                      │              │  exposure, VaR    │
                                                            │              └───────────────────┘
  schedule ─────►┌───────────────────┐  risk:shard × N      │                       ▲
  (end of day)   │  Risk Coordinator │──────────────────────┘                       │ risk:completed
                 │  [map-reduce]     │◄── partial risk vectors                       │
                 └───────────────────┘                                              │
                          │  every trade/cash/fee command                            │
                          ▼                                                          │
                 ┌───────────────────┐   ledger:INSERT (CDC)                         │
                 │  Trade Ledger     │─────────────────────────────────────────────►┘
                 │  [event sourcing] │   immutable, ordered, audited event log
                 └───────────────────┘
```

Six services, each a plain Benzene service targeting the
[Cloud Service Profile](../specification/cloud-service-profile.md), each owning its own data
(share-nothing), each addressed by [topic](service-communication.md). The whole thing is the
[two-tier architecture](two-tier-architecture.md) — core services (ledger, valuation), orchestrators
(risk coordinator), read models — with its **event-driven half** doing the heavy lifting.

---

## The flow, pattern by pattern

### 1. Ingest market data — [stream processing](streaming-processing.md)

The **Market-Data Aggregator** consumes a Kinesis firehose of ticks, **partitioned by symbol** so
each symbol's ticks stay shard-ordered. It rolls them into one-minute OHLC bars (idempotent upserts,
rolling state in a store because each invocation is one batch) and emits `bar:closed` per closed bar.
Throughput scales with shard count; order is preserved per symbol; checkpointing gives at-least-once,
in-order progress.

*Why Benzene:* the `UseKinesisStream` binding gives ordered fan-in, `PartitionBy`/`Window` do the
per-symbol grouping, and the checkpoint contract handles resume-from-sequence — no bespoke stream
plumbing.

### 2. Revalue in real time — [choreography](choreography.md)

The **Valuation Service** subscribes to `bar:closed` and revalues every position exposed to that
symbol, emitting `position:revalued`. It knows nothing about who produced the bar or who consumes the
revaluation — it just reacts. New reactions (a limit-monitor, a hedging trigger) are added as new
subscribers without touching valuation.

*Why Benzene:* an event is a topic; a reaction is a handler; the [mesh](../specification/mesh.md)
draws the live "what reacts to what" graph from trace parentage, so this decoupled flow is still
fully observable.

### 3. Serve cross-cutting queries — [CQRS & read models](cqrs-read-models.md)

**Risk Read Models** project `position:revalued`, `risk:completed`, and ledger events into
denormalized views built for specific questions — current positions per desk, live P&L, exposure by
counterparty, VaR by book. These are queries **no single core service can answer** (they span the
ledger, valuations, and reference data), served as single fast reads instead of runtime fan-outs.

*Why Benzene:* the read model is a normal service — event handlers project, query handlers serve —
and it stays share-nothing, owning its query store. Read the read model for cross-aggregate views;
read the core service directly when you need read-your-writes.

### 4. End-of-day risk — [map-reduce](map-reduce.md)

The **Risk Coordinator** fires on a schedule, partitions the book into shards, and scatters
`risk:shard` across a burst of hundreds of stateless Lambda workers (`BoundedFanOut` cap), each
revaluing its slice against the day's curves and returning a partial risk vector. The coordinator
folds the partials into the firm-level number and emits `risk:completed`. A job that is hours serial
is minutes parallel.

*Why Benzene:* the scatter is `Task.WhenAll`/`BoundedFanOut` over `SendAsync` resolving to
Lambda-to-Lambda invokes (burst-cheap); the reduce is a deterministic fold (results in shard order);
partial-failure policy is explicit so a regulatory number is never silently under-covered.

### 5. The book of record — [event sourcing](event-sourcing.md)

The **Trade Ledger** is the system of record. Every trade, cash movement, and fee is a command
handler that appends an **immutable, ordered event** to a DynamoDB log (conditional write for
optimistic concurrency). The log's stream feeds the projections (step 3) and *is* the audit trail:
point-in-time reconstruction is a replay of the pure fold; years of schema drift are absorbed by
upcasting historical events on read (`AddPayloadVersioning`).

*Why Benzene:* command handlers ingest, DynamoDB-Streams CDC projects in order at-least-once, payload
versioning evolves the events, idempotency makes replay converge — the ledger is composed from
built-ins with the append and the fold as the only app-owned pieces.

### 6. Low-latency internal pricing — [gRPC](service-communication.md)

The **Pricing Service** offers other desks a low-latency, **streaming** price/greeks feed over gRPC
(HTTP/2, protobuf, bidirectional streaming), with deadlines propagated end to end. Internal callers
that need microsecond-sensitive request/response or a live subscription use this rather than the
event bus.

*Why Benzene:* the same `IMessageHandler` shape serves gRPC (including all four streaming modes); the
service is still topic-routed and result-status-mapped, so it is a Benzene service that merely speaks
a faster wire to its neighbours.

---

## What makes this hold together

- **One model, many transports.** Kinesis, SNS/EventBridge, Lambda-to-Lambda, DynamoDB Streams, gRPC,
  a schedule — every service is the same pipeline-and-handlers core behind a different adapter. The
  architecture is transport choices, not framework rewrites.
- **Reliability is layered, not bolted on.** The [outbox](transactional-outbox.md)/CDC guarantees no
  event is lost; idempotent consumers make at-least-once safe; the saga (inside the coordinator and
  any atomic booking flow) keeps multi-service writes all-or-nothing. Nothing lost, nothing
  double-applied — the contract a trading platform lives or dies by.
- **The whole estate is visible.** Every service targets the Cloud Service Profile, so the mesh gives
  live topology, health, schemas, and — from trace parentage — the real event-flow graph across all
  six services, derived from traffic rather than a diagram someone has to keep current.
- **It degrades sensibly.** A slow read model lags but never blocks a trade; a failed risk shard is
  retried or flags reduced coverage; a dropped market-data batch resumes from its checkpoint. No
  single failure takes the platform down, because the couplings are events and bounded calls, not
  shared state.

---

## Building it, in order

A team (or an agent) can stand this up incrementally — each step is independently useful:

1. **Trade Ledger** first — the book of record ([event sourcing](event-sourcing.md)); everything
   else derives from its events.
2. **Risk Read Models** — project the ledger into query views ([CQRS](cqrs-read-models.md)); now the
   business can *see* the book.
3. **Market-Data Aggregator + Valuation** — bring in live prices ([stream](streaming-processing.md) +
   [choreography](choreography.md)); positions revalue in real time.
4. **Risk Coordinator** — the end-of-day number ([map-reduce](map-reduce.md)).
5. **Pricing Service** — low-latency internal feed (gRPC) once neighbours need it.

Each rung is a shippable system on its own — the [adoption ladder](../specification/design-principles.md#2-the-adoption-ladder)
applied to a whole platform, not just one service.

---

See also — the patterns this solution composes: [two-tier architecture](two-tier-architecture.md),
[stream processing](streaming-processing.md), [map-reduce](map-reduce.md),
[event sourcing](event-sourcing.md), [CQRS & read models](cqrs-read-models.md),
[choreography](choreography.md), [transactional outbox](transactional-outbox.md),
[service communication](service-communication.md).
