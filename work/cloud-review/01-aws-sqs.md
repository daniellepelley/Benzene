## AWS SQS

Reviewed `src/Benzene.Aws.Sqs` (self-hosted poller + publishing client), `src/Benzene.Aws.Lambda.Sqs` (SQS-triggered Lambda), and `src/Benzene.Clients.Aws.Sqs` (egress) against the SQS Developer Guide and the Lambda SQS event-source-mapping docs.

---

**[DIVERGENCE] WholeBatch consumer deletes non-throwing failures (message loss)** (Severity: High)
- Benzene today: In `SqsConsumer.StartAsync` (`src/Benzene.Aws.Sqs/Consumer/SqsConsumer.cs:87-99`), the default `SqsConsumerAckMode.WholeBatch` deletes `result.Messages` (the *entire* batch) whenever no handler threw. A handler that returns an unsuccessful `IBenzeneResult` (e.g. `BenzeneResult.ServiceUnavailable(...)`) without throwing is deleted anyway — only a thrown exception keeps messages on the queue.
- AWS intent: SQS is at-least-once; "you should delete messages after they're successfully processed." Deleting a message you did not successfully process is data loss (SQS long/short polling docs).
- Impact: A downstream outage surfaced as a failure *result* rather than an exception silently drops messages. This is the default mode.
- Recommendation: Make `PerMessage` the default, or at minimum treat an unsuccessful result the same as a throw under `WholeBatch`. (Honestly called out in `Benzene.Aws.Sqs/CLAUDE.md` as "Unsafe by default" — a known gap, but the default still loses messages.)

**[DIVERGENCE] A message with no result set is treated as success and acked** (Severity: Medium)
- Benzene today: Both consumers ack on "not explicitly failed." Lambda: `SqsApplication.HandleAsync` (`src/Benzene.Aws.Lambda.Sqs/SqsApplication.cs:71`) reports a failure only when `context.IsSuccessful.HasValue && !context.IsSuccessful.Value`; a `null` `IsSuccessful` is omitted from `BatchItemFailures` and thus deleted by the ESM. Consumer: `SqsConsumerApplication.HandleAsync` (`src/Benzene.Aws.Sqs/Consumer/SqsConsumerApplication.cs:69`) treats `MessageResult?.IsSuccessful == false` as the only failure; a `null` `MessageResult` counts as success.
- AWS intent: at-least-once delivery expects unhandled messages to remain for redrive to a DLQ, not to be silently deleted.
- Impact: Any path where the result setter never runs — an unroutable message (missing/unknown `topic` attribute so the router matches no handler), or a middleware that short-circuits before `UseMessageHandlers` — acks and deletes the message. A producer that forgets the `topic` attribute can have messages silently discarded rather than dead-lettered.
- Recommendation: Default unset/`null` outcome to "failed" (report as `BatchItemFailure` / keep on queue) so unhandled messages redrive to the DLQ instead of being deleted.

**[DIVERGENCE] FIFO ordering is broken on the consumption side (concurrent fan-out + per-message settlement)** (Severity: High)
- Benzene today: Both `SqsApplication` and `SqsConsumerApplication` fan every record out concurrently via `BoundedFanOut.WhenAllAsync` (unbounded by default) with no `MessageGroupId` awareness, then in Lambda report only the individually-failed records via `BatchItemFailures`, and in the consumer's `PerMessage` mode delete only the individually-succeeded messages.
- AWS intent: For FIFO + `ReportBatchItemFailures`, "your function should stop processing messages after the first failure and return all failed and unprocessed messages in `batchItemFailures`... This helps preserve the ordering of messages" — messages in a group are ordered and a failure must block the rest of that group (Lambda SQS error handling).
- Impact: Point Benzene at a FIFO queue and (a) messages within a group are processed out of order (concurrent), and (b) a message *after* a failed one in the same group can be acked/deleted while the failed one is retried — a hard ordering violation. Nothing in code or the CLAUDE.md warns that FIFO consumption is unsupported.
- Recommendation: Detect FIFO (group-by `MessageGroupId`, forced serial per group; on first failure in a group report that message + all later same-group messages as failures / leave them on the queue), or explicitly document FIFO consumption as unsupported and guard against it.

