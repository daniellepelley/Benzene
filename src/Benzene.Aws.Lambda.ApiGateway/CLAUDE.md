# Benzene.Aws.Lambda.ApiGateway

## What this package does
AWS API Gateway Lambda integration for Benzene. Processes API Gateway events (REST API and HTTP API) through Benzene's HTTP middleware pipeline. Provides context adapters, response builders, CORS support, and custom authorizer functionality.

## Key types/interfaces

### Application & Handler
- `ApiGatewayApplication` - API Gateway application
- `ApiGatewayLambdaHandler` - Lambda function handler for API Gateway

### Context & Adapters
- `ApiGatewayContext` - Implements `IHttpContext` for API Gateway
- `ApiGatewayHttpRequestAdapter` - Adapts API Gateway request
- `ApiGatewayResponseAdapter` - Builds API Gateway response

### Message Handling
- `ApiGatewayMessageBodyGetter` - Extracts body from API Gateway event
- `ApiGatewayMessageHeadersGetter` - Extracts headers
- `ApiGatewayMessageTopicGetter` - Extracts topic/route
- `ApiGatewayRequestEnricher` - Enriches requests with API Gateway data
- `ApiGatewayMessageMessageHandlerResultSetter` - Sets result on response

### Custom Authorizer
- `ApiGatewayCustomAuthorizerApplication` - Custom authorizer app
- `ApiGatewayCustomAuthorizerLambdaHandler` - Custom authorizer handler
- `ApiGatewayCustomAuthorizerContext` - Context for authorizers

### CORS
- `ApiGatewayContextCorsMiddleware` - CORS middleware for API Gateway
- CORS extension methods

### Other
- `ApiGatewayRegistrations` - Registers API Gateway services
- Extension methods for configuration
- Log context extensions

## When to use this package
- When building API Gateway Lambda functions with Benzene
- For REST APIs or HTTP APIs on AWS Lambda
- When you need HTTP endpoints in Lambda
- For custom authorizers with Benzene

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Benzene.Http** - HTTP abstractions
- **Benzene.Aws.Lambda.Core** - AWS Lambda core
- **Amazon.Lambda.APIGatewayEvents** - API Gateway event types

## Important conventions
- Supports both REST API and HTTP API (v1 and v2) events
- CORS must be configured in middleware for preflight requests
- Custom authorizers return IAM policy documents
- Request/response transformation handles API Gateway format
- Path parameters extracted into route values
- Query strings and headers mapped to HTTP abstractions
- Binary content supported via base64 encoding
- Multi-value headers and query strings supported
