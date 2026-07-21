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
- `KinesisStreamApplication : StreamMiddlewareApplication<KinesisEvent, KinesisEventRecord, KinesisBatchResponse>`
  — maps the batch to one `StreamContext<KinesisEventRecord>` wired with a real
  `KinesisStreamCheckpointer`, runs it, and maps the result back to a `KinesisBatchResponse`; tags
  the transport `"kinesis"`. Catches a pipeline exception (logs it) instead of letting it cascade,
  so the response still carries whatever was checkpointed before the failure — see "Real
  checkpointing" below.
- `KinesisBatchResponse` — Benzene's own hand-rolled response type (mirrors
  `Amazon.Lambda.SQSEvents.SQSBatchResponse`'s wire shape: `batchItemFailures`/`itemIdentifier`,
  confirmed against the actual assembly) for the Kinesis event source mapping's
  `ReportBatchItemFailures` contract. Unlike SQS, only the *first* `BatchItemFailures` entry is
  meaningful to AWS for Kinesis/DynamoDB Streams — its constructor takes a single optional failed
  sequence number, not a list-builder.
- `KinesisStreamCheckpointer : IStreamCheckpointer<KinesisEventRecord>` (internal) — tracks the last
  record a stream handler checkpointed and computes the sequence number to resume from.
- `KinesisLambdaHandler : AwsLambdaMiddlewareRouter<KinesisEvent>` — claims invocations whose first
  record's source is `aws:kinesis`; otherwise defers. Writes back the `KinesisBatchResponse` —
  Kinesis event source mapping invocations are synchronous from Lambda's own perspective once
  `ReportBatchItemFailures` is configured on the trigger.
- `UseKinesisStream(action)` / `AddKinesis()` / `KinesisRegistrations` — standard adapter wiring, no
  signature change from the checkpointing work above.

## When to use this package
- Consuming a Kinesis Data Stream in Lambda for stream processing: windowed aggregation, per-partition
  ordering, high-throughput ingestion/ETL, CDC fan-in.

## Dependencies on other Benzene packages
- **Benzene.Aws.Lambda.Core** — `AwsLambdaMiddlewareRouter`, `AwsEventStreamContext` (and, transitively,
  `Benzene.Core.Middleware` for `StreamContext` / `StreamMiddlewareApplication` / the stream operators).

## Important conventions
- Transport name: `"kinesis"`.
- **Real checkpointing and per-record failure containment** (2026-07-17, closing the gap flagged
  below this line previously): the batch's `StreamContext<KinesisEventRecord>` is wired with a real
  `KinesisStreamCheckpointer`, not `NullStreamCheckpointer`. A handler calls
  `context.Checkpointer.CheckpointAsync(record)` after successfully processing (or windowing past)
  a record — the exact point a windowed/aggregating handler decides "everything up to here is
  safe". If the pipeline throws, `KinesisLambdaHandler` still returns a `KinesisBatchResponse`
  naming the sequence number *after* the last checkpointed record, so AWS resumes the batch from
  there on retry instead of redelivering the whole thing (Kinesis's shard-ordered
  `ReportBatchItemFailures` contract only reads the *first* reported failure — see
  `work/kinesis-batch-failure-handling-design.md` §2). **One real behavior change**: a handler that
  never calls `CheckpointAsync` at all now gets a response naming the *first* record's sequence
  number on any exception — i.e. AWS retries the entire batch from the start — instead of the
  previous silent no-op via `NullStreamCheckpointer`. This is purely additive/more-correct (no code
  changes needed to adopt it), but changes what AWS does on retry, so it's worth knowing about even
  if you never call `CheckpointAsync` yourself.
- **`AutoCheckpointOnSuccess` (default `true`, `KinesisStreamOptions`):** a batch whose pipeline
  completes without throwing and whose handler never checkpointed anything itself is checkpointed to
  the end, so a fully-processed batch advances its resume point instead of being redelivered by
  Kinesis forever. This closes the `UseStream((records, ct) => ...)` callback-overload gap: that
  overload exposes no checkpointer, so without this a successful batch reported record 0 as failed and
  AWS re-ran it indefinitely. Mirrors Cosmos's `BenzeneCosmosChangeFeedConfig.AutoCheckpointOnSuccess`.
  It only runs on the success path and only when the handler checkpointed nothing — a handler managing
  its own checkpoints is left untouched, and on a thrown exception the resume point stays at the
  handler's last explicit checkpoint. Set `AutoCheckpointOnSuccess = false`
  (`UseKinesisStream(action, new KinesisStreamOptions { AutoCheckpointOnSuccess = false })`) for full
  manual control. Covered by `KinesisStreamApplicationTest`.
- No automatic `UseCheckpointAfterEach()` operator exists yet (streaming Phase 2, see
  `docs/plans/streaming-plan.md`) — a handler that wants per-record (rather than whole-batch-on-success)
  checkpointing must still checkpoint explicitly at the right point in its own stream-processing logic.

## Tests
- `test/Benzene.Core.Test/Aws/Kinesis/UseKinesisStreamTest.cs` — full pipeline happy path, routing
  (including `CanHandle`'s null-`Records`/empty-`Records` guard branches, not just the
  wrong-`EventSource` case), and an end-to-end wire-response assertion: a handler that throws after
  checkpointing one record produces a real `KinesisBatchResponse` on `AwsEventStreamContext.Response`
  naming the correct resume sequence number (deserialized via `AwsEventStreamContextBuilder.StreamToObject<T>`) -
  `KinesisStreamApplicationTest` below only ever checks the in-memory return value, never the
  serialized wire response `KinesisLambdaHandler.MapResponse` actually writes.
- `test/Benzene.Core.Test/Aws/Kinesis/KinesisRecordDataTest.cs` — base64 decode helpers.
- `test/Benzene.Core.Test/Aws/Kinesis/KinesisBatchResponseTest.cs` — constructor shape (empty vs
  single-failure `BatchItemFailures`).
- `test/Benzene.Core.Test/Aws/Kinesis/KinesisStreamApplicationTest.cs` — checkpoint/resume behavior
  end to end through `KinesisStreamApplication.HandleAsync`: all records checkpointed → empty
  response; throwing after checkpointing record 2 of 5 → response names record 3; throwing before
  checkpointing anything → response names record 1; empty batch → empty response. Exercises
  `KinesisStreamCheckpointer` (internal, no direct unit test — this repo has no
  `InternalsVisibleTo` wiring) through this public surface instead.
