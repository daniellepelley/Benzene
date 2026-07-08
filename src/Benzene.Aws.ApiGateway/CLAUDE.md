# Benzene.Aws.ApiGateway

## What this package does
AWS API Gateway utilities for Benzene (outside Lambda context). Provides types and utilities for working with API Gateway events, responses, and configuration. Shared between Lambda and non-Lambda API Gateway integrations.

## Key types/interfaces

### API Gateway Utilities
- API Gateway event models
- Response builders
- Request/response transformations

## When to use this package
- When working with API Gateway outside Lambda
- As a shared dependency for API Gateway integrations
- Typically used transitively via Aws.Lambda.ApiGateway

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Http** - HTTP abstractions
- **Benzene.Aws.Core** - AWS core utilities

## Important conventions
- Provides API Gateway-specific models
- Shared between different API Gateway scenarios
- Lower-level than Aws.Lambda.ApiGateway
