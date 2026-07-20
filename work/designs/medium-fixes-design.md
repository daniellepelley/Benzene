# Design — Medium-severity cloud/transport fixes (#29, #30)

Design for the Medium-tier issues from the cloud review. Each item: **approach**, **API**,
**backward-compat**, **tests**, **effort**. All follow Benzene's established conventions (opt-in,
additive, caller owns the SDK client, context stays transport-shape-only).

Guiding principles for this tier:
- **Additive & opt-in** wherever a default change would be behavioral — the safe-default flips have
  already shipped; these are capability additions.
- **Mirror the existing per-transport pattern** rather than inventing new abstractions (e.g. batch
  send mirrors the single-send converter; a partition/dedup key rides a configurable header, like the
  Event Hub `partitionKeyHeader` / Kafka `keyHeader` already shipped).
- **Keep the SDK's own features reachable** — Benzene exposes the knob, the caller owns the client.

---

## #29 — Provider feature gaps (Kafka worker, gRPC, Service Bus)

### 29.1 Kafka worker — consumer-group rebalance drain + dead-letter/retry topic

**Problem.** `BenzeneKafkaWorker` builds its `IConsumer` with no partition-revoked/assigned handlers,
so in-flight records for a revoked partition are neither drained nor committed before it moves; and a
poison record forces a choice between silent skip (`CatchHandlerExceptions=true`) and halting the
whole worker (`false`) — there is no retry/dead-letter path.

**Approach — rebalance drain.** The `IKafkaConsumerFactory` seam already exposes the
`ConsumerBuilder`, so a caller *can* set handlers today, but the worker owns the dispatcher lanes and
must coordinate the drain. Add, on `BenzeneKafkaConfig`:
- `bool DrainOnRevoke` (default `true` when `CommitOnlyOnSuccess` is on, else `false`) — when set, the
  worker registers `SetPartitionsRevokedHandler` and, on revoke, stops feeding new records for the
  revoked partitions, waits (bounded by `DrainTimeout`) for their in-flight dispatcher lanes to
  finish, then `Commit()`s their stored offsets before returning from the callback.
- Prefer `PartitionAssignmentStrategy.CooperativeSticky` guidance in docs (a `ConsumerConfig`
  passthrough, no Benzene code needed) to reduce stop-the-world rebalances.

This needs the worker to (1) key dispatcher lanes by `TopicPartition` (already does via
`PreserveOrderPerPartition`), and (2) expose a "quiesce these partitions" call to the
`BoundedConcurrentDispatcher`. Add `Task DrainLanesAsync(IEnumerable<int> laneKeys, TimeSpan timeout)`
to the dispatcher (lanes are already keyed by `partition % laneCount`).

**Approach — dead-letter/retry topic.** Add an opt-in `KafkaDeadLetterOptions` on the worker:
```csharp
public class KafkaDeadLetterOptions {
    public string DeadLetterTopic { get; set; }          // e.g. "<topic>.DLT"; null = disabled
    public int MaxAttempts { get; set; } = 1;            // in-process retries before dead-lettering
    public IProducer<string,string> Producer { get; set; } // caller-built; Benzene doesn't wrap auth
}
```
On a handler failure/exception, the worker retries up to `MaxAttempts` (with the record's existing
key/headers), then produces the original record — key, value, headers, plus `x-dlt-reason`/
`x-dlt-original-topic`/`x-dlt-original-offset` headers (exception **type name** only, never the
message) — to `DeadLetterTopic`, and commits the offset so the partition advances. This keeps a
poison record from wedging the partition without silently losing it.

**Backward-compat.** Both additive: `DrainOnRevoke` defaults preserve current behavior except when
`CommitOnlyOnSuccess` is on (where draining is strictly safer); dead-letter is off unless
`DeadLetterTopic` is set.

**Tests.** Dispatcher `DrainLanesAsync` unit test (lanes quiesce within timeout); dead-letter routing
via a mocked producer (failed record → produced to DLT with reason headers, offset committed);
retry-then-dead-letter attempt counting. Live rebalance behavior stays an integration/emulator test.

**Effort.** Medium-High (dispatcher drain is the fiddly part). Split into two PRs: (a) rebalance
drain, (b) dead-letter topic.

### 29.2 gRPC — streaming client, deadline computation, rich errors

**Problem.** The gRPC *client* is unary-only and computes no deadline (cancellation-token propagation
already shipped); non-OK results collapse to a flat `RpcException(Status, detail)` with no structured
detail; a mid-stream fault surfaces as `UNKNOWN`.

