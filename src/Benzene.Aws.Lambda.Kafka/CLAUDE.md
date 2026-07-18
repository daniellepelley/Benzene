# Benzene.Aws.Lambda.Kafka

## What this package does
AWS Kafka Lambda integration for Benzene (MSK and self-managed Kafka). Processes Kafka events from Lambda event source mappings through Benzene's message handler pipeline. Handles batch processing, Kafka headers, and partition/offset management.

## ⚠️ Unsafe by default, and there is no opt-out: a handler failure result is always silently dropped
`KafkaApplication` is a plain `MiddlewareMultiApplication<KafkaEvent, KafkaContext>` fan-out over
every record in the batch — there is **no `Options` class**, no `RaiseOnFailureStatus`, and **no
`ReportBatchItemFailures`/`KafkaBatchResponse` implementation**, even though the Lambda event
source mapping for MSK/self-managed Kafka supports partial-batch-failure reporting at the AWS
level (the same mechanism `Benzene.Aws.Lambda.Sqs`/`Benzene.Aws.Lambda.DynamoDb` use). If a
handler returns a non-exception failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`),
nothing in this package inspects it — that record (and the rest of a successfully-processed batch)
is always treated as fully consumed; there is no way to have just that record retried. Only an
unhandled exception propagating out of the batch's `Task.WhenAll` fails the whole Lambda
invocation, which replays the *entire* batch from the last committed offset (subject to the event
source mapping's `BisectBatchOnFunctionError`/retry/DLQ configuration) — there is no per-record
outcome, partial or otherwise. (An earlier version of this doc claimed "failed records can trigger
batch reprocessing" as if that were a routine per-record mechanism — it isn't: it's an all-or-
nothing consequence of an *unhandled exception*, not of a returned failure result, and there is
currently no supported way to get per-record `BatchItemFailures` reporting out of this package.)

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
- Processes Kafka records in batches
- Kafka headers mapped to Benzene headers
- Topic name extracted from Kafka record
- Partition and offset available in context
- An unhandled exception fails the whole invocation and replays the entire batch (see the
  "Unsafe by default" section above) — a returned failure result does not, and there is no
  per-record retry/reprocessing mechanism
- Message key available in context
- Supports both MSK and self-managed Kafka

## Tests
- `test/Benzene.Core.Test/Aws/Kafka/KafkaGettersTest.cs` — the three getters directly: body
  (UTF-8 decode), topic, and headers (empty-header-batch case returning `{}`, plus the positive
  case asserting both the decoded header value and the injected `topic` entry).
- `test/Benzene.Core.Test/Aws/Kafka/KafkaMessagePipelineTest.cs` — end-to-end sends (JSON, XML,
  unprocessable-entity, Kafka-in/SNS-out fan-out), `CanHandle` routing (`Send_FromStream` for the
  matching `aws:kafka` event source, `Send_FromStream_NonKafkaEvent_DoesNotRoute` for a mismatched
  one), and `MultiplePartitionsAndRecords_AllRecordsAreProcessed` pinning down
  `KafkaApplication`'s per-topic-partition flattening (`Records.Values.SelectMany(...)`) across
  more than one partition and more than one record per partition.
