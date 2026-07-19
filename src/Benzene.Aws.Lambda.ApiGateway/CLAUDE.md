# Benzene.Aws.Lambda.ApiGateway

## What this package does
AWS API Gateway Lambda integration for Benzene. Wraps API Gateway proxy events (payload format
version 1.0 only — the `APIGatewayProxyRequest`/`APIGatewayProxyResponse` types from
`Amazon.Lambda.APIGatewayEvents`) and runs them through Benzene's HTTP middleware pipeline. Provides
the request/response context, HTTP request/response adapters, message getters, health-check
endpoints, and API Gateway custom authorizer handling.

CORS is **not** in this package — it lives in the shared `Benzene.Http/Cors/CorsMiddleware.cs` and
is added to any HTTP-shaped pipeline via `Benzene.Http`'s CORS extension methods.

## Key types/interfaces

### Application & Handler
- `ApiGatewayApplication` - API Gateway application
- `ApiGatewayLambdaHandler` - Lambda function handler for API Gateway

### Context & Adapters
- `ApiGatewayContext` - Implements `IHttpContext`; wraps the raw `APIGatewayProxyRequest` and the
  `APIGatewayProxyResponse` being built (v1.0 payload format only)
- `ApiGatewayHttpRequestAdapter` - Maps the context onto a Benzene `HttpRequest` (path, method, and
  the single-value `Headers` dictionary only — see "Important conventions")
- `ApiGatewayResponseAdapter` - Writes status code, headers, content-type and body onto the
  `APIGatewayProxyResponse`

### Message Handling
- `ApiGatewayMessageBodyGetter` - Returns `APIGatewayProxyRequest.Body` verbatim (no base64 decode)
- `ApiGatewayMessageHeadersGetter` - Extracts the single-value `Headers`, applying `IHttpHeaderMappings`
- `ApiGatewayMessageTopicGetter` - Resolves the topic by matching HTTP method + path via `IRouteFinder`
- `ApiGatewayMessageVersionGetter` - Resolves the payload schema version from the matched route's
  `version` route parameter, falling back to the header list
- `ApiGatewayRequestEnricher` - Enriches the request object with query-string parameters, path
  parameters, and mapped headers
- `ApiGatewayMessageHandlerResultSetter` - Runs the `IResponseHandler` chain to set the result
  on the response

### Custom Authorizer (`ApiGatewayCustomAuthorizer/`)
- `ApiGatewayCustomAuthorizerApplication` - Custom authorizer app
- `ApiGatewayCustomAuthorizerLambdaHandler` - Routes `APIGatewayCustomAuthorizerRequest` invocations
  (claims payloads with a non-empty API ID)
- `ApiGatewayCustomAuthorizerContext` - Wraps `APIGatewayCustomAuthorizerRequest` and the
  `APIGatewayCustomAuthorizerResponse` (an IAM policy document) to return

### Health checks (`DependencyInjectionExtensions.cs`)
- `.UseHealthCheck(method, path, ...)` - matches on raw HTTP method + path, verified via
  `ApiGatewayLivenessReadinessTest`/`ApiGatewayMessagePipelineTest`
- `.UseLivenessCheck(...)` / `.UseReadinessCheck(...)` - Kubernetes-style convenience wrappers,
  defaulting to `GET /livez`/`GET /readyz` (path overridable); see `docs/kubernetes-health-checks.md`.
  Note: this package has its own local `Constants` class (with its own `DefaultHealthCheckTopic`,
  a separate string constant from `Benzene.HealthChecks.Constants.DefaultHealthCheckTopic` that
  happens to share the same value) - the liveness/readiness topic constants are NOT duplicated here,
  they're referenced as `Benzene.HealthChecks.Constants.DefaultLivenessTopic`/`DefaultReadinessTopic`
  explicitly qualified to avoid the two `Constants` types colliding.

### Other
- `ApiGatewayRegistrations` - Registers API Gateway services
- Extension methods for configuration
- Log context extensions

## When to use this package
- When building API Gateway Lambda functions with Benzene
- For REST APIs, or HTTP APIs configured with payload format version 1.0, on AWS Lambda
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
- **Payload format version 1.0 only.** Only `APIGatewayProxyRequest` is wrapped — this covers REST
  APIs and HTTP APIs configured for payload format 1.0. There is deliberately no v2 support
  (`APIGatewayHttpApiV2ProxyRequest` is not referenced anywhere in the package); `ApiGatewayLambdaHandler`
  claims an invocation only when the deserialized payload has a non-null `HttpMethod` (a v1.0-shaped field).
- **Single-value headers and query strings only — multi-value entries are dropped.** Query-string
  parameters, path parameters, and headers *are* surfaced onto the request object (by
  `ApiGatewayRequestEnricher`, which reads `QueryStringParameters`/`PathParameters`/`Headers`), but only
  their **single-value** forms: `MultiValueHeaders` and `MultiValueQueryStringParameters` are not read, so
  a header or query key sent more than once collapses to the single value AWS puts in the single-value map
  (the last value). `ApiGatewayHttpRequestAdapter` likewise maps path, method, and single-value `Headers`
  only. Full multi-value support is a post-1.0 enhancement (it needs the multi-value maps threaded through
  the single-value getter/enricher contracts).
- **Base64-encoded request bodies are decoded.** When API Gateway sets `IsBase64Encoded` (binary media
  types, or any payload it can't treat as text), `ApiGatewayMessageBodyGetter` base64-decodes the body back
  to its real text (UTF-8) so the handler never sees base64; a normal text body is returned verbatim.
- CORS is not handled here — use the shared `Benzene.Http` `CorsMiddleware`.
- Custom authorizers return an `APIGatewayCustomAuthorizerResponse` (IAM policy document).
- Topic/route resolution is by matching HTTP method + path against registered routes via `IRouteFinder`.
