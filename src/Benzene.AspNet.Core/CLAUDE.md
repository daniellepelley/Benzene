# Benzene.AspNet.Core

## What this package does
Hosts Benzene's message-handler pipeline inside an ASP.NET Core application. An incoming ASP.NET Core
`HttpContext` is wrapped as a Benzene `AspNetContext` and run through a Benzene middleware pipeline that
resolves a topic and dispatches to a message handler. This is **Benzene endpoints coexisting inside an
ASP.NET Core app** (Kestrel owns the process and the request pipeline) — it is not a minimal-API layer
and does not aim for minimal-API/MVC feature parity.

## Key types/interfaces
- `BenzeneExtensions` - the wiring entry points:
  - `WebApplicationBuilder.UseBenzene<TStartUp>()` (registers a platform-neutral `BenzeneStartUp`'s
    services) + `IApplicationBuilder.UseBenzene()` / `UseBenzene(Action<IAspApplicationBuilder>)` (runs
    `Configure`, wiring the pipeline into the ASP.NET Core request pipeline).
  - `IAspApplicationBuilder.UseHttp(Action<IMiddlewarePipelineBuilder<AspNetContext>>)` - configures the
    Benzene HTTP middleware pipeline.
- `IAspApplicationBuilder` / `AspApplicationBuilder` - adapts an ASP.NET Core `IApplicationBuilder` into
  a Benzene application builder; `AspApplicationBuilder : IAspApplicationBuilder, IBenzeneApplicationBuilder`.
- `AspNetApplication : EntryPointMiddlewareApplication<HttpContext>` - the entry-point app that wraps each
  `HttpContext` in an `AspNetContext` and runs the built pipeline.
- `AspNetContext : IHttpContext` - the Benzene context over ASP.NET Core's `HttpContext`.
- `DependencyInjectionExtensions.AddAspNetMessageHandlers()` - registers the ASP.NET-specific pieces
  (called automatically by `UseHttp`): the getter set (`AspNetMessageTopicGetter`,
  `AspNetMessageVersionGetter`, `AspNetMessageHeadersGetter`, `AspNetMessageBodyGetter`),
  `AspNetRequestEnricher`, `AspNetHttpRequestAdapter`, `AspNetResponseAdapter`,
  `AspMessageMessageHandlerResultSetter`, media-format negotiation, and a `TransportInfo("asp")`.

## When to use this package
- When building an ASP.NET Core web app and you want Benzene's hexagonal message handlers behind HTTP.
- When adding Benzene endpoints to an existing ASP.NET Core application (they run alongside its own
  middleware/routing).

## Dependencies on other Benzene packages
- **Benzene.Core.MessageHandlers** - message-handler pipeline, getters/adapters, media negotiation
- **Benzene.Http** - HTTP abstractions (`IHttpContext`, header mappings, `HttpStatusCodeResponseHandler`)
- **Benzene.Microsoft.Dependencies** - the MEL DI adapter (`AddBenzene`)
- Microsoft.AspNetCore.* comes via the ASP.NET Core framework reference (no separate NuGet here)

## Important conventions
- Register with `UseBenzene<TStartUp>()` on the `WebApplicationBuilder`, then `UseBenzene()` on the built
  app; or wire `IApplicationBuilder.UseBenzene(builder => builder.UseHttp(...))` by hand.
- Uses ASP.NET Core's built-in (MEL) DI container.
- Async/await throughout.
