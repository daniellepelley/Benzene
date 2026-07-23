# Benzene Cookbooks

Practical recipes for common real-world scenarios using Benzene.

## What are Cookbooks?

Cookbooks are step-by-step guides that show you how to solve specific problems with Benzene. Each cookbook focuses on a single use case and provides complete, copy-pasteable code that you can adapt to your needs.

## Available Cookbooks

### Observability
- [Diagnosing Failures](../diagnosing-failures.md) - A message failed in production: the recommended middleware stack, the log fields you get, and how a failure shows up across logs, traces, and metrics
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
- [Deploying with the Serverless Framework](deploy-with-serverless-framework.md) - Deploy a Benzene Lambda via `serverless.yml` (for teams already on the Serverless Framework), and keep the one seam — `events:` ↔ `.UseXxx(...)` — in sync

### Azure
- [Service Bus Message Handling](service-bus-handling.md) - Process Service Bus messages with Benzene
- [Event Hub Stream Processing](event-hub-processing.md) - Handle high-throughput Event Hub streams, batching, and checkpointing boundaries
- [Cosmos DB Change Feed Processing](cosmos-change-feed-processing.md) - Consume a container's change feed as an ordered document stream
- [Managed Identity & RBAC for Azure Resources](managed-identity.md) - Run every Azure integration with no connection strings: `DefaultAzureCredential` wiring for the Service Bus/Event Hubs/Cosmos DB workers, identity-based connection settings for the Functions triggers, and the RBAC roles each one needs

### Validation & Error Handling
- [FluentValidation with Custom Rules](fluentvalidation-custom-rules.md) - Cross-field rules, async DB validation, and per-rule status overrides
- [Global Error Handling](global-error-handling.md) - Centralized error handling and logging
- [Request/Response Transformations](../middleware.md) - Transform messages in the pipeline via `.Convert()` / `ContextConverterMiddleware`

### Data & Persistence
- [Entity Framework Core Integration](entity-framework-integration.md) - Database access patterns
- [Redis Caching](redis-caching.md) - Cache handler responses with Redis
- [Transactional Outbox](transactional-outbox.md) - Publish a handler's event atomically with its DB write by swapping the `IResponseEventPublisher` behind `UseResponseEvents` for an outbox table + relay

### Configuration & Secrets
- [Secrets & Multi-Cloud Configuration](secrets-configuration.md) - A provider-agnostic `ISecretStore` seam (env vars, mounted files, composed, cached), startup fail-fast validation, and copy-paste Key Vault / AWS Secrets Manager / SSM adapters

### Testing
- [Integration Testing Lambda Functions](testing-lambda-functions.md) - Test Lambda handlers end-to-end
- [Mocking External Dependencies](mocking-dependencies.md) - Test message handlers in isolation
- [Contract Testing (schema compatibility)](contract-testing.md) - Catch breaking contract changes before they reach consumers, at runtime (schema-hash drift check) and in CI (backward-compatibility gate)
- [Schema Registry Integration](schema-registry.md) - Register event payload schemas centrally, frame messages with the Confluent wire format for cross-consumer interop, and gate deploys with a byte-identical compatibility check (structural evolution needs a real registry server or your own checker; with copy-paste Confluent / Azure adapters)
- [Contract Testing (conformance)](../specification/porting-guide.md) - Verify message contracts between services via the conformance-testing approach

### Orchestration
- [Sagas (distributed transactions that roll back cleanly)](sagas.md) - Run a multi-service operation as all-or-nothing: each step carries a compensation, and any failure rolls the whole thing back in reverse (LIFO) order, leaving no orphaned records
- [Response as Event](response-as-event.md) - Republish a request/response handler's response payload as a follow-up event on fire-and-forget transports (SQS `order:create` → broadcast `order:created`), declaratively via `UseResponseEvents`

### Cross-Cutting Concerns
- [Request Correlation Across Services](request-correlation.md) - Track requests through distributed systems
- [Idempotency (de-duplicating redelivered messages)](idempotency.md) - Ensure a handler's side effect runs at most once per message on at-least-once transports (SQS, Service Bus, Event Hubs, Kafka), with a pluggable store
- [Multi-Tenancy](multi-tenancy.md) - Attribute every request to a tenant and isolate its data/cache using a scoped `TenantHolder` set by a resolver middleware (claim / header / subdomain), with a "tenant required" guard
- Rate Limiting *(planned)*
- [Polly Resilience Pipelines (circuit breaker, timeout, hedging, fallback)](polly-resilience.md) - `Benzene.Resilience` implements retry-with-backoff only; the sibling `Benzene.Resilience.Polly` package runs your own Polly `ResiliencePipeline` as middleware (`.UseResiliencePipeline(...)`) for the full toolkit, and bridges a returned failure result to Polly's outcome model
- [Request Authentication & Authorization](auth-patterns.md) - OAuth2 bearer token (JWT) validation, Basic auth, and scope-based authorization for services with no security-terminating gateway in front of them
- [Bring Your Own DI Container](bring-your-own-di-container.md) - Use Lamar, DryIoc, Grace, or any other container behind Benzene with no Benzene-specific package, by plugging its `IServiceProvider` into the host (Benzene ships adapters only for Microsoft DI and Autofac)

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
