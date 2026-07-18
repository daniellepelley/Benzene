# Benzene.Aws.Lambda.Sns

## What this package does
AWS SNS Lambda integration for Benzene. Processes SNS events from Lambda triggers through Benzene's message handler pipeline. Handles SNS message structure, attributes, and notification processing.

## ⚠️ Unsafe-by-default: a handler failure result is silently dropped, not retried
**`SnsOptions.RaiseOnFailureStatus` defaults to `false`.** If your message handler returns a
non-exception failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`), `SnsApplication`
still reports the Lambda invocation as **successful** — SNS sees a delivered notification and
**never retries it**. The message is gone; nothing dead-letters it. This is different from a
thrown exception, which *does* cascade out of the invocation by default and lets SNS's
subscription-level retry/redrive policy do its job.

If you want at-least-once/retry-on-failure semantics for business-logic failures too, set:
```csharp
app.UseAwsLambda(eventPipeline => eventPipeline
    .UseSns(snsApp => snsApp.UseMessageHandlers(),
        options => options.RaiseOnFailureStatus = true));
```
This escalates a failure result into a thrown `SnsMessageProcessingException`, so SNS retries it
the same way it would an unhandled exception. **If you turn this on, your handler must be
idempotent** — a retried SNS delivery re-runs the same handler with the same message, and SNS
itself provides no dedup — see the idempotency row in
[Capability Matrix](../../docs/capability-matrix.md) and [Idempotency](../../docs/cookbooks/idempotency.md).
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
- `SnsMessageTopicGetter` - Extracts the topic from the `topic` **message attribute** (not the topic ARN)
- `SnsMessageHandlerResultSetter` - Sets result on context
- `SnsUtils` - Helper for reading string message attributes
- `SnsMessageProcessingException` - Thrown when `SnsOptions.RaiseOnFailureStatus` escalates a failure result

### Other
- `SnsRegistrations` - Registers SNS services
- Extension methods for configuration

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
- No response expected - fire-and-forget pattern
- Exception/failure-status handling is configurable via `SnsOptions` (`UseSns(..., configure)`),
  defaulting to today's implicit behavior: a handler exception cascades out of the invocation
  (SNS's own subscription retry policy applies) and a non-exception failure result is silently
  accepted (no retry). Set `SnsOptions.CatchExceptions = true` to catch and log exceptions instead
  of cascading them; set `SnsOptions.RaiseOnFailureStatus = true` to escalate a non-exception
  failure result into a thrown `SnsMessageProcessingException` so SNS retries it too. Both default
  to `false` (purely additive/opt-in) - see `docs/cookbooks/sns-fan-out.md`
