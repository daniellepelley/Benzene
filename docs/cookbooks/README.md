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
- [S3 Event Processing](../getting-started-aws.md) - React to S3 object creation/deletion via `Benzene.Aws.Lambda.S3` (see the "S3" section)
- API Gateway Custom Authorizers *(planned - `Benzene.Aws.Lambda.ApiGateway`'s `ApiGatewayCustomAuthorizer` support exists in source but isn't cookbook-documented yet)*
- [Lambda Cold Start Optimization](lambda-cold-start-optimization.md) - Reduce cold start times

### Azure
- [Service Bus Message Handling](service-bus-handling.md) - Process Service Bus messages with Benzene
- [Event Hub Stream Processing](event-hub-processing.md) - Handle high-throughput Event Hub streams, batching, and checkpointing boundaries
- Managed Identity for Azure Resources *(planned)*

### Validation & Error Handling
- [FluentValidation with Custom Rules](fluentvalidation-custom-rules.md) - Cross-field rules, async DB validation, and per-rule status overrides
- [Global Error Handling](global-error-handling.md) - Centralized error handling and logging
- [Request/Response Transformations](../middleware.md) - Transform messages in the pipeline via `.Convert()` / `ContextConverterMiddleware`

### Data & Persistence
- [Entity Framework Core Integration](entity-framework-integration.md) - Database access patterns
- [Redis Caching](redis-caching.md) - Cache handler responses with Redis
- Outbox Pattern *(planned)*

### Testing
- [Integration Testing Lambda Functions](testing-lambda-functions.md) - Test Lambda handlers end-to-end
- [Mocking External Dependencies](mocking-dependencies.md) - Test message handlers in isolation
- [Contract Testing (schema compatibility)](contract-testing.md) - Catch breaking contract changes before they reach consumers, at runtime (schema-hash drift check) and in CI (backward-compatibility gate)
- [Contract Testing (conformance)](../specification/porting-guide.md) - Verify message contracts between services via the conformance-testing approach

### Cross-Cutting Concerns
- [Request Correlation Across Services](request-correlation.md) - Track requests through distributed systems
- Rate Limiting *(planned)*
- Circuit Breaker Pattern *(planned - `Benzene.Resilience` currently implements retry-with-backoff only; see [Resilience](../resilience.md))*
- [Request Authentication & Authorization](auth-patterns.md) - OAuth2 bearer token (JWT) validation, Basic auth, and scope-based authorization for services with no security-terminating gateway in front of them

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

Want to contribute a cookbook? Check the [Contributing](../../README.md#contributing) section of the main README and submit a PR following the cookbook template.
