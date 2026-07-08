# Benzene.Datadog

## What this package does
Datadog APM integration for Benzene. Provides middleware for distributed tracing with Datadog, enabling performance monitoring, error tracking, and request flow visualization in Datadog APM.

## Key types/interfaces

### Datadog Integration
- Datadog tracing middleware
- Span creation and tagging
- Distributed tracing context
- Performance metrics

## When to use this package
- When using Datadog for APM
- For distributed tracing with Datadog
- When you need Datadog-specific features
- For organizations standardized on Datadog

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Datadog.Trace** - Datadog APM SDK

## Important conventions
- Add Datadog middleware early in pipeline
- Spans created per request
- Tags for request metadata
- Integrates with Datadog Agent
- Service names configured in middleware
