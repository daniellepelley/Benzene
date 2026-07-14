# Benzene.SelfHost.Http

## What this package does
Provides HTTP server capabilities for self-hosted Benzene applications. Enables running HTTP endpoints without ASP.NET Core or IIS. Built on HttpListener for lightweight HTTP hosting in console apps or Windows services.

## Key types/interfaces

### HTTP Self-Hosting
- HTTP listener integration
- HTTP context adapter for HttpListener
- HTTP server lifecycle management

### Health checks (`Extensions.cs`)
- `.UseHealthCheck(method, path, ...)` - matches on raw HTTP method + path (before topic resolution),
  same shape as `Benzene.Aws.Lambda.ApiGateway`'s equivalent
- `.UseLivenessCheck(...)` / `.UseReadinessCheck(...)` - Kubernetes-style convenience wrappers,
  defaulting to `GET /livez`/`GET /readyz` (path overridable); see `docs/kubernetes-health-checks.md`
- **No test coverage exists for this package** (topic-based or HTTP-path-based) - `SelfHostHttpContext`
  wraps a real `System.Net.HttpListenerContext`, a sealed BCL type with no public constructor, so unit
  testing it requires a real listener bound to a real port rather than a fake context object; this has
  never been set up in this repo. The new liveness/readiness methods here are thin wrappers over the
  pre-existing (also untested) `.UseHealthCheck(method, path, builder)` overload - verified by clean
  compile and by direct reading, not by a runtime test.

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
