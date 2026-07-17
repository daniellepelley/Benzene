# Benzene.Clients.Aws

## What this package does
AWS client implementations for calling Benzene services in AWS. Provides clients for Lambda, SQS, SNS, and other AWS services that host Benzene applications.

## Key types/interfaces

### AWS Clients
- Lambda invocation client
- SQS message client
- SNS publish client
- AWS service integration

## When to use this package
- When calling Benzene Lambda functions
- For publishing to Benzene SQS consumers
- For SNS-based service communication
- For AWS-native service calls

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Clients** - Client abstractions
- **Benzene.Aws.Core** - AWS core utilities
- **AWS SDK** - AWS service clients

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
  `MessageAttributes` (alongside the `topic` attribute) so header-based decorators (correlation ID,
  W3C trace context) actually reach the wire
- `LambdaContextConverter` (used by the lower-level `UseAwsLambda()` pipeline composition, not the
  `AwsLambdaBenzeneMessageClient`/`CreateAwsLambdaBenzeneMessageClient()` sugar) does NOT forward
  headers — a raw `InvokeRequest` has no header-like concept. `AwsLambdaBenzeneMessageClient` already
  forwards headers correctly by embedding them in its own `BenzeneMessageClientRequest` envelope.
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