**Approach — deadline computation (small, do first).** The server already seeds the ambient token; add
an `IGrpcServerCallAccessor`-sourced deadline. On the server side, when handling an inbound call,
capture `ServerCallContext.Deadline` into a scoped `GrpcCallDeadlineAccessor` (mirrors the
cancellation accessor). In `GrpcBenzeneMessageClient`, resolve it and pass the **remaining** deadline
(`deadline - now`, floored) into `GrpcContextConverter`'s existing `Deadline` field (which
`GrpcClientRoute` already forwards to `CallOptions`). `DateTime.UtcNow` isn't available in workflow
scripts but is fine in library code. Guard against a null/expired deadline (send none).

**Approach — streaming client.** `GrpcClientRouteRegistry` hard-codes `MethodType.Unary`. Add
route registration for the other three `MethodType`s and, in `GrpcClientRoute.InvokeAsync`, branch to
`invoker.AsyncServerStreamingCall` / `AsyncClientStreamingCall` / `AsyncDuplexStreamingCall`. This
needs a streaming-capable client surface beyond the current one-shot `IBenzeneMessageClient.SendMessageAsync`
— introduce `IBenzeneStreamingClient` with `IAsyncEnumerable<TResponse> ServerStreamAsync<TReq,TResp>(...)`
etc., mirroring the server's `GrpcStreamAdapter`. Keep `IBenzeneMessageClient` unary-only (unchanged).

**Approach — rich errors.** In `GrpcMethodHandler`, when a Benzene result carries structured errors
(esp. `ValidationError` → field violations), also attach a `google.rpc.Status` to the
`grpc-status-details-bin` trailer via `Grpc.StatusProto`/`RpcException` metadata, alongside the
existing `benzene-status` trailer. Map `ValidationError` → `BadRequest.FieldViolations`. Wrap the
server-streaming enumeration in the same `IGrpcStatusCodeMapper` so mid-stream faults map instead of
becoming `UNKNOWN`.

**Backward-compat.** All additive. `IBenzeneMessageClient` stays unary; streaming is a new interface.
Rich errors add a trailer without changing the existing one. Deadline uses the already-present (unused)
`Deadline` field.

**Tests.** Deadline: server handler with a short inbound deadline → downstream `CallOptions.Deadline`
is set to ~remaining (fake clock via injected `IClock`). Streaming: server-streaming round-trip
through the new client interface against the in-proc test server. Rich errors: a validation failure →
client reads `BadRequest.FieldViolations` from trailers.

**Effort.** Deadline = Small. Rich errors = Medium. Streaming client = Medium-High (new interface +
adapters). Ship deadline first, then rich errors, then streaming as its own PR.

### 29.3 Service Bus — explicit dead-letter, deferral, lock-renewal, sender properties

**Problem.** The worker's `Explicit` mode (now default) can only complete/abandon — a poison message
abandon-loops to max-delivery-count; no deferral; no `MaxAutoLockRenewalDuration` for long handlers;
the sender can't set `MessageId`/`SessionId`/`ScheduledEnqueueTime`/`TimeToLive`.

