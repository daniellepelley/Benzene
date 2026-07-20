## Azure Event Hubs

Reviewed `src/Benzene.Azure.EventHub` (self-hosted consumer), `src/Benzene.Azure.Function.EventHub` (Functions trigger), and `src/Benzene.Clients.Azure.EventHub` (producer).

**Headline:** the consumer's checkpoint/partition-ownership story is *correct*. Benzene uses `EventProcessorClient` (not `PartitionReceiver`), lets the caller own the blob checkpoint store + consumer group, dispatches one event at a time per partition, and checkpoints *only after successful processing* (at-least-once). It does **not** per-message-ack an Event Hub. The real problems are all on the **producer** side.

---

### [MISSING] Producer cannot set a partition key or partition ID (Severity: High)
- **Benzene today:** `EventHubClientMiddleware.HandleAsync` (`EventHubClientMiddleware.cs:37-46`) calls `_producerClient.CreateBatchAsync()` with **no** `CreateBatchOptions`. `EventHubSendMessageContext` carries only an `EventData`, and neither converter exposes any partition-key/partition-ID input.
- **Azure intent:** partition key is the *only* mechanism that keeps related events ordered — events with the same partitionKey always route to the same partition. Set via `CreateBatchOptions.PartitionKey`/`PartitionId` or `SendEventOptions.PartitionKey`. With no key, the service round-robins across partitions.
- **Impact:** Benzene's producer scatters a topic's events across all partitions. The consumer side loudly advertises per-partition sequential ordering — but a Benzene→Benzene pipeline can never realize that guarantee, because the producer gives no way to co-locate related events. The ordering promise is real on the read side and unreachable on the write side.
- **Recommendation:** add a partition-key (and optionally partition-ID) field to `EventHubSendMessageContext`, populate it in both converters (e.g. from a header or the topic), and pass it as `new CreateBatchOptions { PartitionKey = … }`.

### [WRONG-APPROACH] Producer sends one single-event batch per message (Severity: Medium)
- **Benzene today:** every `SendMessageAsync` builds a fresh `EventDataBatch` via `CreateBatchAsync()`, adds exactly one event, and calls `SendAsync`. One network round-trip per message; `EventDataBatch` is used purely as a size-guard wrapper, never as a batch.
- **Azure intent:** `EventDataBatch` exists to amortize throughput — accumulate many events with `TryAdd` until full, then one `SendAsync`.
- **Impact:** the throughput primitive Event Hubs is built around is unavailable; each publish pays a full AMQP round-trip.
- **Recommendation:** offer a batch-send entry point (accept a list, pack into one `EventDataBatch`, honoring partition key).

### [MISSING] No control over the Functions trigger's batch cardinality / maxBatchSize (Severity: Low — known, host-owned)
- Cardinality and `maxBatchSize` live in the author's `[EventHubTrigger]` + host.json; the Functions host owns batch checkpointing. Genuinely host-owned and documented honestly in the CLAUDE.md. No action; optionally cross-link the cookbook.

### [Known gap, honestly documented] Self-hosted worker: failure-result does not block checkpoint (Severity: Low)
- `EventHubConsumerContext.MessageResult` is diagnostics-only; a handler that *returns* a failure (without throwing) is checkpointed like a success. Event Hubs has no per-event settlement — checkpoint is the only lever — so this is a legitimate design choice matching the stream model, called out honestly in the package's "⚠️ Unsafe by default" section. Consider a future opt-in (`RaiseOnFailureStatus`-style) for parity with SQS/Kafka.

---

**Positives (not findings):** checkpoint occurs strictly *after* successful `HandleAsync`, per-partition, counter reset correctly — at-least-once, no eager-checkpoint data-loss window; `StopProcessingAsync` correctly deferred to a background `Task.Run` to avoid the documented in-handler deadlock, guarded by `Interlocked`; `DefaultStartingPosition` covers Earliest/Latest and offset/enqueued-time reads; consumer groups, blob checkpoint store, and load-balancing correctly delegated to the caller-built `EventProcessorClient`; `EventProcessorClient` over low-level `PartitionReceiver`/epoch is the recommended balanced approach.

**Verdict:** The consumer side is faithful to the partitioned-log/checkpoint model — no queue-style per-message-ack divergence. The correctness gap is on the **producer**: no partition-key support (High) makes the per-partition ordering the consumer advertises unachievable end-to-end, and single-event "batches" (Medium) forgo Event Hubs' core throughput primitive.
