## AWS Lambda event sources

Review of `src/Benzene.Aws.Lambda.{Core,Kinesis,DynamoDb,S3,EventBridge,Kafka}` against AWS Lambda event-source-mapping docs (partial-batch-response, Kafka ordering/error-handling).

---

### Kinesis
Best-designed adapter. Fans **in** (one `StreamContext` per batch, preserving shard order), hand-rolls a `KinesisBatchResponse` matching the `batchItemFailures`/`itemIdentifier` wire shape, and wires a real `KinesisStreamCheckpointer` reporting the first-uncheckpointed sequence number. For a single shard, "first uncheckpointed by index" == "lowest uncheckpointed sequence number" — correct.

**[DIVERGENCE] Swallowed pipeline exception silently drops data when the trigger has no `ReportBatchItemFailures`** (Severity: High)
- `KinesisStreamApplication.CatchAndCheckpointPipeline` (`:77-88`) catches every exception, logs, always writes a `KinesisBatchResponse`, never rethrows.
- AWS: without `ReportBatchItemFailures` on the ESM, the partial-batch JSON is ignored and only a **thrown** function retries the batch; a normal return = full-success checkpoint.
- Impact: if a consumer forgets `FunctionResponseTypes=ReportBatchItemFailures` (Benzene neither sets nor checks it), a poison record produces a normal return → whole batch checkpointed → failed record and everything after it in that run is silently lost. The whole mechanism is load-bearing on a trigger setting Benzene can't see.
- Recommendation: document loudly that `ReportBatchItemFailures` is mandatory; consider failing the invocation when no checkpoint was made; generate trigger config + adapter together (Terraform codegen).

**[MISSING] No Kinesis tumbling-window support** (Severity: Medium)
- No `window`/`state`/`isFinalInvokeForWindow`; `KinesisBatchResponse` has no `state`. Cross-invocation windowed aggregation (canonical Kinesis analytics) can't be expressed. Add window state to the models or scope out explicitly.

**[MISSING] Manual-only checkpointing, no `UseCheckpointAfterEach`** (Severity: Low — documented) — most handlers checkpoint nothing → any exception reports record[0] → whole batch retried (safe, but no poison-pill progress).

Note: `KinesisStreamCheckpointer` uses `_records.IndexOf(lastProcessed)` (reference equality) — correct only if handlers pass back the same instances; a clone/projection yields `-1` → resume from record[0]. Doc caveat (Low).

**Verdict:** Strongest adapter; correct shape/resume math, but correctness depends on a trigger setting Benzene doesn't enforce, and tumbling windows are absent.

---

### DynamoDB Streams
Correctly processes records **sequentially, in shard order, stop-at-first-failure** (`DynamoDbApplication.cs:43-84`), reporting the first failed record's `SequenceNumber` as the single `batchItemFailure`. Matches AWS's stream contract.

**[DIVERGENCE] Same swallowed-exception data-loss risk without `ReportBatchItemFailures`** (Severity: High) — catches per record, never rethrows, always maps a response. Same root cause as Kinesis.

**[MISSING] No tumbling-window `state`** (Severity: Low) — less common for DynamoDB; lower priority.

**Verdict:** Correct ordering + partial-batch semantics; shares the trigger-dependent data-loss risk.

---

### S3
Fan-out over `S3Event.Records` (records can exceed one per notification). Fire-and-forget, no response — correct: S3→Lambda is async invoke governed by the function's `MaximumRetryAttempts`/on-failure destination/DLQ.

**[DIVERGENCE] Handler failure *result* silently dropped; only exceptions retry** (Severity: Medium — documented) — no `Options`/`RaiseOnFailureStatus`; a returned failure is diagnostics-only, invocation reports success, S3 async-retry/DLQ never engages. Consistent with AWS (only a failed invocation retries) but a real footgun. Offer an opt-in `RaiseOnFailureStatus` (the SNS package has the pattern).

Note: concurrent fan-out means one throwing record fails the whole invocation → all N reprocessed on retry (at-least-once double-processing). One-line caveat (Low).