**Approach — richer settlement outcome.** Today the worker reads `IMessageResult.IsSuccessful`
(true→complete, false→abandon). Introduce a small settlement intent the handler can express without
polluting the context. Add an optional scoped `ServiceBusSettlementHolder` (DI-registered, like
`PresetTopicHolder`) a handler can resolve and set:
```csharp
public enum ServiceBusSettlement { Complete, Abandon, DeadLetter, Defer }
public class ServiceBusSettlementHolder {
    public ServiceBusSettlement? Override { get; set; }
    public string DeadLetterReason { get; set; }
    public string DeadLetterDescription { get; set; }  // never a secret
}
```
The worker, after the pipeline, applies: explicit `Override` if set, else the current
IsSuccessful→complete/abandon default. `DeadLetter` → `DeadLetterMessageAsync(reason, description)`;
`Defer` → `DeferMessageAsync` (deferred receive is a separate, documented advanced path — out of scope
for the first cut, but the enum reserves it). This keeps `ServiceBusConsumerContext` pure (the intent
lives in a scoped holder, per the repo's context-purity convention).

**Approach — lock renewal.** Add `TimeSpan? MaxAutoLockRenewalDuration` to `BenzeneServiceBusConfig`,
passed straight to `ServiceBusProcessorOptions.MaxAutoLockRenewalDuration` (null = SDK default).
One-line plumb.

**Approach — sender properties.** Add optional configurable header keys to the send converters,
mirroring the shipped `topicPropertyKey`/`partitionKeyHeader` pattern:
`messageIdHeader`, `sessionIdHeader`, `scheduledEnqueueTimeHeader`, `timeToLiveHeader`. When set and the
header is present, map it onto the corresponding `ServiceBusMessage` property (parsing
`ScheduledEnqueueTime` as ISO-8601 / `TimeToLive` as an ISO-8601 duration or seconds). `MessageId`
enables broker-side duplicate detection (valuable given at-least-once); `SessionId` is required to
produce to a session entity (pairs with the sessions work in #21).

**Backward-compat.** All additive/opt-in; the settlement holder is only consulted if resolved+set.

**Tests.** Worker settlement: holder set to `DeadLetter` → `DeadLetterMessageAsync` called with the
reason (mocked `ProcessMessageEventArgs`-equivalent, as the existing worker tests do). Lock-renewal
option flows to `ServiceBusProcessorOptions`. Sender: each header maps to the right `ServiceBusMessage`
property (converter unit tests, mirroring the existing ones).

**Effort.** Lock-renewal = trivial. Sender properties = Small. Settlement holder + dead-letter =
Medium. Deferral-receive = deferred (advanced, own issue).

---

## #30 — Batch producers, and SQS/SNS/StepFunctions/EventHub-worker/Cosmos polish

### 30.1 Batch send across egress clients

**Problem.** Every egress client sends one message per call, forgoing the provider batch primitive.

**Approach.** Add a `SendBatchAsync` to `IBenzeneMessageClient` implementations (or a new
`IBenzeneBatchMessageClient` to avoid touching the core interface) that accepts
`IReadOnlyCollection<IBenzeneClientRequest<T>>`, chunks to the provider's max, and issues the batch
call, mapping per-entry failures back:
- **EventBridge** `PutEvents` (≤10, 256 KB) — already handles `FailedEntryCount`; extend to N entries.
- **SNS** `PublishBatch` (≤10), **SQS** `SendMessageBatch` (≤10) — each returns per-entry
  `Successful`/`Failed`; surface a `BatchSendResult` listing failed entries (by a caller-supplied id).
- **Event Hub** `EventDataBatch` — accumulate via `TryAdd` until full (honoring partition key), then
  one `SendAsync`; roll to a new batch when `TryAdd` returns false.
- **Event Grid** `SendEventsAsync(IEnumerable)`, **Service Bus** `ServiceBusMessageBatch`.

Introduce a shared `BatchSendResult` shape (`IReadOnlyList<FailedEntry>` where `FailedEntry` carries
the caller's request index + error code + message-type-name). New interface, so the core
single-send `IBenzeneMessageClient` is unchanged.

**Backward-compat.** Additive (new interface/methods). Existing single-send untouched.

**Tests.** Per client: N>10 requests chunk into ⌈N/10⌉ calls (mocked SDK client, verify call count);
a per-entry failure surfaces in `BatchSendResult`; Event Hub over-size event rolls to a new batch.

**Effort.** Medium (one converter/middleware batch path per transport, but repetitive). Ship
per-transport PRs; EventBridge first (partial-failure plumbing already exists).

### 30.2 SQS consumer/producer polish

- **10-attribute cap guard** (producer): before send, if `MessageAttributes.Count > 10`, fail fast
  with a clear `InvalidOperationException` naming the offending count (instead of an opaque SDK throw
  swallowed to `ServiceUnavailable`). Optionally add `bool UseAwsTraceHeaderForTraceContext` on the
  converter that maps W3C trace headers onto the reserved `AWSTraceHeader` *system* attribute (doesn't
  count against the 10). Additive.
- **Visibility-timeout heartbeat** (consumer): add `TimeSpan? VisibilityTimeout` +
  `bool HeartbeatVisibility` to `SqsConsumerConfig`. When heartbeat is on, a background timer calls
  `ChangeMessageVisibilityBatch` for in-flight messages at ~⅓ of the visibility window until they're
  deleted. Bounded by a `MaxVisibilityExtension`. Medium (timer lifecycle per poll batch).
- **`WaitTimeSeconds` default → 20** (long polling) — a behavioral change but strictly cost-reducing;
  ship as a default flip with a doc note, or as `= 20` only when unset. Trivial.
- **Surface `ApproximateReceiveCount`** — request `MessageSystemAttributeNames = ["ApproximateReceiveCount"]`
  and expose it on `SqsConsumerMessageContext`/`SqsMessageContext` so handlers can make poison
  decisions. Small.

**Tests.** Attribute-cap guard (11 attrs → throws); heartbeat timer calls ChangeMessageVisibility for
a slow handler (mocked clock + SDK); receive-count surfaced on the context.

### 30.3 SNS FIFO + filter-policy number type

- **FIFO**: add `messageGroupIdHeader`/`messageDeduplicationIdHeader` to the SNS converters (same
  configurable-header pattern as the shipped `topicAttributeKey`); when set, map to
  `PublishRequest.MessageGroupId`/`MessageDeduplicationId`. Enables `.fifo` topics. Small, additive.
- **Filter-policy Number**: when forwarding a header whose value parses as a number, set
  `DataType = "Number"` so numeric subscription filter policies match. Opt-in via a
  `bool InferNumericAttributeTypes` flag (default false to avoid surprising type changes). Small.

**Tests.** FIFO group/dedup headers map to the publish request; numeric header → `DataType=Number`
when the flag is on.

### 30.4 StepFunctions idempotency name

**Problem.** `StartExecutionRequest.Name` is never set, so a retry-after-lost-response starts a
duplicate execution. Needs a stable idempotency token — which requires a correlation id, absent from
the current `IStepFunctionsClient.StartExecutionAsync<T>(T message)` signature.

**Approach.** Add an overload `StartExecutionAsync<TMessage,TResponse>(TMessage message, string executionName)`
and, on the client, an optional `Func<TMessage,string> nameSelector` (or read a correlation id from a
supplied headers dictionary). Prefer plumbing the outbound request's headers: change the client's
entry to accept the `IBenzeneClientRequest` (which carries `Headers`), derive the name from a
configurable correlation-id header (`x-correlation-id`), sanitize to Step Functions' name charset,
and set `StartExecutionRequest.Name`. On `ExecutionAlreadyExists`, treat as success (idempotent).
This is a small interface addition (new overload; old one delegates with a generated name = today's
behavior).

**Backward-compat.** Additive overload; existing call path unchanged (auto-name).

**Tests.** Same correlation id → same execution name (verify on a mocked `IAmazonStepFunctions`);
`ExecutionAlreadyExists` maps to success.

**Effort.** Small-Medium (interface addition + charset sanitization).

### 30.5 Event Hub self-hosted worker — RaiseOnFailureStatus parity

**Approach.** Mirror the escalation just shipped for the Function triggers: add
`BenzeneEventHubConfig.RaiseOnFailureStatus` (default false). When set and the handler returns a
failure result, the worker throws before checkpointing (so the partition doesn't advance past the
failed event), matching the existing `CatchHandlerExceptions=false` exception path. Since Event Hubs
is checkpoint-based, "abandon" isn't available — the semantics are "don't checkpoint, reprocess from
here" (at-least-once, requires idempotency). Additive/opt-in.

**Tests.** Worker with `RaiseOnFailureStatus=true` + a failure-returning handler → checkpoint not
advanced (mirrors the existing worker unit tests).

**Effort.** Small.

### 30.6 Cosmos change feed — all-versions-and-deletes mode

**Approach.** `CosmosChangeFeedProcessorFactory` hard-codes
`GetChangeFeedProcessorBuilderWithManualCheckpoint` (latest-version). Add a
`CosmosChangeFeedMode { LatestVersion, AllVersionsAndDeletes }` config; in
`AllVersionsAndDeletes`, use `GetChangeFeedProcessorBuilderWithAllVersionsAndDeletes` so deletes and
intermediate versions surface (requires container retention configured by the caller). The change
feed item model gains a `ChangeType` (Create/Replace/Delete) when in that mode. Additive; default
stays latest-version.

**Tests.** Factory builds the all-versions processor when configured (unit-level, no live Cosmos);
document the container-retention prerequisite.

**Effort.** Small-Medium.

---

## Suggested sequencing (Medium tier)
1. **Trivial/Small, high-value first**: Service Bus lock-renewal + sender properties; SNS FIFO;
   Event Hub worker RaiseOnFailureStatus; SQS attribute-cap guard + `ApproximateReceiveCount` +
   WaitTimeSeconds; gRPC deadline; StepFunctions idempotency name.
2. **Batch send** per transport (EventBridge → SNS/SQS → Event Hub/Event Grid/Service Bus).
3. **Larger**: Service Bus settlement holder + dead-letter; Kafka rebalance-drain then dead-letter
   topic; gRPC rich errors then streaming client; Cosmos all-versions.
