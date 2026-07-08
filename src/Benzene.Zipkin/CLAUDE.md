# Benzene.Zipkin

## What this package does
Zipkin distributed tracing integration for Benzene. Provides middleware for tracing requests with Zipkin, enabling request flow visualization and performance analysis in Zipkin UI.

## Key types/interfaces

### Zipkin Integration
- Zipkin tracing middleware
- Span creation
- Distributed tracing context
- Zipkin annotations

## When to use this package
- When using Zipkin for distributed tracing
- For organizations using Zipkin
- Alternative to AWS X-Ray in on-premises environments
- For OpenZipkin ecosystem integration

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations

## Important conventions
- Add Zipkin middleware early in pipeline
- Spans created per request
- Annotations for events
- Requires Zipkin server endpoint configuration
