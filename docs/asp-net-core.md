# ASP.NET Core Integration

Benzene runs inside a standard ASP.NET Core application, using the same platform-neutral
`BenzeneStartUp` base class as every other Benzene host, so business logic written as message
handlers is portable between ASP.NET Core, AWS Lambda, and Azure Functions without change. This
guide starts from an empty folder and ends with a working ASP.NET Core app handling an HTTP
request through a Benzene message handler.

## Overview

`Benzene.AspNet.Core` adapts ASP.NET Core's `HttpContext` to Benzene's transport-agnostic message
pipeline: incoming requests are mapped to a topic and a request object (via reflection-discovered
`[Message]`/`[HttpEndpoint]` attributes or manual `IHttpEndpointDefinition` registration), routed
to a message handler, and the handler's `IBenzeneResult` is mapped back onto the HTTP response.

Unlike AWS Lambda or Azure Functions, ASP.NET Core is not itself a Benzene *entry point* — it's a
long-running host, so Benzene is wired in as ordinary ASP.NET Core middleware rather than as the
whole application. That means a Benzene pipeline can sit alongside MVC controllers, minimal API
endpoints, or any other ASP.NET Core middleware in the same app: requests that don't match a
Benzene route or topic simply fall through to the rest of the pipeline.

Use this integration when you want:
- Hexagonal message handlers (`IMessageHandler<TRequest, TResponse>`) that are portable to other
  Benzene hosts later, without rewriting business logic.
- Benzene's middleware pipeline (correlation, validation, health checks, tracing) in front of an
  existing or new ASP.NET Core app.
- To migrate an ASP.NET Core app to Benzene incrementally, one route at a time, while
  controllers/minimal APIs keep handling everything else.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Installation

Benzene's packages are published as prerelease (`-alpha`) versions, so `--prerelease` is required
until 1.0:

```bash
dotnet add package Benzene.AspNet.Core --prerelease
```

`Benzene.AspNet.Core` brings in the middleware pipeline, message handler infrastructure, the
`Microsoft.Extensions.DependencyInjection` adapter (and with it the `BenzeneStartUp` base class),
and an `Microsoft.AspNetCore.App` framework reference, all transitively — no other Benzene package
is required for a plain HTTP app.

## 1. Create the project

```bash
dotnet new web -f net10.0 -o MyApi
cd MyApi
dotnet add package Benzene.AspNet.Core --prerelease
```

## 2. Define a message handler

Business logic lives in message handlers, not in a controller action — this keeps it testable and
portable across hosts. See [Message Handlers](message-handlers.md) for the full picture; the minimal
shape is:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

[Message("hello:world")]
[HttpEndpoint("GET", "/hello/{name}")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldRequest, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldRequest message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}!" }));
    }
}

public class HelloWorldRequest
{
    public string Name { get; set; }
}

public class HelloWorldResponse
{
    public string Message { get; set; }
}
```

`[Message]` maps the handler to a topic; `[HttpEndpoint]` maps an HTTP method and path to that same
topic — the `{name}` route parameter is bound onto `HelloWorldRequest.Name` automatically, the same
way query string parameters and mapped headers are. Both attributes are discovered by reflection,
so there is nothing further to register per-handler.

## 3. Define your StartUp

`BenzeneStartUp` (from `Benzene.Microsoft.Dependencies`, referenced transitively) is the
platform-neutral application definition shared by every Benzene host — the same class shape you'd
write for AWS Lambda or Azure Functions. Configure the HTTP pipeline via `UseHttp`:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http.UseMessageHandlers());
    }
}
```

Note there's no `.AddHttpMessageHandlers()` call needed in `ConfigureServices` here, unlike the AWS
Lambda/Azure Functions guides — on ASP.NET Core, `UseHttp` registers the HTTP-specific services
(serializer, route finder, request/response adapters, etc.) for you automatically the first time
it's called.

## 4. Wire it into Program.cs

Register `StartUp` with the `WebApplicationBuilder`, then run it against the built app:

```csharp
using Benzene.AspNet.Core;

var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();

var app = builder.Build();
app.UseBenzene();

app.Run();
```

