# Real-Time Stream Processing

**Status: DRAFT v0.1 — part of the [Patterns](index.md) "at enterprise scale" set.**

Enterprises that run on **high-volume, time-ordered data** — market-data ticks, trade events,
device telemetry, clickstreams — need a processing model the request/response and per-message
patterns don't give them: an **ordered stream**, processed with **backpressure**, **windowed** and
**partitioned**, with **checkpointed** at-least-once progress. Benzene has a first-class streaming
binding for exactly this, distinct from its per-message transports. This document explains it and
works a **financial market-data pipeline** through it.

---

## Stream vs. fan-out — two different models

Benzene deliberately offers two shapes for a batch of inbound records, and choosing correctly is
the first design decision:

| | **Fan-out** (SQS) | **Stream** (Kinesis / Event Hubs) |
|---|---|---|
| A batch of N records is… | N pipeline invocations, one per record | **one** pipeline invocation over the whole batch |
| Order | order-agnostic, processed concurrently | **shard-ordered**, preserved |
| Failure | per-record: only failed records retried | resume the whole batch **from the first failed sequence number** |
| Use for | independent work items (jobs, commands) | ordered event streams where sequence and windowing matter |

The stream model is the one that keeps per-key order and lets you **window and aggregate** —
the things a fan-out throws away. Reach for it whenever the *order* of records, or a *rolling
computation* over them, is part of the problem.

*(informative, .NET)* Kinesis is `UseKinesisStream(...)`; Azure Event Hubs is the mirror
`UseEventHubStream(...)`; a Cosmos change-feed streaming binding exists too. The operators below are
transport-neutral — they work on the stream from any of them.

---

## How you build it with Benzene

### The stream handler

The streaming binding hands your pipeline the whole batch as a lazily-iterated
`IAsyncEnumerable<TRecord>` — you pull records, and the pull *is* the backpressure:

*(informative, .NET)*

```csharp
app.UseAwsLambda(events => events
    .UseKinesisStream(stream => stream
        .UseStream<KinesisEventRecord>(async (records, ct) =>
        {
            await foreach (var record in records.WithCancellation(ct))
            {
                var tick = Deserialize<Tick>(record.Kinesis.GetDataAsString());
                // … process in shard order …
            }
        })));
```

- The records arrive as `IAsyncEnumerable<KinesisEventRecord>`, **iterated lazily** — decode and
  process one at a time; nothing buffers the whole shard into memory unless you ask it to.
- **Cancellation rides on the context**, not the signature — the `ct` you get is the invocation's
  token (the pipeline signature carries none, so it stays identical across transports).
- Decode each record with `record.Kinesis.GetData()` / `GetDataAsString()` (records arrive base64;
  `PartitionKey`, `SequenceNumber`, and `ApproximateArrivalTimestamp` are on the record).

### Partition and window

Two transport-neutral operators turn a raw stream into aggregatable groups:

- **`Window(n)`** — fixed-size windows, **lazy and order-preserving** (the final window may be
  smaller). Use it for rolling fixed-count aggregation over an unbounded stream; it does not buffer
  the whole stream.
- **`PartitionBy(keySelector)`** — groups the batch into per-key sub-streams, **order preserved
  within each key**, keys yielded in first-seen order. Partition on the record's **partition key**
  to restore the per-shard grouping the poller flattened.

