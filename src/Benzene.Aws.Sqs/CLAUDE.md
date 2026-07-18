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
  **not** map arbitrary Benzene headers onto message attributes.
- The target queue URL is supplied to `SqsMessageClient`'s constructor — it is not resolved from
  configuration by this package.
- **Not supported by the client:** FIFO queues (no `MessageGroupId`/`MessageDeduplicationId` is ever set),
  message deduplication, and batch sending (`SendMessageBatch`). Each `PublishAsync` is a single
  `SendMessageAsync`. If you need FIFO ordering, dedup, or batched sends, use the raw `AWSSDK.SQS` client
  directly — this is a deliberate boundary: Benzene abstracts message publishing at the business-logic
  layer and does not wrap the SQS SDK's own transport features.

## `Consumer/` — standalone polling worker (`SqsConsumer`/`UseSqs` on `IBenzeneWorkerStartup`)
Distinct from the message-publishing client above - a long-running worker that polls a queue
directly (for `Benzene.HostedService`/`Benzene.SelfHost`, not Lambda). `SqsConsumerOptions.AckMode`
(via `UseSqs(config, clientFactory, action, configure)`'s optional `configure` parameter) controls
how a poll batch is acknowledged:
- `SqsConsumerAckMode.WholeBatch` (default, unchanged from prior behavior) - the whole batch is
  deleted together, only once every message has run without throwing; any thrown exception leaves
  the entire batch on the queue.
- `SqsConsumerAckMode.PerMessage` - only the messages that actually succeeded (no thrown exception,
  and no unsuccessful `IBenzeneResult`) are deleted; failed messages are left on the queue
  individually, and one message's exception no longer aborts the whole poll iteration.
`SqsConsumerMessageMessageHandlerResultSetter` now records the outcome onto
`SqsConsumerMessageContext.MessageResult` (previously a no-op, since deletion never used to depend
on it) - `SqsConsumerApplication.HandleAsync` reads it to build the `SqsConsumerBatchResult`
(`SuccessfulMessages`/`FailedMessages`) that `SqsConsumer` uses to decide what to delete.