**[MISSING] No visibility-timeout extension / heartbeat for long-running handlers** (Severity: Medium)
- Benzene today: `SqsConsumer` receives with no `VisibilityTimeout` override and deletes only after the *whole batch* finishes (`SqsConsumer.cs:70-99`); there is no `ChangeMessageVisibility` heartbeat anywhere. Lambda relies entirely on the ESM.
- AWS intent: a message not deleted within the visibility timeout (default 30s) becomes visible again and is redelivered; long handlers must extend visibility or size the timeout generously.
- Impact: A slow handler (or a large batch processed concurrently that collectively exceeds the timeout) causes the message to be redelivered to another poll/worker mid-processing — duplicate work. No knob and no warning.
- Recommendation: Expose a `VisibilityTimeout` on `SqsConsumerConfig` and/or a heartbeat that periodically calls `ChangeMessageVisibilityBatch` for in-flight messages; document the ESM visibility-timeout guidance (≥ handler duration) for the Lambda package.

**[WRONG-APPROACH] Egress forwards all Benzene headers to message attributes — can breach the 10-attribute cap** (Severity: Medium)
- Benzene today: `SqsContextConverter.CreateRequestAsync` and `OutboundSqsContextConverter.CreateRequestAsync` (`src/Benzene.Clients.Aws.Sqs/*.cs`) loop over every `Request.Headers` entry into `MessageAttributes`, then add the `topic` attribute, with no count guard.
- AWS intent: "Each message can have up to 10 attributes" (SQS message metadata). AWS also reserves a *system* attribute `AWSTraceHeader` for X-Ray whose size doesn't count against the message — Benzene instead carries W3C `traceparent`/`tracestate`/`baggage` as ordinary attributes, consuming slots.
- Impact: correlation-id + W3C trace headers + topic + status + a few app headers trivially exceeds 10, and `SendMessageAsync` throws. In `SqsBenzeneMessageClient` the throw is swallowed into `ServiceUnavailable` (`SqsBenzeneMessageClient.cs:97-101`), so it surfaces as an opaque send failure at scale.
- Recommendation: Validate/cap attribute count (fail fast with a clear message), and consider mapping trace context to the `AWSTraceHeader` system attribute rather than spending message-attribute slots.

**[MISSING] No SendMessageBatch, FIFO send, or DelaySeconds on producers** (Severity: Medium)
- Benzene today: `SqsMessageClient.PublishAsync` and the client middleware each issue exactly one `SendMessageAsync`; no `MessageGroupId`/`MessageDeduplicationId`, no `SendMessageBatch`, no `DelaySeconds`.
- AWS intent: `SendMessageBatch` (up to 10/call) is the standard throughput/cost lever; FIFO requires `MessageGroupId` (+ dedup); `DelaySeconds` provides per-message delay queues.
- Impact: 10× the API calls/cost at volume; no first-class FIFO or delayed-send story — adopters must drop to the raw SDK.
- Recommendation: Add a batched send path and FIFO parameters. (Honestly documented as a deliberate boundary in the CLAUDE.md files — known gaps, but real ones for scale/FIFO adopters.)

**[MISSING] Consumer doesn't request system attributes; no ApproximateReceiveCount / poison visibility** (Severity: Low)
- Benzene today: `SqsConsumer` sets `MessageAttributeNames = ["All"]` but never requests `AttributeNames`/`MessageSystemAttributeNames` (`SqsConsumer.cs:70-76`); the headers getters read only `String`-typed message attributes. So `ApproximateReceiveCount`, `SentTimestamp`, `MessageGroupId` etc. are never surfaced to handlers.
- AWS intent: `ApproximateReceiveCount` is the standard signal for poison-message/max-receives handling; DLQ redrive is driven by the queue's `maxReceiveCount`.
- Impact: handlers can't see retry count to make poison decisions.
- Recommendation: request system attributes and expose them (e.g. receive count) on `SqsConsumerMessageContext`.

**[MISSING] Short default long-poll wait; unchecked batch-delete result** (Severity: Low)
- Benzene today: `SqsConsumerConfig.WaitTimeSeconds` defaults to `1` (`SqsConsumerConfig.cs:21`). `SqsConsumer` ignores the `DeleteMessageBatchResponse.Failed` list (`SqsConsumer.cs:93-99`).
- AWS intent: long polling up to 20s is recommended to cut empty responses/cost; `DeleteMessageBatch` can partially fail per entry.
- Impact: a default of 1s yields many more empty `ReceiveMessage` calls than the recommended 20; an unnoticed partial delete failure silently leaves a processed message for redelivery.
- Recommendation: default `WaitTimeSeconds` to 20 (or document the tradeoff), and log/handle `DeleteMessageBatchResponse.Failed`.

---

Overall verdict: A competent standard-queue integration whose partial-batch-failure (Lambda) design and honestly-documented producer gaps are sound — but it silently mishandles at-least-once settlement at the edges (non-throwing failures and unrouted messages get acked/deleted), has no visibility-timeout heartbeat for long handlers, and quietly breaks FIFO ordering on the consumption side.
