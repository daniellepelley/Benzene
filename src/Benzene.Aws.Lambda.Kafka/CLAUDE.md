# Benzene.Aws.Lambda.Kafka

## What this package does
AWS Kafka Lambda integration for Benzene (MSK and self-managed Kafka). Processes Kafka events from Lambda event source mappings through Benzene's message handler pipeline. Handles batch processing, Kafka headers, and partition/offset management.

## Per-partition ordering + partial-batch-failure reporting (closes the old "unsafe by default" gap)
`KafkaApplication` is `IMiddlewareApplication<KafkaEvent, KafkaBatchResponse>` and honours Kafka's
per-partition ordering: records **within** a topic-partition run sequentially in offset order and
**stop at the first failure**; different topic-partitions fan out concurrently (bounded by
`KafkaOptions.MaxDegreeOfParallelism`, which now caps *partitions*, not individual records). Each
partition that fails reports the offset to resume from in a `KafkaBatchResponse`, so the event source
mapping redrives just that partition from that offset (`KafkaBatchFailureMode.PartialBatchFailure`,
the default), or `FailWholeBatch` throws `KafkaBatchProcessingException` to retry the whole batch.
This replaces the earlier `MiddlewareMultiApplication` fan-out that flattened away partition grouping
(losing offset ordering) and silently dropped every returned failure result.

⚠️ **Kafka's `itemIdentifier` wire shape is different from Kinesis/DynamoDB/SQS.** It is a JSON
*object* — `{ "partition": "topic-partition_number", "offset": <number> }` — not a bare string. The
`partition` is exactly the `KafkaEvent.Records` dictionary key (e.g. `"my-topic-0"`); `offset` is the
first failed record's offset. Verified against the AWS "Configuring error handling controls for Kafka
event sources" docs. Honoured only when the event source mapping has
`FunctionResponseTypes=ReportBatchItemFailures`; without it, AWS ignores the return value and only a
thrown exception (`FailWholeBatch`, or an uncaught fault) retries — same as before.

**One behavioural note on unset outcomes:** an *unrouted* record (topic matched no handler, so the
result setter never ran → `MessageResult == null`) is treated as processed and skipped, **not**
reported as a failure. This is deliberate and differs from SQS (which reports a null outcome as a
failure to redrive to a DLQ): Kafka has no per-record DLQ, and reporting a null outcome would replay
the partition from that offset forever. Only an explicit `IsSuccessful == false` result or a thrown
exception counts as a failure.

## W3C trace context and invocationId (release plan Tier 3.5)
`.UseW3CTraceContext<KafkaContext>()` works: `KafkaMessageHeadersGetter` already read real Kafka
record headers. Separately, `UseKafka(...)` now auto-wires `UseBenzeneInvocation()`
(`BenzeneInvocationExtensions.cs`) as the first middleware, so `IBenzeneInvocation` resolves inside
each record's dispatch (`InvocationId` = `"{topic}-{partition}-{offset}"` - Kafka has no single
message-id field) - previously this threw/silently came back `null`, because each record is
dispatched through its own DI scope via `MiddlewareMultiApplication`'s per-record
`CreateScope()`, disconnected from whatever the outer Lambda invocation populated. No application
code changes needed for either fix.

## Key types/interfaces

### Application & Handler
- `KafkaApplication` - Kafka application for Lambda
- `KafkaLambdaHandler` - Lambda function handler for Kafka

### Context
- `KafkaContext` - Context for Kafka message processing

### Message Handling
- `KafkaMessageBodyGetter` - Extracts message body from Kafka record
- `KafkaMessageHeadersGetter` - Extracts Kafka headers
- `KafkaMessageTopicGetter` - Extracts Kafka topic
- `KafkaMessageHandlerResultSetter` - Sets result on context

### Other
- `KafkaRegistrations` - Registers Kafka services
- Extension methods for configuration

## When to use this package
- When building Lambda functions triggered by Kafka (MSK)
- For Kafka event stream processing with Benzene
- When you need event sourcing with Kafka
- For microservices consuming Kafka topics

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.Aws.Lambda.Core** - AWS Lambda core
- **Amazon.Lambda.KafkaEvents** - Kafka event types

## Important conventions
- Processes Kafka records in batches, **per-partition-sequential in offset order, fan-out across
  partitions** (see the top-of-file section) — not a flat all-records fan-out.
- Kafka headers mapped to Benzene headers
- Topic name extracted from Kafka record
- Partition and offset available in context
- **Failure handling** (`KafkaOptions`, passed via `UseKafka(action, configure)`):
  `BatchFailureMode.PartialBatchFailure` (default) reports each failed partition's resume offset via
  `KafkaBatchResponse` for `ReportBatchItemFailures`-configured triggers; `FailWholeBatch` throws
  `KafkaBatchProcessingException` to retry the whole batch. A returned `IsSuccessful == false` result
  or a thrown exception is a failure; an unset outcome (unrouted record) is skipped, not reported.
- Message key available in context
- Supports both MSK and self-managed Kafka
- **Bounded partition fan-out**: `KafkaOptions.MaxDegreeOfParallelism` (via `UseKafka(action,
  configure)`) optionally caps how many *topic-partitions* run concurrently; `null` (the default)
  leaves the fan-out unbounded. Records within a partition are always sequential. Routed through
  `Benzene.Core.Middleware`'s `BoundedFanOut`. (Note: the old `UseKafka(action, int?
  maxDegreeOfParallelism)` positional overload is replaced by `UseKafka(action, Action<KafkaOptions>
  configure)`, matching the SQS shape — set `MaxDegreeOfParallelism` on the options object.)

## Tests
- `test/Benzene.Core.Test/Aws/Kafka/KafkaGettersTest.cs` — the three getters directly: body
  (UTF-8 decode), topic, and headers (empty-header-batch case returning `{}`, plus the positive
  case asserting both the decoded header value and the injected `topic` entry).
- `test/Benzene.Core.Test/Aws/Kafka/KafkaMessagePipelineTest.cs` — end-to-end sends (JSON, XML,
  unprocessable-entity, Kafka-in/SNS-out fan-out), `CanHandle` routing (`Send_FromStream` for the
  matching `aws:kafka` event source, `Send_FromStream_NonKafkaEvent_DoesNotRoute` for a mismatched
  one), and `MultiplePartitionsAndRecords_AllRecordsAreProcessed` covering more than one partition
  and more than one record per partition.
- `test/Benzene.Core.Test/Aws/Kafka/KafkaBatchFailureModeTest.cs` — the per-partition ordering +
  partial-batch behaviour: records within a partition process in offset order; a failure reports the
  first failed offset and stops that partition while others complete; a thrown exception is contained
  to its partition; an unset outcome is skipped (not reported); `FailWholeBatch` throws listing
  failed partitions; all-success returns an empty `KafkaBatchResponse`; option defaults.
