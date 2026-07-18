# Benzene.Clients.Aws.Lambda

## What this package does
Outbound AWS Lambda client for a Benzene app: invoke a Benzene Lambda function (or any Lambda), plus
a Lambda health check. Pins **only** `AWSSDK.Lambda` — it no longer depends on the SQS client (the
old `nameof(SqsClientMiddleware)` cross-reference in `AwsLambdaClientMiddleware` was a copy-paste bug,
fixed during the Tier 2.1 split).

## Key types
- `AwsLambdaBenzeneMessageClient` — `IBenzeneMessageClient`; invokes a Benzene Lambda, embedding
  request headers in its own `BenzeneMessageClientRequest` envelope.
- `AwsLambdaClient` / `IAwsLambdaClient` — lower-level invoke wrapper.
- `AwsLambdaClientMiddleware` / `LambdaSendMessageContext` — terminal invoke middleware and context.
- `LambdaContextConverter<T>` (in `SqsContextConverter.cs` — historically misnamed file) —
  `IBenzeneClientContext<T, Void>` → invoke context, used by `UseAwsLambda()`.
- `AwsLambdaHealthCheck` — pings a function; reports `HealthCheckDependency` (`Kind = "Lambda"`).
- `LocalAwsLambdaClientFactory` — builds an `IAmazonLambda` from a local AWS profile for dev/test.
- `Extensions` — `UseAwsLambdaClient`, `UseAwsLambda<T>`, and **`AddLambdaHealthCheck`**.

## Conventions
- `AwsLambdaBenzeneMessageClient` forwards headers correctly (they go in the
  `BenzeneMessageClientRequest` envelope it invokes with).
- `LambdaContextConverter` (used by the lower-level `UseAwsLambda()` composition, not the message
  client) does **not** forward headers — a raw `InvokeRequest` has no header concept, so a decorator
  like `WithW3CTraceContext()` has no effect on a pipeline built that way.
- **No `OutboundContext` overload of `.UseAwsLambda(...)` yet** — deliberately deferred, not
  forgotten; it would follow the same `Outbound…ContextConverter` recipe as SQS/SNS when picked up.

## Dependencies
`AWSSDK.Lambda`; Benzene `Clients`, `Core.Middleware`, `HealthChecks.Core`, `Results`.
