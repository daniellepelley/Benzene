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
- `AwsLambdaHealthCheck` — verifies a function; reports `HealthCheckDependency` (`Kind = "Lambda"`).
  Default `HealthCheckMode.Reachability` is a **non-destructive** read-only `GetFunctionConfiguration`
  call (`Type = "Lambda"`); `HealthCheckMode.Active` really invokes the function with a `ping`
  (`Type = "Lambda.Active"`, side-effecting). See `HealthCheckMode` in `Benzene.HealthChecks.Core`.
  Failures are classified via `HealthCheckError.Classify` (§3.9, reversed): an authorization/permission
  failure (403) is a **persistent `Failed`**, surfacing as unhealthy rather than being softened to a
  Warning (a deterministic misconfiguration that won't self-heal); the SDK `ErrorCode`/`StatusCode` are
  surfaced in `Data`, never the message.
  - **No auto-wiring — explicit-only (by design).** Unlike SQS/SNS/EventBridge, the Lambda client is a
    **dynamic-target invoker**: `.UseAwsLambda<T>()` carries no function name (the target is supplied
    per-invocation), so there is no fixed dependency to auto-register a check for at config time. Register
    it yourself with `AddLambdaHealthCheck(name)` where you know the function. If a fixed-target Lambda
    client is ever introduced, auto-wire it there. See `work/client-health-checks-remaining-designs.md` §5.
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