`builder.UseBenzene<StartUp>()` runs `StartUp.GetConfiguration()` and `ConfigureServices` against
the builder's `IServiceCollection`, before the app is built. `app.UseBenzene()` (no type argument,
an extension on `IApplicationBuilder`) then runs `StartUp.Configure`, wiring the middleware built by
`UseHttp` into the ASP.NET Core request pipeline as ordinary middleware.

Since Benzene's middleware only intercepts the response if a handler matched and calls
`next()` otherwise, `app.UseBenzene()` can sit before `app.MapControllers()`/other endpoint
middleware in the same pipeline — routes Benzene doesn't own fall straight through.

## 5. Run it

```bash
dotnet run
```

`GET` `/hello/world` to confirm the handler above responds with `{"message":"Hello world!"}`.

## Configuration

### `GetConfiguration()` / `ConfigureServices` / `Configure`

`GetConfiguration()` runs once, before any services are registered, and its result is passed into
both `ConfigureServices` and `Configure`. Anything built on top of `Microsoft.Extensions.Configuration`
works here — environment variables, `appsettings.json` via `AddJsonFile(...)`, Azure App
Configuration, and so on.

### Routing

Benzene maps an incoming request to a topic and request object either by:
- **Attributes** (the common case): `[Message("topic")]` on the handler class, `[HttpEndpoint("METHOD", "/path")]`
  to bind an HTTP method/path to that topic. Both are picked up by reflection when you call
  `.AddMessageHandlers(assembly)` / `.UseMessageHandlers()` with no explicit type list.
- **Manual registration**: register an `IHttpEndpointDefinition` (and, if not attribute-scanned, an
  `IMessageHandlerDefinition`) as a singleton — useful for endpoints backed by a handler that isn't
  discovered by the assembly scan, such as a health check or an OpenAPI spec endpoint:

  ```csharp
  services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("GET", "/health", "healthcheck"));
  ```

### `IAspApplicationBuilder.UseHttp` vs `IBenzeneApplicationBuilder.UseHttp`

`UseHttp` has two overloads, both used above without you needing to think about which is which:
- `IAspApplicationBuilder.UseHttp(Action<IMiddlewarePipelineBuilder<AspNetContext>>)` — the
  ASP.NET Core-specific building block. It registers the HTTP-specific services
  (`AddAspNetMessageHandlers()`) and adds an entry point application to the ASP.NET Core pipeline.
- `IBenzeneApplicationBuilder.UseHttp(Action<IMiddlewarePipelineBuilder<AspNetContext>>)` — the
  platform-neutral overload used inside `BenzeneStartUp.Configure`, so the same `StartUp` class
  compiles against any Benzene host. It delegates to the ASP.NET Core-specific overload when `app`
  is actually an `IAspApplicationBuilder`, and is a no-op on every other platform.

You only need the ASP.NET Core-specific overload directly if you're wiring the app up by hand
instead of through `BenzeneStartUp` (see below).

### Wiring without `BenzeneStartUp`

If you'd rather keep a classic ASP.NET Core `Startup` class (`ConfigureServices`/`Configure`) —
for example, an existing app being migrated incrementally — you can call the same building blocks
directly instead of deriving from `BenzeneStartUp`. This is how
[`examples/Asp/Benzene.Example.Asp`](../examples/Asp/Benzene.Example.Asp) is wired:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    services.UsingBenzene();
    services.AddValidatorsFromAssemblyContaining<GetOrderMessageValidator>();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting();

    app.UseBenzene(benzene => benzene
        .UseHttp(asp => asp
            .UseMessageHandlers(router => router.UseFluentValidation())
        )
    );

    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
}
```

`services.UsingBenzene()` registers the default Benzene services without an accompanying
`BenzeneStartUp`; `app.UseBenzene(Action<IAspApplicationBuilder>)` (on `IApplicationBuilder`, not
`WebApplicationBuilder`) wraps `app` in an `IAspApplicationBuilder` and runs the given
configuration directly, rather than delegating to a registered `StartUp`. Both wiring styles use
the same underlying middleware, so message handlers written for one work unchanged with the other.

## Advanced Usage

### Correlation and `IBenzeneInvocation`

`IBenzeneInvocation` gives you a per-request identifier and platform name without depending on
`HttpContext` directly (so the same handler code works unchanged on other Benzene hosts). Add it to
the pipeline with `UseBenzeneInvocation()`:

```csharp
app.UseHttp(http => http
    .UseBenzeneInvocation()
    .UseMessageHandlers());
