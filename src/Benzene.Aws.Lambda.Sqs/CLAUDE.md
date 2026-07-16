# Benzene.Aws.Lambda.Sqs

## What this package does
AWS SQS Lambda integration for Benzene. Processes SQS events from Lambda triggers through Benzene's message handler pipeline. Handles batch processing, message attributes, and SQS-specific error handling.

## Key types/interfaces

### Application & Handler
- `SqsApplication` - SQS application for Lambda
- `SqsLambdaHandler` - Lambda function handler for SQS

### Context
- `SqsContext` - Context for SQS message processing

### Message Handling
- `SqsMessageBodyGetter` - Extracts message body from SQS event
- `SqsMessageHeadersGetter` - Extracts message attributes as headers
- `SqsMessageTopicGetter` - Extracts topic from the `topic` message attribute
- `SqsMessageMessageHandlerResultSetter` - Sets result on context
- Preset topic override - if the queue's producer never sets a `topic` message attribute (e.g. a
  raw SQS send, not a Benzene client), call `.UsePresetTopic("some-topic")` before
  `.UseMessageHandlers()` in that queue's pipeline (`Benzene.Core.MessageHandlers`) to route every
  message on it to a fixed topic instead. This is scoped DI state
  (`Benzene.Core.MessageHandlers.PresetTopicHolder`), not a capability of `SqsMessageContext`
  itself - the context stays a plain description of the SQS record, per this repo's context-purity
  convention (`Benzene.Abstractions.Middleware/CLAUDE.md`)

### Other
- `SqsRegistrations` - Registers SQS services
- Extension methods for configuration

## When to use this package
- When building Lambda functions triggered by SQS
- For queue-based message processing with Benzene
- When you need asynchronous command/event handling
- For decoupled microservices communicating via SQS

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.Aws.Lambda.Core** - AWS Lambda core
- **Amazon.Lambda.SQSEvents** - SQS event types

## Important conventions
- Processes SQS messages in batches
- Message attributes mapped to headers
- Topic determined from the `topic` message attribute by default, or a fixed preset topic per
  queue if configured via `.UsePresetTopic(...)` (see "Message Handling" above)
- Failed messages can be retried via SQS dead-letter queue
- Partial batch failures supported
- Message body deserialized to request object
- Receipt handle available in context for manual acknowledgment
