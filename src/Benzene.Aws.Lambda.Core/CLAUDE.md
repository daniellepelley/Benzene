# Benzene.Aws.Lambda.Core

## What this package does
Core AWS Lambda integration for Benzene. Provides base classes and abstractions for building Lambda functions with Benzene's middleware pipeline. Foundation for all AWS Lambda transport adapters (API Gateway, SQS, SNS, Kafka, EventBridge).

## Key types/interfaces

### Lambda Entry Points
- `IAwsLambdaEntryPoint` - Entry point abstraction
- `AwsLambdaEntryPoint` - Base entry point implementation
- `IAwsEntryPointBuilder` - Builds Lambda entry points
- `InlineAwsLambdaStartUp` - Inline startup configuration
- `AwsLambdaHost<TStartUp>` - Hosts a platform-neutral `BenzeneStartUp` as a Lambda entry point.
  Exposes a `protected virtual Task OnInvocationCompleteAsync()` hook (default no-op) run in a `finally`
  after every invocation — override it for end-of-invocation work that must complete while the process
  is still running, most notably force-flushing a batched OpenTelemetry exporter before the Lambda
  environment freezes (a background export thread stops on freeze, so without a per-invocation flush the
  invocation's spans can be delayed to the next invocation or lost on scale-in). Core stays OTel-agnostic
  — the flush itself lives in the host that opts in; see `examples/AwsMesh/Shared` `TracingLambdaHost`.

### Context & Routing
- `AwsEventStreamContext` - AWS event stream context
- `AwsLambdaMiddlewareRouter` - Routes AWS events to pipelines

### BenzeneMessage Integration
- `BenzeneMessageLambdaHandler` - Lambda handler for BenzeneMessage
- Direct message handling for Lambda

### Other
- `AwsRegistrations` - Registers AWS Lambda services
- Extension methods for Lambda configuration
- Log context extensions for Lambda

## When to use this package
- When building any AWS Lambda function with Benzene
- As foundation for Lambda transport adapters
- When you need custom Lambda event processing
- Rarely used directly - use specific adapters (ApiGateway, Sqs, etc.)

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core** - Core utilities
- **Benzene.Core.Middleware** - Middleware implementations
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Amazon.Lambda.Core** - AWS Lambda runtime

## Important conventions
- Entry points implement IDisposable for Lambda lifecycle
- Startup class pattern for DI configuration
- Router dispatches to appropriate pipeline based on event type
- BenzeneMessage provides transport-agnostic Lambda handling
- AWS Lambda context passed through middleware pipeline
- Cold start optimization via startup caching
- Async/await used throughout
- `AwsLambdaMiddlewareRouter<TRequest>`'s serializer is shared (static) across router instances,
  because the pipeline constructs middleware fresh per invocation and System.Text.Json caches its
  reflection-built type metadata per `JsonSerializerOptions` instance — a per-instance serializer
  re-paid the full metadata build for `TRequest` (tens of ms for the large AWS event types) on every
  invocation. The `protected JsonSerializer` field is still per-instance assignable for overrides.
  Regression-guarded by `test/Benzene.Core.Test/Aws/AwsLambdaMiddlewareRouterSerializerTest.cs`.
  - The field is typed **`ILambdaSerializer`** (default: the reflection-based
    `DefaultLambdaJsonSerializer`) so a derived router can plug a **source-generated**
    `SourceGeneratorLambdaJsonSerializer<TContext>` for its event type — this removes even the *first*
    (cold-start) reflection metadata build, the bulk of the API-Gateway conversion cost in the X-Ray
    cold-start analysis. **Every event-source adapter now does this**, each via its own public
    `*JsonSerializerContext` registering the event type it deserializes plus, where it writes one, the
    response type it serializes: API Gateway v1 (`ApiGatewayJsonSerializerContext`), v2
    (`ApiGatewayV2JsonSerializerContext`) and custom authorizer
    (`ApiGatewayCustomAuthorizerJsonSerializerContext`) — kept as **separate** contexts because the v1/v2
    request types have nested types with the same simple name (a shared context trips `SYSLIB1031`) —
    plus SQS, SNS, S3, EventBridge, DynamoDB, Kinesis, Kafka, and the BenzeneMessage direct-invoke path
    (`Benzene.Aws.Lambda.Core.BenzeneMessage.BenzeneMessageJsonSerializerContext`). The batch adapters
    register their `*BatchResponse`; SNS/S3/EventBridge write no response so register only the event.
    `MapResponse<TResponse>` is generic (not `object`) so the response's static type reaches the
    serializer — required for source generation to resolve the right `JsonTypeInfo`, and behaviourally
    identical for the reflection path. One wrinkle: BenzeneMessage's pipeline returns the
    **`IBenzeneMessageResponse` interface** (its declared members match the concrete), so that context
    registers the *interface* — the static type the serializer is actually invoked with. Each adapter's
    handler exercises its source-gen serializer end-to-end in the existing `*MessagePipelineTest`s;
    round-trip equivalence (source-gen vs reflection) for the API Gateway v1 event is guarded by
    `test/Benzene.Core.Test/Aws/ApiGateway/ApiGatewaySourceGenSerializerTest.cs`.
