# Benzene Cookbooks

Practical recipes for common real-world scenarios using Benzene.

## What are Cookbooks?

Cookbooks are step-by-step guides that show you how to solve specific problems with Benzene. Each cookbook focuses on a single use case and provides complete, copy-pasteable code that you can adapt to your needs.

## Available Cookbooks

### Observability
- [Logging to Application Insights](logging-application-insights.md) - Send structured logs to Azure Application Insights
- [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md) - Set up end-to-end tracing across services
- [Custom Metrics with OpenTelemetry](custom-metrics-opentelemetry.md) - Track business and performance metrics
- [Structured Logging with Serilog](structured-logging-serilog.md) - Rich structured logging with Serilog

### AWS
- [Handling SQS Message Failures](handling-sqs-failures.md) - Implement retry and DLQ patterns for SQS
- [SNS Fan-Out Pattern](sns-fan-out.md) - Broadcast events to multiple Lambda functions
- [S3 Event Processing](s3-event-processing.md) - React to S3 object creation/deletion
- [API Gateway Custom Authorizers](api-gateway-authorizers.md) - Secure your API with custom auth
- [Lambda Cold Start Optimization](lambda-cold-start-optimization.md) - Reduce cold start times

### Azure
- [Service Bus Message Handling](service-bus-handling.md) - Process Service Bus messages with Benzene
- [Event Hub Stream Processing](event-hub-processing.md) - Handle high-throughput event streams
- [Managed Identity for Azure Resources](managed-identity.md) - Secure access to Azure services

### Validation & Error Handling
- [FluentValidation with Custom Rules](fluentvalidation-custom-rules.md) - Complex validation scenarios
- [Global Error Handling](global-error-handling.md) - Centralized error handling and logging
- [Request/Response Transformations](request-response-transforms.md) - Transform messages in the pipeline

### Data & Persistence
- [Entity Framework Core Integration](entity-framework-integration.md) - Database access patterns
- [Redis Caching](redis-caching.md) - Implement response caching with Redis
- [Outbox Pattern](outbox-pattern.md) - Reliable message publishing with transactional outbox

### Testing
- [Integration Testing Lambda Functions](testing-lambda-functions.md) - Test Lambda handlers end-to-end
- [Mocking External Dependencies](mocking-dependencies.md) - Test message handlers in isolation
- [Contract Testing](contract-testing.md) - Verify message contracts between services

### Cross-Cutting Concerns
- [Request Correlation Across Services](request-correlation.md) - Track requests through distributed systems
- [Rate Limiting](rate-limiting.md) - Protect your services from overload
- [Circuit Breaker Pattern](circuit-breaker.md) - Handle downstream failures gracefully
- [Request Authentication & Authorization](auth-patterns.md) - Common auth patterns

## Cookbook Structure

Each cookbook follows this structure:

1. **Problem Statement** - What you're trying to achieve
2. **Prerequisites** - What you need before starting
3. **Step-by-Step Implementation** - Detailed walkthrough with code
4. **Testing** - How to verify it works
5. **Troubleshooting** - Common issues and solutions
6. **Variations** - Alternative approaches or extensions
7. **Further Reading** - Related docs and resources

## Request a Cookbook

Don't see a cookbook for your use case? [Open an issue](https://github.com/daniellepelley/Benzene/issues/new) describing:
- The problem you're trying to solve
- The AWS/Azure services or patterns involved
- Any specific constraints or requirements

## Contributing

Want to contribute a cookbook? Check our [contribution guidelines](../../CONTRIBUTING.md) and submit a PR following the cookbook template.