> **Accuracy caveat, by design:** `PartitionBy` **buffers the whole batch** to group it, so it is
> for **bounded batches** — which a single stream invocation is — not for holding an infinite stream
> in memory. Within one Lambda batch (one shard's chunk) it is exactly right; do not use it as if it
> streamed lazily. `Window` is the lazy one.

### Checkpointing and at-least-once

The stream binding tracks a **monotonic checkpoint** (it only ever advances). On success it
auto-checkpoints to the end of the batch; on failure the last checkpoint is what the response
reports, and — because Kinesis/DynamoDB streams are shard-ordered — AWS resumes the **whole batch
from the first failed sequence number**. That gives you **at-least-once, in order**: a record is
never skipped, and a mid-batch failure replays from the failure, not from zero. (Event Hubs' binding
is fan-in and ordered but does not itself wire a checkpointer/result — its progress is the
platform's.)

Because delivery is at-least-once, a record can be reprocessed — so aggregation must be **idempotent
or replayable** (upsert the bar, don't blindly `+=`). This is the same discipline the
[outbox](transactional-outbox.md) and [read models](cqrs-read-models.md) already require.

---

## Worked example: a market-data tick pipeline

**The system:** ingest a firehose of market-data **ticks** (symbol, price, size, timestamp), roll
them into **one-minute OHLC bars** (open/high/low/close/volume) per symbol, and publish each closed
bar for downstream valuation and charting.

**Ingress.** Ticks land on a Kinesis stream, **partitioned by symbol** (the producer sets the
partition key to the symbol), so all of one symbol's ticks share a shard and stay ordered. A Benzene
stream Lambda consumes it.

**Aggregate.** Within each invocation the handler partitions the batch by symbol and folds each
symbol's ticks into its current bar:

*(informative, .NET)*

```csharp
.UseStream<KinesisEventRecord>(async (records, ct) =>
{
    // Partition on the record's native partition key (the producer set it to the symbol), so no
    // decode is needed to group. PartitionBy buffers this batch and preserves per-symbol order.
    await foreach (var symbolGroup in records.PartitionBy(r => r.Kinesis.PartitionKey).WithCancellation(ct))
    {
        var symbol = symbolGroup.Key;
        var ticks = symbolGroup.Value.Select(r => Deserialize<Tick>(r.Kinesis.GetDataAsString()));

        foreach (var minute in ticks.GroupBy(t => t.Timestamp.TruncateToMinute()))
        {
            var bar = Ohlc.From(minute);              // open/high/low/close/volume for that minute
            await _bars.UpsertAsync(symbol, minute.Key, bar);   // idempotent: replay-safe
            if (bar.IsClosed)
                await _sender.SendAsync<BarClosed, Void>("bar:closed", new BarClosed(symbol, bar));
        }
    }
})
```

**Cross-invocation state.** Each Lambda invocation sees **one batch**, so a bar that spans several
invocations is accumulated in a **store** (the `UpsertAsync` above — a per-`(symbol, minute)` item),
not in memory. In-memory `Window`/`PartitionBy` handle ordering and grouping *within* a batch; the
*rolling* state across batches lives in the store. Be explicit about that boundary — it is the one
thing newcomers get wrong about serverless streaming.

**Egress.** A closed bar is published as a `bar:closed` event — from here it is ordinary
[choreography](choreography.md): a valuation service revalues positions on the new price, a charting
read model projects the bar into a [query view](cqrs-read-models.md), an alerting service watches for
thresholds. None of them know they are downstream of a Kinesis shard.

**Throughput.** A stream's parallelism is its **shard count** — more shards, more concurrent
invocations, order preserved *within* each shard's key. For the *self-hosted* streaming transports
(Kafka, RabbitMQ workers), throughput is instead bounded by `ConcurrentRequests` (default 5), which
drives a `BoundedConcurrentDispatcher` — N concurrent lanes, and with a per-partition key selector,
strict FIFO per partition while running many partitions at once. (The AWS Lambda stream path scales
by shards, not that dispatcher.)

---

## Streaming request/response (a different tool)

The binding above is for *ingesting* a stream. Benzene also lets a **handler itself** take or return
a stream — `IMessageHandler<IAsyncEnumerable<TRequest>, IAsyncEnumerable<TResponse>>` — realized
today by the **gRPC** transport, which supports all four modes (unary, server-, client-, and
bidirectional-streaming) over HTTP/2. Use that for a low-latency **streaming RPC** between services
(a live price feed a caller subscribes to); use the Kinesis/Event Hubs binding for **durable,
checkpointed ingestion** of a firehose. They are different tools for different jobs — don't reach for
a queue-shaped stream when you want an RPC, or vice versa.

---

## When to use it

- **Use it** when records are **time-ordered** and the computation is a **rolling aggregation**,
  windowed reduction, or anything where sequence matters — market data, telemetry, CDC, metering.
- **Don't** use the stream model for independent work items with no ordering relationship — that is
  a **fan-out** ([SQS](service-communication.md)), which parallelizes freely and retries per-record.
  Using a stream for order-agnostic work throws away parallelism for ordering you don't need.

---

## Checklist

Stream processing is well-formed when:

- [ ] Ordered, windowed, or rolling computations use the **stream** binding; independent work uses
      **fan-out**.
- [ ] The stream is **partitioned by the key whose order matters** (symbol, account, device).
- [ ] Records are **pulled lazily**; only bounded, within-batch grouping uses `PartitionBy`.
- [ ] Rolling state **across invocations** lives in a store, not in memory.
- [ ] Aggregation is **idempotent/replayable**, because delivery is at-least-once with
      resume-from-sequence.
- [ ] Throughput is sized by **shards** (AWS) or `ConcurrentRequests` (self-hosted workers).

See also: [map-reduce & high-volume compute](map-reduce.md) (when each record needs heavy
calculation), and [choreography](choreography.md) (what consumes the events a stream emits).
