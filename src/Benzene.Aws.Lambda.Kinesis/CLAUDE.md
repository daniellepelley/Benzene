# Benzene.Aws.Lambda.Kinesis

## What this package does
Inbound AWS Kinesis Data Streams adapter: delivers a Kinesis batch to a Benzene **streaming**
pipeline. The whole batch is exposed as one ordered `IAsyncEnumerable<KinesisEventRecord>` (fan-in) —
the AWS counterpart to `Benzene.Azure.Function.EventHub`'s streaming binding, and the flagship AWS
consumer of the streaming engine in `Benzene.Core.Middleware/Streaming`.

## Fan-in (streaming), not fan-out
Unlike SQS/SNS/S3 — which fan **out**, mapping each record to its own context and message handler
concurrently — Kinesis fans **in**: one `StreamContext<KinesisEventRecord>`, one pipeline run, one DI
scope for the batch. This is deliberate: Kinesis records are ordered per shard/partition-key and
opaque (no per-record message type to route on), so the stream is handed to the handler intact.
Handlers use the stream operators to window and re-order:

```csharp
app.UseKinesisStream(kinesis => kinesis
    .UseStream<KinesisEventRecord>(async (records, ct) =>
    {
        await foreach (var partition in records.PartitionBy(r => r.Kinesis.PartitionKey, ct))
        {
            // records within a partition key are in shard order
        }
    }));
```

## Key types
- `KinesisEvent` / `KinesisEventRecord` / `KinesisRecordData` — Benzene's own model of the Lambda
  Kinesis event (dependency-free, mirroring `Benzene.Aws.Lambda.EventBridge`). `KinesisRecordData`
  exposes `PartitionKey`, `SequenceNumber`, the base64 `Data`, and `GetData()` / `GetDataAsString()`
  decode helpers. Swap for `Amazon.Lambda.KinesisEvents` if you prefer the official POCOs.
- `KinesisStreamApplication : StreamMiddlewareApplication<KinesisEvent, KinesisEventRecord>` — maps the
  batch to one `StreamContext<KinesisEventRecord>`; tags the transport `"kinesis"`.
- `KinesisLambdaHandler : AwsLambdaMiddlewareRouter<KinesisEvent>` — claims invocations whose first
  record's source is `aws:kinesis`; otherwise defers. Fire-and-forget (no response).
- `UseKinesisStream(action)` / `AddKinesis()` / `KinesisRegistrations` — standard adapter wiring.

## When to use this package
- Consuming a Kinesis Data Stream in Lambda for stream processing: windowed aggregation, per-partition
  ordering, high-throughput ingestion/ETL, CDC fan-in.

## Dependencies on other Benzene packages
- **Benzene.Aws.Lambda.Core** — `AwsLambdaMiddlewareRouter`, `AwsEventStreamContext` (and, transitively,
  `Benzene.Core.Middleware` for `StreamContext` / `StreamMiddlewareApplication` / the stream operators).

## Important conventions
- Fire-and-forget: Kinesis targets are invoked asynchronously; the adapter writes no response.
- Transport name: `"kinesis"`.
- Checkpointing uses the streaming engine's `NullStreamCheckpointer` default — Lambda's own poller
  checkpoints on success. A real `IStreamCheckpointer<KinesisEventRecord>` keyed on
  `SequenceNumber` is future (streaming Phase 2, see `docs/plans/streaming-plan.md`).
