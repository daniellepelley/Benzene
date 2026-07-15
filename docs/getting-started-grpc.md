# gRPC Setup

`Benzene.Grpc` lets a `Grpc.AspNetCore` service's methods — unary, server-streaming,
client-streaming, and bidirectional — be implemented by ordinary Benzene message handlers instead
of hand-written service method bodies, wired into the same platform-neutral
`BenzeneStartUp`/`IBenzeneApplicationBuilder` model as HTTP, AWS Lambda, and Azure Functions (see
[Unified Hosting Model](hosting)). This guide walks through the whole surface: routing, both
handler styles (protobuf-direct and POCO), all four RPC shapes, metadata, status codes,
cancellation, health checks/reflection, and the outbound client — matching
[`examples/Grpc`](../examples/Grpc), the fully worked reference for this package.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Familiarity with [gRPC on ASP.NET Core](https://learn.microsoft.com/aspnet/core/grpc) and
  protobuf service definitions — this guide assumes you already know how `.proto` files, generated
  service base classes, and `Grpc.AspNetCore` normally fit together, and focuses on where Benzene
  slots in

## 1. Create the project

```bash
mkdir MyGrpcService && cd MyGrpcService
dotnet new grpc -f net10.0
```

This scaffolds a standard ASP.NET Core gRPC service (`Grpc.AspNetCore`, a sample `.proto`, and a
generated service class) — Benzene adds to it rather than replacing it.

## 2. Install the NuGet packages

```bash
dotnet add package Benzene.Grpc.AspNet --prerelease
dotnet add package Benzene.Microsoft.Dependencies --prerelease
```

`Benzene.Grpc.AspNet` pulls in `Benzene.Grpc` and `Grpc.AspNetCore` transitively — you don't need
to reference either directly for a hosted service. Add `Benzene.Grpc.Client` separately if this
service also calls other gRPC services (see [step 11](#11-calling-other-grpc-services)), and
`Benzene.Grpc.TestHelpers` in your test project (see [step 12](#12-testing)).

## 3. Define your `.proto` and generated service

All four RPC shapes, so there's one example of each to build handlers against:

```proto
syntax = "proto3";

option csharp_namespace = "MyGrpcService";

package greet;

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
  rpc SayHelloServerStream (HelloRequest) returns (stream HelloReply);
  rpc SayHelloClientStream (stream HelloRequest) returns (HelloReply);
  rpc SayHelloBidiStream (stream HelloRequest) returns (stream HelloReply);
}

message HelloRequest {
  string name = 1;
}

message HelloReply {
  string message = 1;
}
```

```xml
<ItemGroup>
  <Protobuf Include="Protos\greet.proto" GrpcServices="Server" />
</ItemGroup>
```

You still need a real service class deriving from the generated `Greeter.GreeterBase` —
`app.MapGrpcService<GreeterService>()` requires one to exist so gRPC's own routing/reflection has
somewhere to point. You do **not** need to override every method: any method left unoverridden
returns `Unimplemented` by default, which is exactly what you want once `BenzeneInterceptor` claims
it via a matching `[GrpcMethod]`-tagged handler (step 4) — that override's body would never run for
that method anyway. Only override methods you intend to keep as native gRPC code with no Benzene
handler:

```csharp
public class GreeterService : Greeter.GreeterBase
{
    // No overrides needed here if every method has a matching [GrpcMethod] handler.
}
```

## 4. Define message handlers

Business logic lives in a message handler, tagged with `[GrpcMethod]` giving the **full gRPC
method route** (`/<package>.<Service>/<Method>`, exactly as it appears in the generated client) —
this is what `BenzeneInterceptor` matches against the incoming call to decide whether to redirect
it to your handler instead of the generated service class — plus `[Message("topic")]`, which is
how the middleware pipeline routes internally (nothing outside this package reads it as a gRPC
concept).

### Style 1: protobuf-direct (zero-copy)

Declare the generated protobuf types directly and there's no serialization step at all:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Results;

[GrpcMethod("/greet.Greeter/SayHello")]
[Message("say_hello")]
public class SayHelloMessageHandler : IMessageHandler<HelloRequest, HelloReply>
{
    public Task<IBenzeneResult<HelloReply>> HandleAsync(HelloRequest request)
    {
        return BenzeneResult.Ok(new HelloReply { Message = $"Hello {request.Name}" }).AsTask();
    }
}
```

### Style 2: POCO (JSON-bridged)

Declare your own plain classes instead, and `IGrpcMessageAdapter` bridges them via protobuf's own
JSON representation, matching properties by name (`Message`, not `message` — the generated C#
property names, not the `.proto` field names):

```csharp
[GrpcMethod("/greet.Greeter/SayHello")]
[Message("say_hello")]
public class SayHelloMessageHandler : IMessageHandler<SayHelloRequest, SayHelloReply>
{
    public Task<IBenzeneResult<SayHelloReply>> HandleAsync(SayHelloRequest request)
    {
        return BenzeneResult.Ok(new SayHelloReply { Message = $"Hello {request.Name}" }).AsTask();
    }
}

public class SayHelloRequest { public string Name { get; set; } = ""; }
public class SayHelloReply { public string Message { get; set; } = ""; }
```

Pick whichever style fits the handler — nothing else in the pipeline cares, and the two styles can
be mixed freely across handlers in the same service, or even across the two sides of one handler
(protobuf request in, POCO response out, or vice versa).

### Streaming handlers (D1)

Streaming handlers are ordinary message handlers whose request and/or response type is
`IAsyncEnumerable<T>` — `T` can be the protobuf item type or a POCO, independently, same rule as
above:

```csharp
// Server-streaming: IMessageHandler<TRequest, IAsyncEnumerable<TResponseItem>>
[GrpcMethod("/greet.Greeter/SayHelloServerStream")]
[Message("say_hello_server_stream")]
public class SayHelloServerStreamMessageHandler : IMessageHandler<HelloRequest, IAsyncEnumerable<HelloReply>>
{
    public Task<IBenzeneResult<IAsyncEnumerable<HelloReply>>> HandleAsync(HelloRequest request)
    {
        return BenzeneResult.Ok(Produce(request.Name)).AsTask();
    }

    private static async IAsyncEnumerable<HelloReply> Produce(string name)
    {
        foreach (var salutation in new[] { "Hello", "Hi", "Hey" })
        {
            yield return new HelloReply { Message = $"{salutation} {name}" };
        }
    }
}

// Client-streaming: IMessageHandler<IAsyncEnumerable<TRequestItem>, TResponse>
[GrpcMethod("/greet.Greeter/SayHelloClientStream")]
[Message("say_hello_client_stream")]
public class SayHelloClientStreamMessageHandler : IMessageHandler<IAsyncEnumerable<HelloRequest>, HelloReply>
{
    public async Task<IBenzeneResult<HelloReply>> HandleAsync(IAsyncEnumerable<HelloRequest> request)
    {
        var names = new List<string>();
        await foreach (var item in request)
        {
            names.Add(item.Name);
        }
        return BenzeneResult.Ok(new HelloReply { Message = $"Hello {string.Join(", ", names)}" });
    }
}

// Bidirectional: IMessageHandler<IAsyncEnumerable<TRequestItem>, IAsyncEnumerable<TResponseItem>>
[GrpcMethod("/greet.Greeter/SayHelloBidiStream")]
[Message("say_hello_bidi_stream")]
public class SayHelloBidiStreamMessageHandler : IMessageHandler<IAsyncEnumerable<HelloRequest>, IAsyncEnumerable<HelloReply>>
{
    public Task<IBenzeneResult<IAsyncEnumerable<HelloReply>>> HandleAsync(IAsyncEnumerable<HelloRequest> request)
    {
        return BenzeneResult.Ok(Produce(request)).AsTask();
    }

    private static async IAsyncEnumerable<HelloReply> Produce(IAsyncEnumerable<HelloRequest> source)
    {
        await foreach (var item in source)
        {
            yield return new HelloReply { Message = $"Hello {item.Name}" };
        }
    }
}
```

One middleware pipeline invocation happens per RPC call, not per stream item — a middleware you add
via `UseGrpc` (step 5) sees exactly one `HandleAsync` for a whole `SayHelloServerStream` call,
regardless of how many items the handler yields. Per-item concerns (throttling, per-item auth,
...) belong inside the handler itself, not the pipeline.

## 5. Wire it up

`Benzene.Grpc.AspNet` fits into the same `BenzeneStartUp` your other transports use — see
[Unified Hosting Model](hosting) if this is your first Benzene transport:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Grpc;
using Benzene.Grpc.AspNet;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddBenzeneGrpc();
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddBenzeneMessage()
            .AddMessageHandlers(typeof(SayHelloMessageHandler).Assembly)
            .AddGrpcMessageHandlers());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
        => app.UseGrpc(grpc => grpc.UseMessageHandlers());
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();

var app = builder.Build();
app.MapGrpcService<GreeterService>();
app.UseBenzene();
app.Run();
```

Two things that are easy to get wrong because gRPC's hosting model differs from Benzene's other
transports (see [Unified Hosting Model — gRPC on ASP.NET Core](hosting#grpc-on-aspnet-core)
for why):

- **`AddBenzeneGrpc()` in `ConfigureServices` is required**, in addition to `UsingBenzene(...)` —
  it registers ASP.NET Core's own gRPC services and `BenzeneInterceptor` as a server interceptor.
- **Handler types must be discoverable from `ConfigureServices`.** `AddMessageHandlers(assembly)`
  (assembly-scan) or an explicit `AddMessageHandlers(typeof(SayHelloMessageHandler), ...)` type list
  both work, because `BenzeneInterceptor`'s route table is built from ASP.NET Core's own
  per-request DI, populated during `ConfigureServices` — not from whatever you pass to
  `UseGrpc(grpc => grpc.UseMessageHandlers(...))` alone.
- **You still need `app.MapGrpcService<GreeterService>()`.**

## 6. How it works

`BenzeneInterceptor` overrides `Interceptor.UnaryServerHandler`/`ClientStreamingServerHandler`/
`ServerStreamingServerHandler`/`DuplexStreamingServerHandler` — one per RPC shape. On every call it
asks `IGrpcRouteFinder` (built by reflecting over `[GrpcMethod]` attributes across your discovered
message handlers) whether the call's method route has a registered handler:

- **No match** — falls through to `base.XxxServerHandler(..., continuation)`, i.e. the generated
  service class's method runs as normal.
- **Match** — substitutes the corresponding `IGrpcMethodHandler` method (`HandleAsync`/
  `ServerStreamingAsync`/`ClientStreamingAsync`/`DuplexStreamingAsync`) as the continuation instead.
  The generated service class's method is **never invoked** for that route.

Inside that substituted call, `GrpcMethodHandler` runs the `GrpcContext` middleware pipeline built
in step 5 (via a shared internal helper regardless of shape), then:

1. **Request mapping (D2)** — `GrpcRequestMapper` hands the handler's declared request type
   (`TRequest`) to `IGrpcMessageAdapter`: pass through untouched if it already *is* the incoming
   protobuf type (or, for a streaming request, the item type matches); otherwise convert via
   protobuf's own JSON representation (`JsonFormatter`/`JsonParser`, not a raw `System.Text.Json`
   round trip — this handles protobuf-specific constructs like enums and well-known types
   correctly).
2. **Handler execution** — your `HandleAsync` runs and returns an `IBenzeneResult<TResponse>`.
3. **Response mapping** — the reverse of step 1: pass through if the payload already is the
   protobuf response type, otherwise convert via the same JSON bridge.
4. **Status/trailer mapping (D4)** — the handler's `IBenzeneResult.Status` is mapped to a gRPC
   `StatusCode` (see the table below) and always added as a `benzene-status` response trailer,
   carrying the *original*, more specific status even when several statuses map to the same
   `StatusCode` (e.g. `Created`/`Accepted`/`Updated` all map to `OK`).

## 7. Metadata and trace context (D5)

Inbound gRPC metadata is mapped onto Benzene's own header abstraction, so header-driven middleware
(`UseW3CTraceContext()`, custom header middleware, ...) works unchanged over gRPC:

```csharp
app.UseGrpc(grpc => grpc
    .UseW3CTraceContext()
    .UseMessageHandlers());
```

Send `traceparent` (or whatever header your middleware reads) as call metadata from the client:

```csharp
var headers = new Metadata { { "traceparent", Activity.Current?.Id ?? "" } };
var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" }, headers);
```

Binary metadata entries (keys ending in `-bin`) are skipped, matching gRPC's own convention that
they aren't accessible as plain strings. Outbound: set `GrpcContext.ResponseHeaders` from a handler
or middleware to flush custom response headers before the first response message; `ResponseTrailers`
is a direct pass-through to `ServerCallContext.ResponseTrailers` (this is where `benzene-status`
gets added).

## 8. Status codes (D4)

| `BenzeneResultStatus` | gRPC `StatusCode` |
|---|---|
| `Ok`, `Ignored`, `Created`, `Accepted`, `Updated`, `Deleted` | `OK` |
| `BadRequest`, `ValidationError` | `InvalidArgument` |
| `Unauthorized` | `Unauthenticated` |
| `Forbidden` | `PermissionDenied` |
| `NotFound` | `NotFound` |
| `Conflict` | `AlreadyExists` |
| `NotImplemented` | `Unimplemented` |
| `ServiceUnavailable` | `Unavailable` |
| `UnexpectedError` / anything unrecognized | `Internal` |

A non-OK status throws `RpcException(new Status(mappedCode, detail))` — `detail` is the joined
`IBenzeneResult.Errors` if present, otherwise the raw status string. The `benzene-status` trailer
(see step 6) is added regardless of whether the call succeeded, so a client that also happens to be
a Benzene.Grpc client can recover the original status precisely (see
[Clients — gRPC](clients#grpc)) even where several statuses collapse to the same `StatusCode`.

## 9. Deadlines and cancellation (D6)

`GrpcContext.CancellationToken` (from the call's `ServerCallContext`) is available to any pipeline
middleware directly. Inside a handler, inject the scoped `IGrpcServerCallAccessor` instead — it
mirrors ASP.NET Core's `IHttpContextAccessor`:

```csharp
public class SayHelloMessageHandler : IMessageHandler<HelloRequest, HelloReply>
{
    private readonly IGrpcServerCallAccessor _accessor;

    public SayHelloMessageHandler(IGrpcServerCallAccessor accessor) => _accessor = accessor;

    public async Task<IBenzeneResult<HelloReply>> HandleAsync(HelloRequest request)
    {
        _accessor.CancellationToken.ThrowIfCancellationRequested();
        // ...
    }
}
```

A pipeline (not handler-level, per `MessageHandler<TRequest,TResponse>`'s own exception handling)
`OperationCanceledException` is translated to `RpcException(DeadlineExceeded)` if the call's
deadline has already passed, or `RpcException(Cancelled)` otherwise.

## 10. Health checks and reflection (D8)

Both off by default — each is real extra surface area (grpc.health.v1, grpc.reflection.v1alpha) a
service should opt into deliberately:

```csharp
services.AddBenzeneGrpc(o =>
{
    o.EnableHealthChecks = true;
    o.EnableReflection = true;
});
services.AddScoped<Benzene.HealthChecks.Core.IHealthCheck, DatabaseHealthCheck>();
```

```csharp
app.MapGrpcService<GreeterService>();
app.MapBenzeneGrpcHealthService();
app.MapBenzeneGrpcReflectionService();
```

See [Health Checks — gRPC](health-checks#grpc-grpchealthv1) for how `BenzeneHealthCheckBridge`
aggregates Benzene health checks onto the standard grpc.health.v1 protocol.

## 11. Calling other gRPC services

Package: `Benzene.Grpc.Client`. See [Clients — gRPC](clients#grpc) for the full guide;
`GrpcBenzeneMessageClient : IBenzeneMessageClient` sends unary calls through a Benzene middleware
pipeline over a `GrpcChannel` you own, with the same POCO-or-protobuf-direct choice on both sides
as the server, and status mapping symmetric to step 8.

## 12. Testing

Package: `Benzene.Grpc.TestHelpers`.

```csharp
using var host = BenzeneTestHost.Create<StartUp>()
    .BuildGrpcHost(endpoints => endpoints.MapGrpcService<GreeterService>());
var client = new Greeter.GreeterClient(host.CreateChannel());

var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" });
```

`BuildGrpcHost` extends `Benzene.Testing`'s `BenzeneTestHostBuilder<TStartUp>`, running your
`StartUp`'s `ConfigureServices`/`Configure` against a real in-process ASP.NET Core `TestServer` —
`BenzeneInterceptor` routing, the middleware pipeline, and serialization all run exactly as they
would in production; nothing is bypassed. `WithServices`/`WithConfiguration` (from
`BenzeneTestHostBuilder`) work the same way they do for every other platform's test host — see
[Testing Benzene](testing-benzene).

For unit-testing a `GrpcMethodHandler`/pipeline directly, without a host, `TestServerCallContext`
(also in `Benzene.Grpc.TestHelpers`) is a minimal hand-rolled `ServerCallContext` —
`TestServerCallContext.Create(method: ..., requestHeaders: ..., cancellationToken: ..., deadline: ...)`.
`Grpc.Core.Testing` is deliberately not a dependency anywhere in the Benzene.Grpc family.

## Troubleshooting

- **My `GreeterService` override's changes aren't showing up** — if a message handler carries a
  matching `[GrpcMethod("/greet.Greeter/SayHello")]`, `BenzeneInterceptor` always wins; the
  generated service class's method body only runs for routes with no matching handler.
- **Response fields are always empty/null (POCO handlers)** — check your POCO's property names
  match the protobuf message's generated C# property names exactly; the JSON bridge matches by
  name, not by field position or `.proto` field name.
- **"has been assigned to more than one message handler"** — `ReflectionGrpcMethodFinder` throws a
  `BenzeneException` at startup if two message handlers declare the same `[GrpcMethod("...")]`
  route; each gRPC method route may map to exactly one handler.
- **A streaming call never routes to my handler / falls through to `Unimplemented`** — confirm the
  handler type is registered in `ConfigureServices` (not only inside `UseGrpc`'s configuration
  action) — see step 5.
- **Headers aren't available in the handler** — confirm `AddBenzeneGrpc()` ran in
  `ConfigureServices`; also check for a `-bin`-suffixed key, which is intentionally skipped.
- **My client can't tell `Created` from `Accepted` from `Ok`** — all three map to `StatusCode.OK`;
  read the `benzene-status` trailer instead, or use `Benzene.Grpc.Client`'s
  `IGrpcStatusReverseMapper`, which already prefers it.

## See Also

- [Unified Hosting Model](hosting)
- [Clients](clients)
- [Health Checks](health-checks)
- [Testing Benzene](testing-benzene)
- [ASP.NET Core Integration](asp-net-core)
- [Message Handlers](message-handlers)
- [Middleware](middleware)
