# Benzene.Aws.XRay

## What this package does
AWS X-Ray distributed tracing integration for Benzene. Provides middleware for tracing requests through Benzene pipelines, creating X-Ray segments and subsegments, and capturing metadata for distributed tracing in AWS.

## Key types/interfaces

### X-Ray Middleware
- X-Ray tracing middleware
- Segment and subsegment creation
- Metadata and annotation capture

## When to use this package
- When you need distributed tracing in AWS
- For Lambda functions that need X-Ray instrumentation
- When debugging performance issues across services
- For request flow visualization in AWS X-Ray console

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **AWSXRayRecorder** - AWS X-Ray SDK

## Important conventions
- Add X-Ray middleware early in pipeline
- Segments created automatically per request
- Subsegments for downstream calls
- Metadata captured for debugging
- Annotations for filtering in X-Ray console
- Works with AWS Lambda automatic instrumentation