```

On ASP.NET Core, `IBenzeneInvocation.InvocationId` is populated from `HttpContext.TraceIdentifier`,
`Platform` is `"AspNet"`, and `GetFeature<HttpContext>()` returns the current request's native
`HttpContext` if you need to drop down to it.

For cross-service correlation see [Correlation IDs](correlation-ids.md) — this rides on the W3C
trace context propagation described next.

### W3C trace context

See [W3C Trace Context](monitoring.md#w3c-trace-context) in the monitoring guide for the full
picture of how `Benzene.Diagnostics`' `UseW3CTraceContext()` middleware continues a caller's
distributed trace. On ASP.NET Core specifically: the framework's own hosting layer already
extracts the inbound `traceparent` header and starts its own `Activity` before your middleware
pipeline runs, so `Activity.Current` is already correctly parented by the time Benzene's
automatically-wrapped middleware spans (from `AddDiagnostics()`) start. In practice this means you
usually don't need to add `UseW3CTraceContext()` yourself on ASP.NET Core — it exists as a portable
option mainly for HTTP-shaped transports that don't have that built-in extraction (API Gateway,
Azure Functions' ASP.NET-style trigger).

### Tracing and enrichment

`AddDiagnostics()` wraps every middleware in every pipeline in a `System.Diagnostics.Activity` span
automatically — no explicit call needed per middleware:

```csharp
services.UsingBenzene(x => x.AddDiagnostics());
```

To attach `invocationId`/`traceId`/`spanId`/`topic`/`transport`/`handler` to the logging scope and
the current `Activity` in one call, add `UseBenzeneEnrichment()` to the pipeline:

```csharp
app.UseHttp(http => http
    .UseBenzeneEnrichment()
    .UseMessageHandlers());
```

See [Monitoring & Diagnostics](monitoring.md) for the full set of options, and
[Common Middleware](common-middleware.md) for `UseBenzeneEnrichment()`'s exact behavior.

### Health checks

Health checks are transport-agnostic (`UseHealthCheck(topic, ...)` responds to a topic, not a
route directly), so on ASP.NET Core you pair it with an `IHttpEndpointDefinition` that maps a route
to that topic:

```csharp
services.AddSingleton<IHttpEndpointDefinition>(_ => new HttpEndpointDefinition("GET", "/health", "healthcheck"));
```

```csharp
app.UseHttp(http => http
    .UseHealthCheck("healthcheck", x => x
        .AddHealthCheck<DatabaseConnectionHealthCheck>())
    .UseMessageHandlers());
```

Place `UseHealthCheck` before `UseMessageHandlers` in the pipeline — it short-circuits requests for
its topic and calls `next()` for everything else. See [Health Checks](health-checks.md) for how to
write a health check and the full set of ways to register one.

### Validation

```csharp
app.UseHttp(http => http
    .UseMessageHandlers(router => router.UseFluentValidation()));
```

See [Fluent Validation](fluent-validation.md) for how validators are resolved and how failures are
turned into a `ValidationError` result.

### Testing

Use the framework's own `WebApplicationFactory` against your `Program`/`Startup` rather than a
Benzene-specific dispatch helper — since the app already *is* a standard ASP.NET Core app,
`WebApplicationFactory`/`TestServer` exercises the real request pipeline (routing, model binding,
middleware ordering) and gives you a real `HttpClient`. See the
[ASP.NET Core section of Testing Benzene](testing-benzene.md#aspnet-core) for the full walkthrough,
including overriding services via `WithWebHostBuilder`.

## See Also

- [Correlation IDs](correlation-ids.md)
- [Monitoring & Diagnostics](monitoring.md)
- [Testing Benzene](testing-benzene.md)
- [Health Checks](health-checks.md)
- [Fluent Validation](fluent-validation.md)
- [Message Handlers](message-handlers.md)
- [Middleware](middleware.md)
