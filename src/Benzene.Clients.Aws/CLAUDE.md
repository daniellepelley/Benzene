# Benzene.Clients.Aws

## What this package does
Outbound AWS client implementations for calling Benzene services (or plain AWS targets) from a Benzene
app. Covers AWS Lambda invocation, SQS send, SNS publish, EventBridge put-events, and Step Functions
start-execution, plus health checks for those services.

## Key types/interfaces

### Standalone message clients (each implements `IBenzeneMessageClient`)
- `AwsLambdaBenzeneMessageClient` (`Lambda/`) - invokes a Benzene Lambda, embedding request headers in
  its own `BenzeneMessageClientRequest` envelope
- `SqsBenzeneMessageClient` (`Sqs/`) - sends to an SQS queue
- `SnsBenzeneMessageClient` (`Sns/`) - publishes to an SNS topic
- `EventBridgeBenzeneMessageClient` (`EventBridge/`) - puts events on an EventBridge bus

### Pipeline middleware, send contexts, and context converters
- `AwsLambdaClientMiddleware` / `LambdaSendMessageContext` / `LambdaContextConverter<T>`
- `SqsClientMiddleware` / `SqsSendMessageContext` / `SqsContextConverter<T>` / `OutboundSqsContextConverter`
- `SnsClientMiddleware` / `SnsSendMessageContext` / `SnsContextConverter<T>` / `OutboundSnsContextConverter`
- `EventBridgeClientMiddleware` / `EventBridgeSendMessageContext` / `EventBridgeContextConverter<T>`
- `Extensions` per folder wire these in: `UseSqs`/`UseSns`/`UseAwsLambda`/`UseEventBridge` (both the
  `IBenzeneClientContext<T, Void>` overloads and, for SQS/SNS, `OutboundContext` overloads)

### Step Functions & health checks
- `IStepFunctionsClient` / `StepFunctionsClient` / `StepFunctionsClientFactory`
- `SqsHealthCheck` / `AwsLambdaHealthCheck` / `StepFunctionsHealthCheck`, registered via the top-level
  `Extensions.AddSqsHealthCheck` / `AddLambdaHealthCheck` / `AddStepFunctionHealthCheck`

## When to use this package
- When calling Benzene Lambda functions
- For publishing to Benzene SQS/SNS/EventBridge consumers
- For starting Step Functions executions
- For AWS-native service calls

## Dependencies on other Benzene packages
- **Benzene.Clients** - client / outbound-routing abstractions (`IBenzeneMessageClient`, `OutboundContext`)
- **Benzene.Core.Middleware** - middleware pipeline implementation
- **Benzene.HealthChecks.Core** - health check abstractions
- **Benzene.Results** - `IBenzeneResult` / `Void`
- **AWS SDK** - `AWSSDK.Lambda`, `AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`, `AWSSDK.EventBridge`,
  `AWSSDK.StepFunctions`

## Important conventions
- Uses AWS SDK clients
- IAM-based authentication
- Region configuration
- ARN-based addressing
- `SqsHealthCheck`/`StepFunctionsHealthCheck`/`AwsLambdaHealthCheck` (in `Sqs`/`StepFunctions`/`Lambda`)
  report a structured `HealthCheckDependency` (`Kind` = `"Queue"`/`"StateMachine"`/`"Lambda"`,
  `Name` = the queue URL/state machine ARN/Lambda function name) on every result they produce - see
  `Benzene.HealthChecks.Core`'s `IHealthCheckResult.Dependencies`
- `SqsContextConverter`/`SnsContextConverter` forward `IBenzeneClientRequest.Headers` onto real
  `MessageAttributes` so header-based decorators (correlation ID, W3C trace context) actually reach the
  wire. `SqsContextConverter` additionally sets a `topic` message attribute (the SQS consumer routes on
  it); `SnsContextConverter` does not — SNS routing is the topic ARN itself, so it forwards headers only.
- `LambdaContextConverter` (used by the lower-level `UseAwsLambda()` pipeline composition, not
  `AwsLambdaBenzeneMessageClient` directly) does NOT forward headers — a raw `InvokeRequest` has no
  header-like concept. `AwsLambdaBenzeneMessageClient` already forwards headers correctly by
  embedding them in its own `BenzeneMessageClientRequest` envelope.
- **Outbound routing (2026-07-17, Step 3 of `work/benzene-clients-redesign-plan.md`)**:
  `OutboundSqsContextConverter`/`OutboundSnsContextConverter` are the `Benzene.Clients.OutboundContext`
  counterparts of `SqsContextConverter<T>`/`SnsContextConverter<T>` - the `OutboundContext`
  overloads of `.UseSqs(queueUrl, ...)`/`.UseSns(topicArn, ...)` in `Sqs/Extensions.cs`/`Sns/Extensions.cs`
  use them to convert an `OutboundRoutingBuilder.Route(topic, pipeline => pipeline.UseSqs(...))` route
  onto the same `SqsClientMiddleware`/`SnsClientMiddleware` the older `IBenzeneClientContext<T,Void>`-typed
  `.UseSqs<T>(...)`/`.UseSns<T>(...)` overloads already use - forward per-call headers (from
  `IBenzeneMessageSender.SendAsync`'s `headers` parameter) onto message attributes exactly like the
  old converters do. Both response mappers hardcode `IBenzeneResult<Void>` - SQS/SNS have no
  request/response semantics beyond a send acknowledgement, so a topic routed through either must
  be sent via `SendAsync<TRequest,Void>`; any other `TResponse` compiles but throws
  `InvalidCastException` at runtime. **`.UseAwsLambda(...)` has no `OutboundContext` overload yet** -
  explicitly deferred, not forgotten; would follow the identical `OutboundAwsLambdaContextConverter`
  recipe whenever picked up.
- **Registering outbound clients**: there is no client-factory/`ClientsBuilder` layer in this package.
  Route outbound sends via `AddOutboundRouting(...)` + `.UseSqs(...)`/`.UseSns(...)` on an
  `OutboundRoutingBuilder.Route` pipeline (see `docs/migration-alpha-to-1.0.md`). The four standalone
  `IBenzeneMessageClient` implementations (`SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`,
  `AwsLambdaBenzeneMessageClient`, `EventBridgeBenzeneMessageClient`) can also be constructed and used
  directly.
