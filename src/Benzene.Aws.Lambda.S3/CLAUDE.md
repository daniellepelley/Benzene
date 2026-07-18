# Benzene.Aws.Lambda.S3

## What this package does
AWS S3 event notification Lambda integration for Benzene. Processes S3 event
notifications (object created, removed, etc.) from Lambda triggers through Benzene's
middleware pipeline as a fire-and-forget, no-response transport.

> **Note:** This package was previously named `Benzene.Aws.Lambda.EventBridge`
> (assembly/namespace `Benzene.Aws.EventBridge`). It never contained EventBridge/
> CloudWatch Events code — every type here has always handled S3 event notifications
> via `Amazon.Lambda.S3Events`. It was renamed to `Benzene.Aws.Lambda.S3` to match what
> it actually does. Real EventBridge support now lives in the separate
> `Benzene.Aws.Lambda.EventBridge` package.

## ⚠️ Unsafe by default, and there is no opt-out: a handler failure result is always silently dropped
`S3Application` is a plain `MiddlewareMultiApplication<S3Event, S3RecordContext>` fan-out — there
is **no `Options` class** and no equivalent of `Benzene.Aws.Lambda.Sns`'s `RaiseOnFailureStatus`.
If a handler returns a non-exception failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`),
nothing in this package inspects it — the Lambda invocation always reports success, so the S3
event notification is considered delivered and is never retried. Only an unhandled exception
propagating out of the pipeline fails the invocation, which is what lets Lambda's own async-invoke
retry (2 automatic retries) and configured on-failure destination/DLQ take over — S3 event
notifications invoke Lambda asynchronously, so this is governed entirely by the function's own
`MaximumRetryAttempts`/destination configuration, not by anything in this package. If failure
results need to be retried, have the handler throw for failures that should retry.

## Key types/interfaces

### Application & Handler
- `S3Application` - Maps each record in an `S3Event` batch to an `S3RecordContext` and
  runs them through the middleware pipeline, tagging the transport as `"s3"`
- `S3LambdaHandler` - Routes AWS Lambda invocations whose payload deserializes into an
  `S3Event` (matched via `Records[0].EventSource == "aws:s3"`) to `S3Application`

### Context
- `S3RecordContext` - Context for a single record within an S3 event notification
  batch; exposes both the full `S3Event` batch and the specific
  `S3Event.S3EventNotificationRecord`

### Registration
- `S3Registrations` - Declares the `.AddS3()` registration for `RegistrationCheck`'s
  missing-registration diagnostics
- `DependencyInjectionExtensions.AddS3` - Registers the `ITransportInfo` for `"s3"`
- `Extensions.UseS3` - Adds S3 handling to an `AwsEventStreamContext` pipeline

## When to use this package
- When building Lambda functions triggered by S3 event notifications (object created,
  removed, restored, etc.)
- For processing pipelines driven by S3 bucket activity (e.g. image processing,
  file ingestion)

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware pipeline implementation
- **Benzene.Aws.Lambda.Core** - AWS Lambda core (`AwsEventStreamContext`,
  `AwsLambdaMiddlewareRouter`)
- **Amazon.Lambda.S3Events** - S3 event types

## Important conventions
- No response expected — this is a fire-and-forget pattern, consistent with SNS/Kafka
- `CanHandle` only matches invocations where the first record's `EventSource` is
  `"aws:s3"`; otherwise the router defers to the next middleware
- Transport tagged as `"s3"` for the duration of processing each batch
