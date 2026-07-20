# Benzene.Grpc.AspNet

## What this package does
Hosts `Benzene.Grpc` on ASP.NET Core: registers `Grpc.AspNetCore` and `BenzeneInterceptor`, wires a
Benzene middleware pipeline into the gRPC request path, and optionally exposes grpc.health.v1 and
grpc.reflection.v1alpha. This is the package a gRPC server application actually references.

## Key types/interfaces

### Registration and hosting
- `ServiceCollectionExtensions.AddBenzeneGrpc` - two overloads: `Action<GrpcServiceOptions>?` (simple
  case) and `Action<BenzeneGrpcOptions>` (opts into health checks/reflection); both register
  `services.AddGrpc()` with `BenzeneInterceptor` and a shared `IGrpcMethodHandlerFactoryAccessor`
  instance
- `BenzeneExtensions.UseGrpc` - builds the `IMiddlewarePipelineBuilder<GrpcContext>`, runs the
  caller's configuration action, and populates the accessor with the built
  `IGrpcMethodHandlerFactory`; the `IBenzeneApplicationBuilder` overload no-ops on any platform other
  than ASP.NET Core (pattern-matches `IAspApplicationBuilder`), matching `UseHttp`'s shape
- `BenzeneGrpcOptions` - `EnableHealthChecks`, `EnableReflection` (both off by default),
  `ConfigureGrpc`, and `LivenessCheckTypes`/`ReadinessCheckTypes` (the liveness/readiness split, below)

### Health checks and reflection (D8, opt-in)
- `BenzeneHealthCheckBridge` - an ASP.NET Core `IHealthCheck` that aggregates registered
  `Benzene.HealthChecks.Core.IHealthCheck`s (unhealthy if any failed, degraded if any warned, healthy
  otherwise); registered as the `"benzene"` check when `EnableHealthChecks` is set. It can be scoped to
  a subset of checks by `Type` (an optional `includeTypes` ctor arg) to back a named grpc.health.v1 service.
- **Liveness/readiness split**: set `BenzeneGrpcOptions.LivenessCheckTypes`/`ReadinessCheckTypes` (lists
  of check `Type`s) to publish named grpc.health.v1 services `"liveness"`/`"readiness"` that report only
  those checks, alongside the overall `""` service - the gRPC analogue of HTTP `UseLivenessCheck`/
  `UseReadinessCheck`. Wired via `Grpc.AspNetCore.HealthChecks` service-mapping (`o.Services.Map(name, …)`
  by tag). When neither is set, behaviour is unchanged: one aggregate check on the overall `""` service.
  So a gRPC liveness probe can be scoped to cheap/local checks, avoiding the restart-on-flaky-dependency
  anti-pattern.
- `GrpcEndpointExtensions.MapBenzeneGrpcHealthService`/`MapBenzeneGrpcReflectionService` - thin
  wrappers over the standard `Grpc.AspNetCore.HealthChecks`/`Grpc.AspNetCore.Server.Reflection`
  endpoint extensions

## When to use this package
- Hosting any Benzene.Grpc service on ASP.NET Core - this is the normal entry point, not
  `Benzene.Grpc` directly

## Dependencies on other Benzene packages
- **Benzene.AspNet.Core** - `IAspApplicationBuilder`/`AspApplicationBuilder`, the reopened-container
  pattern `UseGrpc` relies on (see `Benzene.AspNet.Core`'s own docs for why gRPC interceptors, unlike
  Benzene's own HTTP entry point, are activated by ASP.NET Core's own per-request DI rather than the
  pipeline-building container - that's exactly what the accessor instance bridges)
- **Benzene.Core.MessageHandlers** - `AddGrpcMessageHandlers`
- **Benzene.Grpc** - the actual routing/dispatch this package hosts
- **Benzene.HealthChecks.Core** - the lightweight health check interfaces `BenzeneHealthCheckBridge`
  aggregates (deliberately not the full `Benzene.HealthChecks` pipeline-middleware package)

## Important conventions
- `AddBenzeneGrpc` must run in `ConfigureServices`; `UseGrpc` must run in `Configure`, after
  `UseRouting` and typically alongside `MapGrpcService<TService>()`
- Handler types routed by `[GrpcMethod]` must be registered eagerly in `ConfigureServices` (e.g. via
  `AddMessageHandlers`), not only inside `UseGrpc`'s configuration action - the interceptor's route
  finder is built from ASP.NET Core's own per-request DI, which only sees `ConfigureServices`
  registrations
- Health checks and reflection are off by default; each is real extra surface area a service should
  opt into deliberately
