# Benzene.Aws.Lambda.Sns

## What this package does
AWS SNS Lambda integration for Benzene. Processes SNS events from Lambda triggers through Benzene's message handler pipeline. Handles SNS message structure, attributes, and notification processing.

## Settlement: safe-by-default (a handler failure result is retried, not dropped)
**`SnsOptions.RaiseOnFailureStatus` defaults to `true`** (flipped from `false`, 2026-07-21 — see
`work/settlement-contract-1.0.md`). A handler that returns a non-exception failure result (e.g.
`BenzeneResult.ServiceUnavailable(...)`) is escalated into a thrown `SnsMessageProcessingException`,
so the Lambda invocation fails and SNS's subscription-level retry/redrive policy redelivers it (and
eventually dead-letters it) — the same at-least-once treatment a thrown exception already got. This
means **your handler must be idempotent**: a retried SNS delivery re-runs it with the same message,
and SNS provides no dedup — see the idempotency row in
[Capability Matrix](../../docs/capability-matrix.md) and [Idempotency](../../docs/cookbooks/idempotency.md).

To opt back into the old at-most-once behavior (a failure result is accepted, no retry):
```csharp
app.UseAwsLambda(eventPipeline => eventPipeline
    .UseSns(snsApp => snsApp.UseMessageHandlers(),
        options => options.RaiseOnFailureStatus = false));
```
Full detail (including the interaction with `CatchExceptions`): `docs/cookbooks/sns-fan-out.md`
§"Configuring exception and retry behavior with `SnsOptions`" and `docs/message-result.md` §"AWS SNS".

## Key types/interfaces

### Application & Handler
- `SnsApplication` - SNS application for Lambda
- `SnsLambdaHandler` - Lambda function handler for SNS

### Context
- `SnsRecordContext` - Context for a single record within an SNS batch event (`IHasMessageResult`);
  exposes both the full `SNSEvent` and the specific `SNSEvent.SNSRecord`

### Message Handling
- `SnsMessageBodyGetter` - Returns `SnsRecord.Sns.Message` (the SNS message body) verbatim
- `SnsMessageHeadersGetter` - Extracts the SNS message attributes as headers
- `SnsMessageTopicGetter` - Extracts the topic from the `topic` **message attribute** (not the topic
  ARN). The attribute key is a configurable default, not hard-coded:
  `new SnsMessageTopicGetter(topicAttributeKey)`, or via `.AddSns(topicAttributeKey)` /
  `.UseSns(..., topicAttributeKey: "x")` — keep it in sync with the producer's key
- `SnsMessageHandlerResultSetter` - Sets result on context
- `SnsUtils` - Helper for reading string message attributes
- `SnsMessageProcessingException` - Thrown when `SnsOptions.RaiseOnFailureStatus` escalates a failure result

### Other
- `SnsRegistrations` - Registers SNS services
- Extension methods for configuration

## W3C trace context and invocationId (release plan Tier 3.5)
`.UseW3CTraceContext<SnsRecordContext>()` works: `SnsMessageHeadersGetter` already read real SNS
message attributes. Separately, `UseSns(...)` now auto-wires `UseBenzeneInvocation()`
(`BenzeneInvocationExtensions.cs`) as the first middleware in the SNS pipeline, so
`IBenzeneInvocation` resolves inside each record's dispatch (`InvocationId` = the record's SNS
`MessageId`) - previously this threw/silently came back `null`, because each record is dispatched
through its own DI scope, disconnected from whatever the outer Lambda invocation populated. No
application code changes needed for either fix.

## When to use this package
- When building Lambda functions triggered by SNS
- For pub/sub event handling with Benzene
- When you need fan-out message delivery
- For event-driven architectures using SNS

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.Aws.Lambda.Core** - AWS Lambda core
- **Amazon.Lambda.SNSEvents** - SNS event types

## Important conventions
- Processes each record in an `SNSEvent` batch (fan-out, one context/handler per record)
- Message attributes mapped to headers
- Topic determined from the `topic` message attribute (which a Benzene SNS client sets); a raw SNS
  publish that omits it yields a null topic. The topic is **not** derived from the SNS topic ARN.
- The message body is `SnsRecord.Sns.Message` as-is — the package does not unwrap a Benzene envelope
- The raw `SNSEvent.SNSRecord` (subject, message ID, timestamp, etc.) is reachable via the context
- **Bounded batch fan-out** (`SnsOptions.MaxDegreeOfParallelism`): defaults to `null` (unbounded -
  every record starts at once, the original behavior). Set a positive value to cap how many records
  run concurrently. Purely additive/opt-in; routed through `Benzene.Core.Middleware`'s `BoundedFanOut`.
- No response expected - fire-and-forget pattern
- Exception/failure-status handling is configurable via `SnsOptions` (`UseSns(..., configure)`).
  A handler exception cascades out of the invocation (SNS's own subscription retry policy applies);
  a non-exception failure result is **escalated** into a thrown `SnsMessageProcessingException` so
  SNS retries it too (`RaiseOnFailureStatus` defaults to `true` - see "Settlement" above). Set
  `SnsOptions.CatchExceptions = true` to catch and log exceptions instead of cascading them; set
  `SnsOptions.RaiseOnFailureStatus = false` for at-most-once (a failure result is accepted, no
  retry) - see `docs/cookbooks/sns-fan-out.md`
