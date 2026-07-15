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
- **No test coverage exists for `BenzeneHttpWorker`/`SelfHostHttpContext` itself** (topic-based or
  HTTP-path-based) - `SelfHostHttpContext` wraps a real `System.Net.HttpListenerContext`, a sealed
  BCL type with no public constructor, so unit testing it requires a real listener bound to a real
  port rather than a fake context object; this has never been set up in this repo. The new
  liveness/readiness methods here are thin wrappers over the pre-existing (also untested)
  `.UseHealthCheck(method, path, builder)` overload - verified by clean compile and by direct
  reading, not by a runtime test. The concurrency/drain behavior `BenzeneHttpWorker` relies on
  *is* unit-tested in isolation, though, via `Benzene.SelfHost.BoundedConcurrentDispatcher<T>`'s
  own test suite (`test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`).

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
