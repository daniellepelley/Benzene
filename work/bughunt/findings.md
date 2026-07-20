# Overnight bug hunt — findings & disposition

Method: 8 parallel review agents across the whole codebase (recent commits first, then the
load-bearing core), each finding independently re-verified by the orchestrator before any change.
Every fix has a regression test; each was built + tested + committed + pushed to `main` separately.

## FIXED & pushed (11 commits)

| Sev | Area | Bug | Commit |
|-----|------|-----|--------|
| **CRITICAL** | Hosting | `CompositeBenzeneWorker` re-enumerated a deferred worker query, so `StopAsync` built & stopped a *fresh, never-started* worker set — every self-hosted worker's graceful-shutdown drain (Kafka/HTTP/SQS/ServiceBus/EventHub + the new Kafka DrainOnRevoke) was a silent no-op. | `a4876c0` |
| HIGH | SNS egress | Converters didn't skip empty-valued headers → real SNS rejects the empty attribute and fails the **whole publish** (LocalStack hid it). | `c796f65` |
| MED | Clients egress | `OutboundContext` aliased the caller's header dict; outbound trace/correlation middleware mutated it → stale traceparent leaked across sends reusing one dict. | `c796f65` |
| HIGH | Kafka | Dead-letter **produce failure** under the default config was silent loss (offset auto-stored, exception swallowed). DLT now manages offsets manually and stops the worker on produce failure → redelivery, not loss. | `dd40729` |
| MED | Kafka | Rebalance revoke handler's blanket `Commit()` committed in-flight offsets of **non-revoked** partitions under auto-store → silent loss on later crash. Now commits only under manual-offset modes. | `dd40729` |
| MED | Kafka | `DrainOnRevoke` was a silent no-op with a custom consumer factory (DIM drops the callback) — now warns at startup. | `dd40729` |
| LOW-MED | SelfHost | `BoundedConcurrentDispatcher` leaked a lane's outstanding count when the lane died with a queued item → every later `DrainLanesAsync` burned its full timeout. | `dd40729` |
| MED | Core | `ExceptionHandlerMiddleware` swallowed a fired-token `OperationCanceledException` into "success" → settle/ack transports drop an interrupted message on shutdown. Now rethrows genuine cancellation. | `6cba5c5` |
| MED | Cosmos | Both change-feed workers' skip mode swallowed shutdown cancellation and checkpointed a partial batch (lost the tail). Now propagate. All-versions worker: map moved inside try. | `6cba5c5` |
| MED | DynamoDB | `IsSuccessful.HasValue && !…` let a **null** result advance the shard checkpoint past an un-processed ordered record (silent skip). Now `!= true`, matching SQS. | `ccead61` |
| MED | ServiceBus | Worker + Functions trigger completed a **null** result (Explicit ack) — now abandon for redelivery, matching SQS. | `ccead61` |
| MED | Kinesis | `CheckpointAsync` set the watermark via `IndexOf` unconditionally; `IndexOf == -1` (a foreign/projected record) **rewound** the resume point to the batch start → reprocess everything. Now monotonic. | `21f7333` |
| MED | Kafka | Header getters (×4) threw `ArgumentNullException` on a null header value (a valid wire state) → hard-failed the message. Now coalesce null→empty. | `cde36ba` |
| LOW | Batch/SQS | SQS/SNS batch clients enumerated `response.Failed` unguarded (NRE on an AWS SDK v4 upgrade); SQS inbound getters unguarded on `MessageAttributes`. | `cde36ba` |
| LOW | Resilience | Retry backoff growth overflowed `TimeSpan` on many retries → `OverflowException` outside the loop. Clamped. | `6960590` |

## Verified CORRECT (agent-checked, no defect) — highlights
Batch failure-index mapping (SQS/SNS by id, EventBridge positional); ServiceBus/EventHub batch
roll/dispose logic; DI-scope disposal on all paths (incl. streaming); `BoundedFanOut` semaphore
release/order; Saga LIFO compensation + retry idempotency; Event Hub / Kafka-Lambda / SQS-worker
settlement; MemoryHealthCheck / ShutdownReadinessHealthCheck logic; gRPC rich-error trailer encoding
& deadline math; Cosmos DIM-exception observability & NullStreamCheckpointer path.

## LEFT FOR MAINTAINER DECISION (not changed unilaterally overnight)

These are genuine issues but are **semantic/design decisions**, not mechanical bugs — changing them
alters a documented contract or behavior and deserves your call:

1. **Kinesis up-to checkpointing is unsafe under out-of-order `PartitionBy`** (the flagship documented
   usage). A handler that checkpoints partition A's last record claims "up to here safe" while an
   earlier-in-batch record of partition B is unprocessed → that B record is skipped on failure. The
   monotonic fix (`21f7333`) does **not** close this — it needs a per-record (set-based) checkpoint
   model or a documented "checkpoint only in batch order" constraint. `KinesisStreamCheckpointer`.
2. **Kinesis: a successful batch that never checkpoints reprocesses forever.** `BuildResponse` returns
   the resume point unconditionally, so a handler using the `UseStream((records,ct)=>…)` overload
   (which exposes no checkpointer) reports record 0 even on success → AWS redelivers the whole batch.
   Cosmos solved the analogous case with `AutoCheckpointOnSuccess=true`; Kinesis wants the same
   default (or a `UseCheckpointAfterEach()` operator). `KinesisStreamApplication`.
3. **Split-brain `RaiseOnFailureStatus` defaults** across transports (settlement agent #5): SQS/Kafka/
   ServiceBus-worker/RabbitMQ retain a failure *result*; S3/EventBridge/QueueStorage/EventGrid/
   EventHub-trigger discard it by default. A per-transport tradeoff, but the inconsistency bites users
   expecting at-least-once. Consider aligning (or louder docs).
4. **RabbitMQ null-result → ack** (settlement #3): left as-is because it's explicitly documented and
   tested as a deliberate choice, unlike ServiceBus/DynamoDB which were fixed. Flagging for
   cross-transport consistency only.

## NOT fixed (LOW, cosmetic/edge — cheap follow-ups if desired)
- Batch clients: a per-entry *conversion* failure mid-batch aborts the whole `SendBatchAsync` after a
  partial send (should record it as that entry's failure); ServiceBus/EventHub batch leak the native
  batch on an exception before Dispose (needs try/finally). (batch agent #1/#2)
- Cosmos `MapChangeType` default→Replace mislabels any future SDK op type (latent; safe today).
- gRPC `ReadAll`/`Convert` missing `[EnumeratorCancellation]`; OK-with-no-payload unary → `Unknown`.
- `MiddlewareRouter` null-guard is always-false for a value-type `TRequest`.
- `MiddlewarePipelineBuilder` is mutable but its CLAUDE.md claims "immutable, returns new instance"
  (doc drift; internal code is safe — only a consumer forking a shared builder would be surprised).
- Outbound Kafka converters `GetBytes(header.Value)` can throw on a null header value (inbound getters
  fixed; outbound is caller-controlled, lower risk).
