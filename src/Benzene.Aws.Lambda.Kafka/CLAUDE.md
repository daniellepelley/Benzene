# Benzene.Aws.Lambda.Kafka

## What this package does
AWS Kafka Lambda integration for Benzene (MSK and self-managed Kafka). Processes Kafka events from Lambda event source mappings through Benzene's message handler pipeline. Handles batch processing, Kafka headers, and partition/offset management.

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
- `KafkaMessageMessageHandlerResultSetter` - Sets result on context

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
- Failed records can trigger batch reprocessing
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
