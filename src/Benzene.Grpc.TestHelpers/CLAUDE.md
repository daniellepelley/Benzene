# Benzene.Grpc.TestHelpers

## What this package does
Test infrastructure for Benzene.Grpc applications: an in-memory `TestServer`-backed host built from a
`BenzeneStartUp`, plus a hand-rolled `ServerCallContext` for unit tests that don't need a live host.
User-facing, like the other `*.TestHelpers` packages - referenced from test projects, not shipped in
production code.

## Key types/interfaces

- `GrpcTestHostBuilderExtensions.BuildGrpcHost<TStartUp>` - extends `Benzene.Testing`'s
  `BenzeneTestHostBuilder<TStartUp>`; runs the StartUp's `ConfigureServices`/`Configure` against a real
  ASP.NET Core `TestServer` (via `HostBuilder().ConfigureWebHost(...).UseTestServer()`), given an
  `Action<IEndpointRouteBuilder>` to map the gRPC service(s) under test
- `GrpcTestHost` - the built host; `CreateChannel()` returns a `GrpcChannel` wired directly to the
  in-memory `TestServer`, for constructing a generated client stub against
- `TestServerCallContext` - a minimal `ServerCallContext` subclass for handler/pipeline-level unit
  tests that don't need a full host. Only the members `Benzene.Grpc` actually reads (`Method`,
  `Deadline`, `RequestHeaders`, `CancellationToken`, `ResponseTrailers`,
  `WriteResponseHeadersAsync`) are meaningfully implemented; anything else throws if touched.
  `Create(...)` takes optional method/headers/cancellation token/deadline overrides.

## When to use this package
- Unit-testing a `GrpcMethodHandler`/pipeline directly against a hand-rolled call context
  (`TestServerCallContext`)
- Integration-testing a full Benzene.Grpc application, including `BenzeneInterceptor` routing, over a
  real generated client and an in-memory host (`GrpcTestHost`)

## Dependencies on other Benzene packages
- **Benzene.AspNet.Core** - `AspApplicationBuilder`, to run `BenzeneStartUp.Configure` against the
  `TestServer`'s `IApplicationBuilder` exactly as production ASP.NET Core hosting does
- **Benzene.Core.MessageHandlers** - `AddBenzene`
- **Benzene.Grpc** - the routing/dispatch types these tests exercise (`BenzeneInterceptor`,
  `GrpcMethodHandler`)
- **Benzene.Grpc.AspNet** - `AddBenzeneGrpc`/`UseGrpc`, which `GrpcTestHost` exercises unchanged
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`
- **Benzene.Testing** - `BenzeneTestHostBuilder<TStartUp>`, which `BuildGrpcHost` extends

## Important conventions
- `BuildGrpcHost` copies every `ServiceDescriptor` the StartUp's `ConfigureServices` registered onto
  the `TestServer`'s own service collection, then adds routing on top - the StartUp doesn't need to
  call `AddRouting()` itself
- `Grpc.Core.Testing` is deliberately not a dependency anywhere in the Benzene.Grpc family;
  `TestServerCallContext` and the fakes it's used alongside are hand-rolled instead
