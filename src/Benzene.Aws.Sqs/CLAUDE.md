# Benzene.Aws.Sqs

## What this package does
AWS SQS client utilities for Benzene. Provides abstractions and implementations for publishing messages to SQS queues, handling message attributes, and integrating SQS with Benzene's message infrastructure.

## Key types/interfaces

### SQS Client
- SQS message publisher
- Queue URL resolution
- Message attribute handling

## When to use this package
- When publishing messages to SQS from Benzene apps
- For command/event dispatching via SQS
- When building distributed systems with SQS
- Complements Aws.Lambda.Sqs for receiving messages

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Aws.Core** - AWS core utilities
- **AWSSDK.SQS** - AWS SQS SDK

## Important conventions
- Message attributes mapped from Benzene headers
- Queue URLs resolved from configuration
- Batch sending supported for efficiency
- Message deduplication supported
- FIFO queue support included

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
