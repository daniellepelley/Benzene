# Benzene.Aws.Sqs

## What this package does
AWS SQS utilities for Benzene, split into two independent pieces: a thin **publishing client**
(`Client/`) and a standalone **polling consumer worker** (`Consumer/`, documented in its own section
below). Both sit on top of `AWSSDK.SQS`.

## Key types/interfaces

### SQS Client (`Client/`)
- `ISqsClient` - `Task<string> PublishAsync(string topic, string message, string status)`
- `SqsMessageClient` - Publishes to a single queue (queue URL passed to the constructor). Each send is
  one `IAmazonSQS.SendMessageAsync` call carrying the message body plus a `topic` message attribute (and
  a `status` attribute when non-empty); it returns the send's HTTP status code as a string.

## When to use this package
- When publishing messages to SQS from Benzene apps
- For command/event dispatching via SQS
- When running a standalone (non-Lambda) SQS polling worker (see the `Consumer/` section)
- Complements Aws.Lambda.Sqs for receiving messages in Lambda

## Dependencies on other Benzene packages
- **Benzene.Core** / **Benzene.Core.MessageHandlers** / **Benzene.Abstractions.Pipelines** - message
  handling and pipeline infrastructure (used by the consumer)
- **Benzene.SelfHost** - `IBenzeneWorkerStartup` for the polling consumer
- **AWSSDK.SQS** / **Amazon.Lambda.SQSEvents** - AWS SDK and event types

## Important conventions
- `SqsMessageClient` tags each message with a `topic` (and optional `status`) message attribute; it does
  **not** map arbitrary Benzene headers onto message attributes. Both the topic and status attribute keys
  are configurable defaults, not hard-coded
  (`new SqsMessageClient(sqs, queueUrl, topicAttributeKey, statusAttributeKey)`) — keep them in sync with
  the consumer's keys. The polling consumer's topic key is `SqsConsumerConfig.TopicAttributeKey`
  (threaded into `AddSqsConsumer(topicAttributeKey)`).
- The target queue URL is supplied to `SqsMessageClient`'s constructor — it is not resolved from
  configuration by this package.
- **Not supported by the client:** FIFO queues (no `MessageGroupId`/`MessageDeduplicationId` is ever set),
  message deduplication, and batch sending (`SendMessageBatch`). Each `PublishAsync` is a single
  `SendMessageAsync`. If you need FIFO ordering, dedup, or batched sends, use the raw `AWSSDK.SQS` client
  directly — this is a deliberate boundary: Benzene abstracts message publishing at the business-logic
  layer and does not wrap the SQS SDK's own transport features.

## `Consumer/` — standalone polling worker (`SqsConsumer`/`UseSqs` on `IBenzeneWorkerStartup`)

### Settlement default: only messages that succeeded are deleted (safe by default)
`SqsConsumerOptions.AckMode` defaults to `SqsConsumerAckMode.PerMessage`. Under this default, only a
message whose handler reported an **explicit success** is deleted; a message whose handler returns a
failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`), throws, or is unrouted (result setter
never ran, so the outcome is unset) is left on the queue individually for redelivery/DLQ redrive.
This is a behavioral change from earlier versions, which defaulted to `WholeBatch` (a returned
failure or unrouted message was deleted along with the batch unless a handler *threw*). Set
`AckMode = SqsConsumerAckMode.WholeBatch` for the older all-or-nothing-on-throw behavior — see
`SqsConsumerAckMode`'s doc comments below. A redelivered message means the handler needs to be
idempotent — see [Capability Matrix](../../docs/capability-matrix.md) /
[Idempotency](../../docs/cookbooks/idempotency.md).

Distinct from the message-publishing client above - a long-running worker that polls a queue
directly (for `Benzene.HostedService`/`Benzene.SelfHost`, not Lambda). `SqsConsumerOptions.AckMode`
(via `UseSqs(config, clientFactory, action, configure)`'s optional `configure` parameter) controls
how a poll batch is acknowledged:
- `SqsConsumerAckMode.PerMessage` (**default**) - only the messages that actually succeeded (no
  thrown exception, and no unsuccessful `IBenzeneResult`) are deleted; failed messages are left on
  the queue individually, and one message's exception no longer aborts the whole poll iteration.
- `SqsConsumerAckMode.WholeBatch` (the older all-or-nothing-on-throw behavior) - the whole batch is
  deleted together, only once every message has run without throwing; any thrown exception leaves
  the entire batch on the queue.
`SqsConsumerMessageHandlerResultSetter` now records the outcome onto
`SqsConsumerMessageContext.MessageResult` (previously a no-op, since deletion never used to depend
on it) - `SqsConsumerApplication.HandleAsync` reads it to build the `SqsConsumerBatchResult`
(`SuccessfulMessages`/`FailedMessages`) that `SqsConsumer` uses to decide what to delete.

### Long polling + poison-message signal
- `SqsConsumerConfig.WaitTimeSeconds` defaults to **20** (maximum long polling) — a receive waits for
  messages rather than returning empty immediately, cutting empty-receive request cost and latency.
  Lower it only to make the poll loop spin faster (e.g. faster shutdown reaction).
- `SqsConsumer` requests the `ApproximateReceiveCount` system attribute on each receive and surfaces
  it as `SqsConsumerMessageContext.ApproximateReceiveCount` (`int?`), so a handler can make
  poison-message decisions (e.g. dead-letter after N deliveries).
- (Visibility-timeout heartbeat — extending a slow handler's message visibility on a background timer
  — is a separate, larger follow-up, not yet implemented.)

### Bounded batch fan-out (`SqsConsumerOptions.MaxDegreeOfParallelism`)
Defaults to `null` (unbounded - every message in a poll batch starts at once, the original
behavior). Set a positive value to cap how many messages run concurrently, e.g. so a large poll
batch can't open more scoped DB connections than the pool allows. Purely additive/opt-in; routed
through `Benzene.Core.Middleware`'s `BoundedFanOut`, same as `SqsConsumerAckMode`.

### W3C trace context and invocationId (release plan Tier 3.5)
`.UseW3CTraceContext<SqsConsumerMessageContext>()` works: `SqsConsumerMessageHeadersGetter` already
read real message attributes. Separately, `UseSqs(...)` now auto-wires `UseBenzeneInvocation()`
(`Consumer/BenzeneInvocationExtensions.cs`) as the first middleware, so `IBenzeneInvocation`
resolves inside each message's dispatch (`InvocationId` = the message's SQS `MessageId`, `Platform`
= `"Worker"`) - a long-running worker has no Lambda-style outer invocation boundary at all, so this
is the only invocation identity available here. No application code changes needed for either fix.