**Verdict:** Correct single-vs-multi-record + async-retry; the sharp edge is documented.

---

### EventBridge
Single event per invocation, `detail-type` → topic, `detail` → body, fire-and-forget. Correct for EventBridge target invocation.

**[DIVERGENCE] Handler failure result silently dropped** (Severity: Medium — documented) — same shape as S3; only a thrown exception lets the rule target's retry/OnFailure engage. Provide the opt-in escalation out of the box.

**[WRONG-APPROACH] Topic routes on `detail-type` only, ignoring `source`** (Severity: Low) — `EventBridgeMessageTopicGetter` returns `new Topic(DetailType)`; `source` is metadata only. EventBridge rules match on `source` **and** `detail-type`; detail-type isn't globally unique (two systems both emitting `"OrderPlaced"` collide). Consider a `source:detail-type` composite topic option, or document that detail-types must be unique per bus.

**Verdict:** Correct single-event/async model; two documented gaps (failure-drop, source-agnostic routing).

---

### Kafka (MSK / self-managed)

**[DIVERGENCE] Concurrent fan-out breaks per-partition ordering** (Severity: High)
- `KafkaApplication` flattens `Records.Values.SelectMany(...)` and runs every record **concurrently** via `BoundedFanOut.WhenAllAsync` (unbounded default). Records within one topic-partition run in parallel.
- AWS: Lambda "reads messages sequentially for each Kafka topic partition… commits offsets per partition in order… ensures messages are processed in order within each partition." Per-partition ordering is a core Kafka guarantee the ESM preserves.
- Impact: for any ordered Kafka topic (CDC, event sourcing, keyed streams) Benzene processes a partition's records out of order — a correctness violation, and inconsistent with the DynamoDB adapter which went sequential for exactly this reason.
- Recommendation: process grouped by topic-partition, sequentially within each partition (fan out across partitions).

**[MISSING] No partial batch response / `KafkaBatchResponse`** (Severity: High — documented)
- No `Options`, no `RaiseOnFailureStatus`, no `batchItemFailures`. Only an unhandled exception fails the invocation, replaying the **entire** batch from the last committed offset.
- AWS: the MSK / self-managed Kafka ESM **does** support partial batch response (return `batchItemFailures` with `itemIdentifier`s by topic-partition/offset). Benzene has no story — the largest functional gap here, coupled to the ordering bug.
- Recommendation: add a `KafkaBatchResponse` + per-partition sequential processing reporting the first failed offset per partition. Highest-value addition of this review.

**Verdict:** Weakest adapter — undocumented per-partition ordering divergence plus documented absence of partial-batch reporting. Fix together.

---

### Lambda.Core cross-cutting
Routing (`AwsLambdaMiddlewareRouter.TryExtractRequest`) deserializes the whole payload into each candidate type, then `CanHandle` discriminates on `Records[0].EventSource` (or EventBridge's `detail-type`+`source`). Sound — homogeneous batches share an `EventSource`, so cross-source misrouting can't happen.

**[WRONG-APPROACH] Full-payload re-deserialization per candidate adapter** (Severity: Low) — each registered adapter re-parses the entire invocation before rejecting it; N full deserializations per invocation on the cold-path-sensitive hot path. Cheap discriminator peek first. Performance, not correctness.

**[MISSING] No timeout/`RemainingTime`-driven checkpoint or cancellation for stream sources** (Severity: Low — documented) — `ILambdaContext` exposes no token, only `RemainingTime`; a handler near timeout is killed with no chance to checkpoint → whole batch retried. Consider seeding a token derived from `RemainingTime`.

**Verdict:** Solid dispatch core; only low-severity perf/timeout refinements.

---

### Overall
Top priorities: (1) Kafka per-partition ordering + partial-batch response; (2) the shared Kinesis/DynamoDB "swallow-exception assumes `ReportBatchItemFailures` is enabled" data-loss risk — make the trigger requirement enforced/generated, not implicit. Batch-failure/checkpointing is correct where implemented (Kinesis, DynamoDB) and the biggest gap where absent (Kafka).
