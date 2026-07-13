# gRPC Setup

`Benzene.Grpc` lets a `Grpc.AspNetCore` service's unary methods be implemented by ordinary Benzene
message handlers instead of hand-written service method bodies. It's an early integration: **only
unary calls are supported today** (no server-streaming, client-streaming, or bidirectional streaming,
despite what the package's own internal notes say elsewhere), and it isn't wired into the
platform-neutral `BenzeneStartUp`/`IBenzeneApplicationBuilder` model the way HTTP, AWS Lambda, and
Azure Functions are — there's no `UseGrpc(this IBenzeneApplicationBuilder ...)` extension, so you
wire it up by hand in `Program.cs`, on top of a normal ASP.NET Core + `Grpc.AspNetCore` project.
This guide walks through exactly that wiring, matching
[`examples/Grpc`](../examples/Grpc), the only currently-compiling reference for this package.

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
dotnet add package Grpc.AspNetCore
dotnet add package Benzene.Grpc --prerelease
dotnet add package Benzene.AspNet.Core --prerelease
dotnet add package Benzene.Microsoft.Dependencies --prerelease
```

## 3. Define your `.proto` and generated service

Nothing Benzene-specific here — a standard unary RPC:

```proto
syntax = "proto3";

option csharp_namespace = "MyGrpcService";

package greet;

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
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

You still need a real service class deriving from the generated `Greeter.GreeterBase` and
overriding `SayHello` — `app.MapGrpcService<GreeterService>()` requires one to exist so gRPC's own
routing/reflection has somewhere to point. Once Benzene's interceptor takes over a given method
(step 6), **this override's body never runs for that method** — keep it minimal. If your service
has other RPC methods with no matching `[GrpcMethod]`-tagged handler, those methods' overrides
*do* still run normally — only routes Benzene has claimed are replaced:

```csharp
public class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        // Unreachable once BenzeneInterceptor routes "/greet.Greeter/SayHello" to a message
        // handler (step 4) — kept only so MapGrpcService<GreeterService>() has a class to bind.
        throw new NotImplementedException();
    }
}
```

## 4. Define a message handler

Business logic lives in a message handler, tagged with `[GrpcMethod]` giving the **full gRPC
method route** (`/<package>.<Service>/<Method>`, exactly as it appears in the generated client) —
this is what `BenzeneInterceptor` matches against the incoming call to decide whether to redirect
it to your handler instead of the generated service class:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Grpc;
using Benzene.Results;

[GrpcMethod("/greet.Greeter/SayHello")]
[Message("say_hello")]
public class SayHelloMessageHandler : IMessageHandler<SayHelloRequest, SayHelloReply>
{
    public Task<IBenzeneResult<SayHelloReply>> HandleAsync(SayHelloRequest request)
    {
        return BenzeneResult.Ok(new SayHelloReply
        {
            Message = "Hello " + request.Name
        }).AsTask();
    }
}

public class SayHelloRequest
{
    public string Name { get; set; }
}

public class SayHelloReply
{
    public string Message { get; set; }
}
```

Your handler's request/response types are **plain POCOs, not the generated protobuf
`HelloRequest`/`HelloReply` types** — Benzene bridges between them with a JSON round-trip (see
"How it works" below), matching properties by name. `[Message("...")]` still needs a value (it's
how the middleware pipeline routes internally) but nothing outside this package reads it as a
transport topic for gRPC.

## 5. Wire it up in `Program.cs`

Unlike the other getting-started guides, there's no `BenzeneStartUp` here — you construct the
`GrpcMethodHandlerFactory` and interceptor registration directly against `WebApplicationBuilder`,
matching `examples/Grpc/Benzene.Example.Grpc/Program.cs`:

```csharp
using Benzene.Abstractions.DI;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Grpc;
using Benzene.Microsoft.Dependencies;

var builder = WebApplication.CreateBuilder(args);

// Register BenzeneInterceptor as a gRPC server interceptor - this is what redirects
// matched calls away from the generated service class to a message handler.
builder.Services.AddGrpc(x => x.Interceptors.Add(typeof(BenzeneInterceptor)));

var benzeneServiceContainer = new MicrosoftBenzeneServiceContainer(builder.Services);
builder.Services.AddScoped<IBenzeneServiceContainer>(_ => benzeneServiceContainer);

// Build the GrpcContext middleware pipeline once, up front - unlike AWS/Azure/ASP.NET Core,
// there's no per-request pipeline construction path here.
var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<GrpcContext>(benzeneServiceContainer);
var grpcMethodHandlerFactory = new GrpcMethodHandlerFactory(
    benzeneServiceContainer,
    Greeter.Descriptor,
    middlewarePipelineBuilder.UseMessageHandlers().Build());
builder.Services.AddScoped<IGrpcMethodHandlerFactory>(_ => grpcMethodHandlerFactory);

builder.Services.UsingBenzene(x => x
    .AddBenzene()
    .AddBenzeneMessage()
    .AddMessageHandlers(typeof(SayHelloMessageHandler).Assembly)
    .AddGrpc());

var app = builder.Build();
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
```

`Greeter.Descriptor` is the generated `ServiceDescriptor` for your `.proto`'s service — swap it for
your own service's descriptor. `GrpcMethodHandlerFactory` needs it to look up each response
message's field layout when converting your handler's JSON-serialized POCO response back into the
generated protobuf type (see below).

## 6. How it works

`BenzeneInterceptor` overrides `Interceptor.UnaryServerHandler`. On every unary call it asks
`IGrpcRouteFinder` (built by reflecting over `[GrpcMethod]` attributes across your discovered
message handlers) whether the call's method route has a registered handler:

- **No match** — falls through to `base.UnaryServerHandler(request, context, continuation)`, i.e.
  the generated `GreeterService` method runs as normal.
- **Match** — calls `base.UnaryServerHandler(request, context, benzeneGrpcMethodHandler.HandleAsync)`,
  substituting your message handler's `HandleAsync` as the continuation. The generated service
  class's method is **never invoked** for that route.

Inside that substituted call, `GrpcMethodHandler.HandleAsync<TRequest, TResponse>` (where
`TRequest`/`TResponse` are the *generated protobuf* types, inferred from the gRPC method's own
signature) does the actual bridging:

1. Wraps the incoming protobuf request in a `GrpcContext<TRequest, TResponse>` and runs it through
   the `GrpcContext` middleware pipeline built in step 5.
2. `GrpcMessageBodyGetter` serializes the protobuf request object to JSON with
   `System.Text.Json` — this JSON is then deserialized into your handler's own POCO request type
   (`SayHelloRequest` above) by the normal message-handler request-mapping pipeline, matching
   properties by name.
3. Your handler runs and returns its own POCO response type.
4. `GrpcMessageMessageHandlerResultSetter` serializes that POCO to JSON, then
   `GrpcContext<TRequest, TResponse>.ResponseAsObject`'s setter deserializes it into the generated
   protobuf response type, completing the round trip back to a real `TResponse`.

Because of this JSON round trip, your POCO's property names need to match the protobuf message's
generated C# property names (`Message`, not `message`) for values to survive the trip.

## 7. Run it locally

```bash
dotnet run
```

Call it with a generated client, same as any gRPC service (see
`examples/Grpc/Benzene.Example.Grpc.Client` for a minimal console client against `greet.proto`):

```csharp
using var channel = GrpcChannel.ForAddress("https://localhost:7268");
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" });
Console.WriteLine(reply.Message); // "Hello World" - from SayHelloMessageHandler, not GreeterService
```

## 8. Testing

There's no `BenzeneTestHost` support for gRPC — `Benzene.Testing` has no `Build*`/`Send*Async`
extension for it, and no test project for this package exists anywhere in the repository today.
Test it the way you'd test any ASP.NET Core gRPC service: spin up the app (e.g. via
`WebApplicationFactory`, same as [ASP.NET Core Integration](asp-net-core#testing) or a real Kestrel
instance for a full integration test) and call it through a real `GrpcChannel`/generated client —
there's no shortcut that bypasses the interceptor and pipeline described above.

## Troubleshooting

- **My `GreeterService.SayHello` override's changes aren't showing up** — if a message handler
  carries a matching `[GrpcMethod("/greet.Greeter/SayHello")]`, `BenzeneInterceptor` always wins;
  the generated service class's method body only runs for routes with no matching handler.
- **Response fields are always empty/null** — check your POCO response's property names match the
  protobuf message's generated property names exactly; the JSON round trip matches by name, not by
  field position.
- **"has been assigned to more than one message handler"** — `ReflectionGrpcMethodFinder` throws a
  `BenzeneException` at startup if two message handlers declare the same `[GrpcMethod("...")]`
  route; each gRPC method route may map to exactly one handler.
- **Only unary calls work** — this is the current state of the package, not a misconfiguration;
  `BenzeneInterceptor` only overrides `UnaryServerHandler`, so streaming RPCs always fall through to
  your generated service class unmodified.
- **Headers aren't available in the handler** — `GrpcMessageHeadersGetter` always returns an empty
  dictionary today; gRPC metadata isn't bridged into Benzene message headers yet.

## See Also

- [Unified Hosting Model](hosting)
- [Testing Benzene](testing-benzene)
- [ASP.NET Core Integration](asp-net-core)
- [Message Handlers](message-handlers)
- [Middleware](middleware)
