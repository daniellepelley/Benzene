# Benzene.Client.Http

## What this package does
An outbound HTTP transport for the Benzene client pipeline: converts an `IBenzeneClientContext<TRequest,
TResponse>` into an `HttpRequestMessage`, sends it with a supplied `HttpClient`, then deserializes the
`HttpResponseMessage` back into a typed `IBenzeneResult<TResponse>`. You supply the `HttpClient` — see
the [Capability Matrix](../../docs/capability-matrix.md)'s *Outbound HTTP* row for the
`IHttpClientFactory`/lifetime story (yours to own on this low-level path).

## Key types/interfaces
- `HttpSendMessageContext` - the pipeline context wrapping an `HttpRequestMessage Request` and a
  settable `HttpResponseMessage Response`.
- `HttpContextConverter<TRequest, TResponse>` - `IContextConverter` between the client context and
  `HttpSendMessageContext`. `CreateRequestAsync` builds the `HttpRequestMessage` from a `verb` + `path`
  (the `path` is used verbatim as the request `Uri` - there is **no** URL-template/path-parameter
  substitution), serializes the request body as `application/json` (UTF-8), and copies
  `contextIn.Request.Headers` onto the request. `MapResponseAsync` reads the response body and maps the
  `HttpStatusCode` to a Benzene result.
- `HttpClientMiddleware` - the transport step: its `HandleAsync` is just
  `context.Response = await _httpClient.SendAsync(context.Request)`. Nothing more (no retry, no header
  logic of its own).
- `Extensions` - `UseHttpClient(...)` (with a passed `HttpClient`, or resolving a DI-registered scoped
  `HttpClientMiddleware`), `Convert(...)`, and the `UseHttp<TRequest,TResponse>(verb, path, ...)` helpers
  that wire the converter + client middleware together.

## When to use this package
- When an outbound Benzene client route should call another service over HTTP with JSON.

## Deliberate boundaries (NOT shipped)
- **Takes a raw `HttpClient`** (constructor / `UseHttpClient(httpClient)`). There is **no**
  `IHttpClientFactory` integration or typed/named-client wiring in this package - lifecycle of the
  `HttpClient` is the caller's responsibility.
- **No URL routing or path-parameter binding.** The `path` you pass is the literal request URI.
- **No header propagation logic** in the middleware. The only headers sent are the ones already on
  `contextIn.Request.Headers`, copied over by the converter; correlation-id/trace-context propagation
  is separate outbound middleware in `Benzene.Clients` (`.UseCorrelationId()` / `.UseW3CTraceContext()`).

## Important conventions
- Bodies are JSON via the shared `JsonSerializer` unless you pass your own `ISerializer` to the converter.
- Async throughout (`SendAsync`, `ReadAsStringAsync`).

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - `IContextConverter`, client context contracts, `ISerializer`
- **Benzene.Clients** - client-context / outbound abstractions
- **Benzene.Core.Middleware** - `ContextConverterMiddleware`, pipeline building
