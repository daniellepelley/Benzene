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

## Failure handling: a returned failure result is escalated (safe by default; opt out via `S3Options`)
Safe-by-default (`S3Options.RaiseOnFailureStatus` defaults to `true`). If a handler returns a
non-exception failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`), `S3Application`
escalates it into a thrown `S3MessageProcessingException`, so the Lambda invocation fails and S3's
own async-invoke retry (2 automatic retries) + on-failure destination/DLQ take over — the S3 event
notification is redelivered, not silently settled (at-least-once). Because a retried S3 delivery
re-runs the handler with the same event, the handler must be idempotent. Set
`S3Options.RaiseOnFailureStatus = false` (via `UseS3(action, configure)`) for at-most-once, where a
failure result is accepted and the invocation is not retried — mirroring `Benzene.Aws.Lambda.Sns`.
`S3Options.CatchExceptions` (default `false`) conversely swallows/logs handler exceptions instead of
cascading them.

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
- **Bounded batch fan-out**: `UseS3(action, maxDegreeOfParallelism)` (and the `S3Application`
  constructor) optionally caps how many records run concurrently; `null` (the default) leaves the
  fan-out unbounded - the original behavior. Threaded straight into the base
  `MiddlewareMultiApplication`, which routes it through `Benzene.Core.Middleware`'s `BoundedFanOut`.
