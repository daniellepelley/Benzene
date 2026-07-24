# Benzene.Client.Http

## What this package does
Outbound HTTP for the Benzene client, in **two flavours**:
1. **Plain REST** (the original) — converts an `IBenzeneClientContext<TRequest, TResponse>` into an
   `HttpRequestMessage` at a `verb`+`path`, sends it with a supplied `HttpClient`, and deserializes the
   `HttpResponseMessage` back into a typed `IBenzeneResult<TResponse>`. The raw message is the request body;
   the target is a normal REST route.
2. **BenzeneMessage envelope** (`HttpBenzeneMessageClient`, added 2026-07-22) — carries the transport-neutral
   envelope `{ topic, headers, body }` over HTTP to another Benzene service's BenzeneMessage endpoint (the
   serving side's `BenzeneMessageHttpMiddleware`, default path `/benzene-message`), and maps the returned
   `{ statusCode, headers, body }` envelope back to `IBenzeneResult<TResponse>`. This is the HTTP counterpart
   of the AWS Lambda invoke path (`AwsLambdaBenzeneMessageClient`) — **the topic travels inside the JSON body,
   so one endpoint serves every topic**, letting two Benzene containers exchange lightweight messages over
   ordinary HTTP without a per-route REST contract. See `work/lightweight-non-http-transport-design.md`
   (Option A / Phase 1).

You supply the `HttpClient` — see the [Capability Matrix](../../docs/capability-matrix.md)'s *Outbound HTTP*
row for the `IHttpClientFactory`/lifetime story (yours to own on both paths).

## The two flavours — which to use
- Use **plain REST** (`UseHttp(verb, path)`) to call a non-Benzene service, or a Benzene HTTP route with a
  fixed method+path contract, where the *message itself* is the HTTP body.
- Use the **envelope client** (`AddHttpBenzeneMessageClient(url)`) for **container-to-container Benzene
  messaging**: the receiving side routes on the envelope's topic exactly as it would for a queue or a Lambda
  invoke, so the same handlers serve it. The response envelope carries the authoritative Benzene status in its
  body (the target also maps that onto the HTTP status), so a mapped non-2xx like a 404 for `NotFound` is a
  normal result — the client reads and maps the envelope regardless of HTTP status and only surfaces a genuine
  transport error (connection failure / non-envelope body) as `ServiceUnavailable`. A `Void` (send-ack) caller
  maps the status only, without deserializing a payload body.

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
  that wire the converter + client middleware together (the **plain REST** flavour).
- `HttpBenzeneMessageClient` - `IBenzeneMessageClient`; POSTs the `{ topic, headers, body }` envelope to a
  configured URL and maps the `{ statusCode, headers, body }` response. Optional `ILogger`; observes the
  ambient cancellation token via `ICancellationTokenAccessor`. `url` is used verbatim as the request URI
  (absolute, or relative to the client's `BaseAddress`). The envelope wire shape is a small internal
  `BenzeneMessageEnvelope` (defined here so the package keeps **no** dependency on `Benzene.Clients.Aws.Lambda`,
  where the identically-shaped invoke-path `BenzeneMessageClientRequest` happens to live).
- `HttpBenzeneMessageHealthCheck` - `IHealthCheck`; non-destructive reachability that POSTs a
  `healthcheck`-topic envelope and treats a 2xx envelope response as healthy. Follows the shared §3.9 policy
  (reversed): a 401/403 permission response → a **persistent `Failed`** (surfaces as unhealthy even for the
  auto-wired dependency check rather than being softened to a Warning — a deterministic misconfiguration that
  won't self-heal), a transport exception → `Failed` via `HealthCheckError.Classify` (reports the exception
  **type**, never its message), any other mapped non-2xx → unhealthy. Reports a
  `HealthCheckDependency("Http", url)` with basic-auth userinfo stripped from the reported URL. `Type` =
  `"HttpBenzeneMessage"`.
- `Extensions.AddHttpBenzeneMessageClient(services, url, healthCheck = true)` - registers a scoped
  `HttpBenzeneMessageClient` (as itself and as `IBenzeneMessageClient`), resolving the `HttpClient`/logger/
  cancellation accessor from DI. **Auto-wires** the reachability check onto the **dependency category** (deep
  `healthcheck` layer only — never a Kubernetes probe, see `IDependencyHealthCheck`), deduped by
  `"HttpBenzeneMessage:{url}"`; pass `healthCheck: false` to opt out.
- `Extensions.AddHttpBenzeneMessageHealthCheck(builder, url, healthCheckTopic = "healthcheck")` - explicit
  health-check registration for a target reached through other wiring.
- **Requires an `HttpClient` in DI** for the envelope client/health check (same as `Benzene.HealthChecks.Http`):
  register one via `AddHttpClient()`/`IHttpClientFactory` or `AddSingleton<HttpClient>()`.

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
- Bodies are JSON via the shared `JsonSerializer` unless you pass your own `ISerializer` to the
  converter's `(verb, path, serializer)` constructor. (The old bare `(ISerializer)` constructor was
  removed — it left the verb/path unset and would `NullReferenceException` on the first request.)
- Async throughout (`SendAsync`, `ReadAsStringAsync`).

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - `IContextConverter`, client context contracts, `ISerializer`
- **Benzene.Clients** - client-context / outbound abstractions
- **Benzene.Core.Middleware** - `ContextConverterMiddleware`, pipeline building
