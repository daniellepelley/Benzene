# Benzene.Aws.Lambda.Core

## What this package does
Core AWS Lambda integration for Benzene. Provides base classes and abstractions for building Lambda functions with Benzene's middleware pipeline. Foundation for all AWS Lambda transport adapters (API Gateway, SQS, SNS, Kafka, EventBridge).

## Key types/interfaces

### Lambda Entry Points
- `IAwsLambdaEntryPoint` - Entry point abstraction
- `AwsLambdaEntryPoint` - Base entry point implementation
- `IAwsEntryPointBuilder` - Builds Lambda entry points
- `InlineAwsLambdaStartUp` - Inline startup configuration
- `AwsLambdaHost<TStartUp>` - Hosts a platform-neutral `BenzeneStartUp` as a Lambda entry point

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
