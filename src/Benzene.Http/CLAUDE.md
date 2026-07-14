# Benzene.Http

## What this package does
Provides HTTP abstractions and utilities for building HTTP-based Benzene applications. Includes HTTP context, request/response handling, routing, endpoint discovery, CORS middleware, and status code mapping. Foundation for all HTTP transport adapters (AspNet.Core, Lambda API Gateway, etc.).

## Key types/interfaces

### HTTP Context & Request
- `IHttpContext` - HTTP request/response context
- `HttpRequest` - Simple HTTP request representation
- `IHttpRequestAdapter<TContext>` - Adapts transport context to HTTP

### Routing
- `IHttpEndpointDefinition` - Metadata for HTTP endpoints
- `IHttpEndpointFinder` - Discovers HTTP endpoints
- `IListHttpEndpointFinder` - List-based endpoint finder
- `IRouteFinder` - Finds routes by HTTP method and path
- `HttpEndpointDefinition` - Concrete endpoint definition
- `HttpTopicRoute` - Route with topic/message name
- `UrlMatcher` - Matches URLs with route patterns
- `RouteFinder` - Default route finder implementation

### Endpoint Discovery
- `ReflectionHttpEndpointFinder` - Discovers endpoints via reflection
- `CacheHttpEndpointFinder` - Caches discovered endpoints
- `CompositeHttpEndpointFinder` - Combines multiple finders
- `DependencyHttpEndpointFinder` - Discovers from DI container
- `ListHttpEndpointFinder` - Manual endpoint list

### Status Codes
- `IHttpStatusCodeMapper` - Maps result status to HTTP status codes
- `DefaultHttpStatusCodeMapper` - Default HTTP status mapping
- `HttpStatusCodeResponseHandler<TContext>` - Sets HTTP status on response

### Headers
- `IHttpHeaderMappings` - Maps custom headers to constants
- `HttpHeaderMappings` - Header mapping implementation
- `DefaultHttpHeaderMappings` - Default header mappings

### CORS
Behavior tracks the CORS spec the same way `Microsoft.AspNetCore.Cors` does (exact origin
matching, credential-safe wildcards, preflight header validation, Vary caching hint).
- `CorsSettings` - CORS configuration:
  - `AllowedDomains` - full URLs match exactly on scheme+host+port; bare hostnames match host only
    (any scheme/port); `"*"` allows any origin
  - `AllowedHeaders` - explicit list, or `"*"` to allow any header (`AllowAnyHeader()` equivalent)
  - `ExposedHeaders` - sets `Access-Control-Expose-Headers` on actual (non-preflight) responses
  - `AllowCredentials`/`MaxAgeSeconds` - control `Access-Control-Allow-Credentials`/`Access-Control-Max-Age`
- `CorsMiddleware<TContext>` - CORS middleware; sets `Vary: Origin` on every response it processes;
  rejects (no CORS headers at all) a preflight whose `Access-Control-Request-Headers` includes a
  header outside a non-wildcard `AllowedHeaders` list
- `CorsOriginChecker` - Validates CORS origins; always echoes the actual `Origin` value (never a
  literal `"*"`), which is what makes wildcard + credentials safe to combine

### Other
- `HttpEndpointAttribute` - Marks HTTP endpoints
- `HttpRegistrations` - Registers HTTP services
- Extension methods in `Extensions.cs`, `Cors/Extensions.cs`

## When to use this package
- When building HTTP-based applications with Benzene
- When implementing custom HTTP transport adapters
- When you need HTTP routing and endpoint discovery
- Typically used via AspNet.Core, Lambda API Gateway, or SelfHost.Http

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Uses core abstractions
- **Benzene.Abstractions.Middleware** - Uses middleware abstractions
- **Benzene.Abstractions.MessageHandlers** - Uses message handler abstractions
- **Benzene.Core** - Uses core utilities
- **Benzene.Core.Middleware** - Uses middleware infrastructure
- **Benzene.Core.MessageHandlers** - Uses message handler infrastructure

## Important conventions
- HTTP endpoints marked with `[HttpEndpoint("GET", "/path")]` attribute
- Route patterns support path parameters: `/users/{id}`
- URL matching is case-insensitive by default
- CORS middleware should be added early in pipeline
- Default status code mapping follows REST conventions
- Endpoint discovery is cached for performance
- Multiple finders can be composed for flexibility
- Header mappings allow transport-agnostic header handling
