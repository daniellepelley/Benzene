# Benzene.SelfHost.Http

## What this package does
Provides HTTP server capabilities for self-hosted Benzene applications. Enables running HTTP endpoints without ASP.NET Core or IIS. Built on HttpListener for lightweight HTTP hosting in console apps or Windows services. This is one of the "self-hosted worker" startup modes documented in `docs/hosting.md` - Benzene itself owns the accept loop and the process here, unlike ASP.NET Core (embedded in Kestrel, which owns the process instead).

## Key types/interfaces

### HTTP Self-Hosting
- `BenzeneHttpWorker : IBenzeneWorker` - runs an `HttpListener.GetContextAsync()` accept loop on a
  background task and dispatches each `HttpListenerContext` through
  `Benzene.SelfHost.BoundedConcurrentDispatcher<T>` (see that package's `CLAUDE.md`) instead of a
  raw semaphore - no ordering key (requests round-robin across dispatcher lanes). `StartAsync`
  kicks off the loop and returns immediately (correct `IHostedService` semantics - it does not
  block until cancellation, which the old implementation did); `StopAsync` signals its own
  `CancellationTokenSource` (which unblocks the loop's pending `GetContextAsync()` via
  `.WaitAsync(token)`, since `HttpListener`'s async API predates `CancellationToken` support), then
  awaits the loop's graceful drain and listener close.
- `BenzeneHttpConfig` - `Url`, `ConcurrentRequests` (max concurrent request handlers), `DrainTimeout`
  (default 30s - how long `StopAsync` waits for in-flight requests before abandoning them).
- HTTP context adapter for HttpListener
- HTTP server lifecycle management

### Health checks (`Extensions.cs`)
- `.UseHealthCheck(method, path, ...)` - matches on raw HTTP method + path (before topic resolution),
  same shape as `Benzene.Aws.Lambda.ApiGateway`'s equivalent
- `.UseLivenessCheck(...)` / `.UseReadinessCheck(...)` - Kubernetes-style convenience wrappers,
  defaulting to `GET /livez`/`GET /readyz` (path overridable); see `docs/kubernetes-health-checks.md`
- `HttpListenerMessageHandlerResultSetter` - the `IMessageHandlerResultSetter<SelfHostHttpContext>`
  every response (health check or routed handler) is written through; runs the registered
  `IResponseHandler<SelfHostHttpContext>` chain via `ResponseMessageMessageHandlerResultSetterBase`,
  same pattern as `AspMessageMessageHandlerResultSetter`/`ApiGatewayMessageMessageHandlerResultSetter`.
  **Fixed real bugs, found by writing the package's first real end-to-end test** (previously misnamed
  `KafkaMessageHandlerResultSetter`, apparently copy-pasted from `Benzene.Kafka.Core`): it unconditionally
  forced `Response.StatusCode = 200` regardless of the actual result and never invoked
  `IResponseHandlerContainer`/`HttpContextResponseAdapter.FinalizeAsync` at all, so response bodies were
  never written and `HttpListenerResponse` was never closed. `AddHttp()` now also calls `AddContextItems()`
  (registers `IResponseHandlerContainer<>`, `IResponsePayloadMapper<>`, `IMessageGetter<>`, etc. as open
  generics) - previously only reachable via `.UseMessageHandlers()`/`.AddMessageHandlers()`, which isn't
  guaranteed for an app that only wires health checks via `UseHttp()`. A second, stacked bug only became
  reachable once responses started actually finalizing: `HttpContextResponseAdapter.FinalizeAsync` writes
  the body then closes the response without ever setting `ContentLength64`/`SendChunked` - .NET's
  cross-platform managed `HttpListener` (unlike the old Windows http.sys-backed one) doesn't infer
  Content-Length from what was written, so a keep-alive client had no signal for where the body ends and
  would hang until its own timeout. Both are fixed now.
- Now covered end to end by `test/Benzene.Core.Test/SelfHost/Http/BenzeneHttpWorkerTest.cs` - binds a
  real `HttpListener` to a free loopback port via `BenzeneHttpWorker` and drives it with a real
  `HttpClient` (`SelfHostHttpContext` wraps a sealed, constructor-less `HttpListenerContext`, so a fake
  context object isn't an option here). Covers: a matched liveness check returning 200, an unmatched
  path falling through to `MessageRouter`'s "missing topic" validation error (non-200, proving the
  status-code bug above is actually fixed), and `StopAsync` actually closing the listener (a further
  request fails to connect rather than hanging). The concurrency/drain behavior `BenzeneHttpWorker`
  relies on is separately unit-tested via `Benzene.SelfHost.BoundedConcurrentDispatcher<T>`'s own test
  suite (`test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`).

## When to use this package
- When you need HTTP endpoints in console apps
- For integration testing with real HTTP without ASP.NET Core
- For lightweight microservices with minimal dependencies
- When deploying to environments without web server

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Benzene.Http** - HTTP abstractions
- **Benzene.SelfHost** - Self-hosting infrastructure

## Important conventions
- Uses System.Net.HttpListener under the hood
- Requires admin privileges for port binding on Windows
- Suitable for development and testing
- Consider ASP.NET Core for production HTTP workloads
- Good for scenarios where IIS/Kestrel is not available
