## Kafka

Scope: `src/Benzene.Kafka.Core` + its dispatch primitive `src/Benzene.SelfHost/BoundedConcurrentDispatcher.cs`, against Confluent .NET Consumer/Producer semantics.

Headline: the single most likely correctness bug — concurrently dispatching one partition's records and committing the high-water offset — is **explicitly guarded against** and guarded well. The real gaps are on the produce side (no message keys) and the missing rebalance/DLQ/EOS story.

---

### [DIVERGENCE] Default settlement is at-most-once (offset stored on consume, before processing) (Severity: Medium)
- `CommitOnlyOnSuccess` defaults to `false` (`BenzeneKafkaConfig.cs:67`). In that mode Confluent defaults are untouched: `EnableAutoOffsetStore = true` and `EnableAutoCommit = true` (never set). librdkafka stores the offset the instant `Consume` returns — before `HandleAsync` runs — and auto-commits on a timer. A crash mid-handling, or a handler that throws while `CatchHandlerExceptions = true`, silently advances the committed offset past the un-processed record.
- Kafka: at-most-once is legitimate but a footgun as a *default*; most frameworks default to at-least-once. Silent data loss on crash/fault out-of-the-box.
- Recommendation: default `CommitOnlyOnSuccess = true`, or surface the choice prominently. Honestly documented (lowers to Medium), but "documented" isn't "safe by default."

### [WRONG-APPROACH — correctly avoided] Concurrent dispatch + StoreOffset watermark (Severity: Low — validated, not a defect)
- In `CommitOnlyOnSuccess` mode the worker enforces at `StartAsync` (`BenzeneKafkaWorker.cs:41-63`) that `CatchHandlerExceptions = false` **and** `PreserveOrderPerPartition = true`, throwing otherwise. Same-partition records route to one FIFO lane (`keySelector = Partition.Value`, capacity-1 single-reader lanes), and `StoreOffset` is called only after `HandleAsync` succeeds. The classic bug (dispatch a partition concurrently, commit the max offset, lose the gap) is structurally prevented. Keep — a model of getting it right. Residual: partitions share lanes via `partition % laneCount`, so a slow partition head-of-line-blocks co-located partitions (throughput, not correctness).

### [MISSING] Producer never sets a message Key → no partitioning control, no producer-side ordering (Severity: High)
- Every produce path builds `Message<string,string>` with only `Value` (+ `Headers` in one converter); `KafkaContextConverter` and `KafkaMessageContextConverter` **never set `Message.Key`**. The key determines the target partition (hash(key) → partition) and hence per-entity ordering. A null key means round-robin/sticky with no co-location.
- Impact: producers cannot preserve per-entity ordering or partition affinity — the consumer-side per-partition ordering the worker carefully protects is undermined at the source. Biggest concrete gap.
- Recommendation: thread a key from `IBenzeneClientRequest` / the message context into `Message.Key`.

### [MISSING] No consumer-group rebalance / revoke handling (drain-before-revoke) (Severity: Medium)
- Consumer built via `ConsumerBuilder(config).Build()` with no `SetPartitionsRevokedHandler`/`SetPartitionsAssignedHandler`. On rebalance, in-flight records for revoked partitions are neither drained nor committed before the partition moves; the worker may `StoreOffset` for a partition it no longer owns. Confluent recommends committing/draining in the revoke callback + cooperative-sticky assignment. Extra duplicate reprocessing (at-least-once) or extra loss (at-most-once) at the revoke boundary. Expose the revoke/assign callbacks + a built-in drain-before-revoke.

### [MISSING] Poison-message handling is skip-or-halt; no retry / dead-letter topic (Severity: High)
- A faulting handler either advances past it (dropped, silently in default mode) or stops the **entire worker**. No retry, no DLQ/retry-topic. A single poison record forces a choice between silent data loss and full consumer outage (and in at-least-once + `CatchHandlerExceptions=false`, a persistently-failing record wedges the worker on redelivery). Add a dead-letter/retry-topic story (`<topic>.DLT` after N attempts).

### [MISSING] No exactly-once / transactions, no idempotent-producer defaults (Severity: Medium)
- Hardcoded `IProducer<string,string>` via `ProduceAsync` per message. No transactional producer, no consume-transform-produce EOS; idempotence/acks left to the supplied producer. `enable.idempotence=true` + `acks=all` is the modern reliable default. Document recommended config; consider an EOS story.

### [MISSING] No schema-registry support; producer locked to `string,string` (Severity: Medium)
- Consumer is generic `TKey,TValue`, but the produce path is `Message<string,string>` with System.Text.Json to a string. No Avro/Protobuf/JSON-Schema serializers, no `ISchemaRegistryClient`. Interop with schema-governed topics requires bypassing Benzene. Allow a pluggable value/schema-registry serializer on produce.

### [MISSING] No seek/replay, no exposed manual `Commit()` (Severity: Low) — settlement only via `StoreOffset` + auto-commit; no `Seek`/`Assign` replay, no synchronous `Commit()`. Low priority; note as known gap.

### [MISSING] Produce path drops headers in one converter (Severity: Low)
- `KafkaContextConverter` forwards headers onto `Message.Headers`, but `KafkaMessageContextConverter` builds the message with `Value` only — no headers, no key. Correlation-id / W3C-trace-context lost on that path. Forward headers (and key) to match.

### [Note — not a defect] 'Benzene topic' == Kafka topic name for routing (Severity: Low)
- `KafkaMessageTopicGetter` returns `ConsumeResult.Topic` as the routing key, with a version header layered on. Reasonable (Kafka has no per-message type beyond topic/headers), but means one handler per Kafka topic unless a discriminator header is used. Document so users expecting per-message-type routing within a topic aren't surprised.

---

**Verdict:** The consumer worker is genuinely well-engineered — it correctly avoids the concurrent-dispatch/watermark ordering trap and offers a sound (if not default) at-least-once mode; the material gaps are the keyless producer (breaks produce-side partitioning/ordering), and the absent rebalance-drain, dead-letter/retry, and EOS/schema-registry stories.
