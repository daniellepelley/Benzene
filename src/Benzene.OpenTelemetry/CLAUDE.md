# Benzene.OpenTelemetry

## What this package does
OpenTelemetry integration for Benzene. Provides middleware for distributed tracing, metrics, and logging using OpenTelemetry standards. Enables observability across multiple platforms (AWS, Azure, Google Cloud, on-premises).

## Key types/interfaces

### OpenTelemetry Integration
- Tracing middleware
- Activity/span creation
- Metrics collection
- Distributed tracing context propagation

## When to use this package
- When you need vendor-agnostic observability
- For distributed tracing across platforms
- When using OpenTelemetry collectors
- For Kubernetes/cloud-native deployments
- Modern alternative to AWS X-Ray/Azure Monitor

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **OpenTelemetry** - OpenTelemetry SDK

## Important conventions
- Add OpenTelemetry middleware early in pipeline
- Spans created per request
- Child spans for nested operations
- Attributes for contextual metadata
- Works with any OpenTelemetry backend
- Standards-based for portability
