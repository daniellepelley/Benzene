# Benzene.Aws.Lambda.Sqs

## What this package does
AWS SQS Lambda integration for Benzene. Processes SQS events from Lambda triggers through Benzene's message handler pipeline. Handles batch processing, message attributes, and SQS-specific error handling.

## Key types/interfaces

### Application & Handler
- `SqsApplication` - SQS application for Lambda
- `SqsLambdaHandler` - Lambda function handler for SQS

### Context
- `SqsMessageContext` - Context for a single record within an SQS batch event; exposes both the full
  `SQSEvent` and the specific `SQSEvent.SQSMessage`, plus a nullable `IsSuccessful` outcome flag

### Message Handling
- `SqsMessageBodyGetter` - Extracts message body from SQS event
- `SqsMessageHeadersGetter` - Extracts message attributes as headers
- `SqsMessageTopicGetter` - Extracts topic from the `topic` message attribute. The attribute key is a
  configurable default, not hard-coded: `new SqsMessageTopicGetter(topicAttributeKey)`, or via
  `.AddSqs(topicAttributeKey)` / `.UseSqs(..., topicAttributeKey: "x")` — keep it in sync with the
  producer's key
- `SqsMessageHandlerResultSetter` - Sets result on context
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

## W3C trace context and invocationId (release plan Tier 3.5)
`.UseW3CTraceContext<SqsMessageContext>()` works: `SqsMessageHeadersGetter` already read real
message attributes, so a `traceparent` set by an upstream egress client round-trips into the
pipeline's root `Activity`. Separately, `UseSqs(...)` now auto-wires `UseBenzeneInvocation()` (in
`BenzeneInvocationExtensions.cs`) as the first middleware in the SQS pipeline, so `IBenzeneInvocation`
resolves inside each record's dispatch (`InvocationId` = the record's SQS `MessageId`) - previously
this threw/silently came back `null` (via `UseBenzeneEnrichment`'s `TryGetService`), because each
record is dispatched through its own DI scope, disconnected from whatever the outer Lambda
invocation populated. No application code changes needed for either fix.

## Important conventions
- Processes SQS messages in batches
- Message attributes mapped to headers
- Topic determined from the `topic` message attribute by default (the attribute key is overridable —
  see `SqsMessageTopicGetter` above), or a fixed preset topic per queue if configured via
  `.UsePresetTopic(...)` (see "Message Handling" above)
- Failed messages can be retried via SQS dead-letter queue
- Partial batch failures supported by default (`SqsBatchFailureMode.PartialBatchFailure` - only
  the messages that actually failed are reported via `SQSBatchResponse.BatchItemFailures`, so SQS
  redrives just those). `UseSqs(..., configure)` takes an optional `SqsOptions` delegate; set
  `BatchFailureMode = SqsBatchFailureMode.FailWholeBatch` to instead throw `SqsBatchProcessingException`
  on any failure, failing the whole invocation so SQS retries every message in the batch. Purely
  additive/opt-in - see `docs/cookbooks/handling-sqs-failures.md`
- Message body deserialized to request object
- The raw `SQSEvent.SQSMessage` (including its receipt handle) is reachable via the context, but this
  package does not delete messages itself — acknowledgment is handled by Lambda's event source mapping,
  driven by the `SQSBatchResponse` this package returns (see partial batch failures above)
