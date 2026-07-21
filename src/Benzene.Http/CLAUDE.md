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
- `UrlMatcher` - Matches URLs with route patterns (delegates to `CompiledRoutePath`)
- `CompiledRoutePath` (internal) - a route pattern with its per-segment split/regex parsing done
  once, up front, so matching an incoming path is cheap string comparisons rather than re-splitting
  the pattern and running `Regex.Split` over it per request
- `RouteFinder` - Default route finder implementation; compiles each route's method (lower-cased) and
  path (to a `CompiledRoutePath`) once at construction, and splits only the incoming path per request
  (once, not once per route), so the hot path does no per-route regex/splitting work

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

### BenzeneMessage over HTTP (`BenzeneMessage/`)
The HTTP equivalent of the direct AWS Lambda invoke path — same `UseBenzeneMessage` name and
overload shapes (inline `Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>>` or a shared
pre-built builder).
- `BenzeneMessageHttpMiddleware<TContext> where TContext : IHttpContext` - on POST to its path,
  deserializes a `BenzeneMessageRequest` envelope from the body (via `IMessageBodyGetter<TContext>`),
  runs it through a `BenzeneMessageApplication` (the `"benzene"` transport — routing, validation,
  middleware, handler), and writes the response envelope as `application/json` with the HTTP status
  mapped via `IHttpStatusCodeMapper`; anything else falls through to `next`. Same short-circuit
  shape as `CorsMiddleware`/`SpecUiMiddleware`, so it works on every HTTP transport. The envelope
  is always read/written with Benzene's default JSON serialization, independent of the app's own
  negotiated payload formats.
- `BenzeneMessageHttpOptions` - `Path` (default `/benzene-message`) and optional `TopicFilter`
  allowlist predicate (rejected topic → `NotFound` envelope, 404).
- `IBenzeneMessageHttpEndpointInfo` - registered by `UseBenzeneMessage` so the `benzene` spec
  builder (`Benzene.Schema.OpenApi`) can advertise the endpoint as the top-level `messageEndpoint`
  field, which the Spec UI's try-it panel feature-detects.
- **Security posture:** the endpoint exposes every routed topic (including ones with no HTTP
  mapping) — strictly opt-in, intended for local dev/admin environments; compose auth middleware
  in front and/or use `TopicFilter`. See `docs/payload-testing.md`.
- Tests: `test/Benzene.Core.Test/Http/BenzeneMessageHttpMiddlewareTest.cs` (unit, Moq'd adapters)
  and `BenzeneMessageHttpPipelineTest.cs` (end-to-end through an API Gateway test host).

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
  literal `"*"`), which is what makes a *specific* origin allow-list + credentials safe to combine.
  A wildcard (`"*"`) origin + credentials is refused, not reflected: when `AllowedDomains` contains
  `"*"` the middleware omits `Access-Control-Allow-Credentials` (matching ASP.NET Core, which throws
  on `AllowAnyOrigin()` + `AllowCredentials()`) so a credentialed response can't leak to any origin

### Request body buffering (`RequestBody/`)
Removes the sync-over-async block from the stream-reading HTTP body getters (ASP.NET Core, Azure
Functions ASP.NET, self-hosted HttpListener), which used to call `ReadToEndAsync().Result` /
`ReadToEnd()` and tie up a thread-pool thread per request.
- `HttpRequestBodyBuffer` - scoped, per-request holder for a body already read from the stream
  (`IsBuffered`/`Body`/`Set`). Same "scoped DI state, not context" pattern as `PresetTopicHolder`:
  the context type stays a pure message description, this per-request state lives in the scope.
- `IHttpRequestBodyReader<TContext>` - transport-specific async body reader; each stream-based
  transport's body getter implements it (`Task<string?> ReadBodyAsync(context)`).
- `BufferRequestBodyMiddleware<TContext>` - front-of-pipeline middleware that awaits
  `IHttpRequestBodyReader<TContext>.ReadBodyAsync` once and stores the result in the scoped buffer,
  so the synchronous `IMessageBodyGetter<TContext>.GetBody` serves it from memory (no thread block).
- `BufferRequestBodyExtensions.UseBufferedRequestBody<TContext>()` - wires the middleware. Each HTTP
  transport auto-adds it as the first middleware in its `UseHttp(...)`, so existing apps get the
  non-blocking read with no code change. If the middleware isn't wired, `GetBody` falls back to the
  original synchronous read, so the behavior is identical either way. Note: the body is read up front
  (before other pipeline middleware); the ASP.NET readers call `EnableBuffering()` so anything
  downstream can still re-read `Request.Body`.

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
