# Benzene.GoogleCloud.Functions.Http

## What this package does
Real, tested Cloud Functions Gen2 HTTP trigger adapter for Benzene - Phase 0 of
`work/google-cloud-roadmap-1.0.md`. Replaces `examples/Google`'s previous hand-rolled,
pre-`BenzeneStartUp` pipeline construction with a real host mirroring
`Benzene.Aws.Lambda.Core.AwsLambdaHost<TStartUp>`'s shape. **Cloud Run needs none of this** - it's a
plain ASP.NET Core container, so it uses `Benzene.AspNet.Core`'s existing
`WebApplicationBuilder.UseBenzene<TStartUp>()` directly; this package exists only for the secondary
Cloud Functions Gen2 deployment target.

## Key types/interfaces
- `GoogleCloudFunctionHost<TStartUp> : IHttpFunction` - the host. Constructor mirrors
  `AwsLambdaHost<TStartUp>` exactly: `GoogleCloudStartUpRunner.Bootstrap<TStartUp>()`, then
  `startUp.ConfigureServices(...)`, then `startUp.Configure(new
  GoogleCloudFunctionApplicationBuilder(container), configuration)`, then builds the final
  `MicrosoftServiceResolverFactory` and calls the builder's `Build(...)` to get the cached entry
  point application. `HandleAsync(HttpContext context) => _app.SendAsync(context);`.
- `GoogleCloudFunctionApplicationBuilder : BenzeneApplicationBuilder, IAspApplicationBuilder` - the
  key piece. `Benzene.AspNet.Core.BenzeneExtensions.UseHttp(IBenzeneApplicationBuilder, ...)` is a
  no-op unless its argument `is IAspApplicationBuilder` - that interface itself has no dependency on
  a live ASP.NET Core `IApplicationBuilder` (only `Benzene.AspNet.Core`'s own
  `AspApplicationBuilder` implementation does, since it wires into a real request pipeline). By
  implementing `IAspApplicationBuilder` here without needing one, `Benzene.AspNet.Core`'s existing
  `AspNetContext`/`AspNetApplication`/`.UseHttp(...)` machinery is reused completely unmodified. Its
  `Add(...)` defers (stores the factory) rather than building immediately like
  `AspApplicationBuilder.Add` does, since the final `IServiceResolverFactory` isn't ready until
  after `ConfigureServices`/`Configure` both finish - `Build(IServiceResolverFactory)` is called by
  `GoogleCloudFunctionHost`'s constructor once it is.

## The architectural payoff worth remembering
**The exact same `Startup : BenzeneStartUp` class works unchanged on both Cloud Run and Cloud
Functions Gen2** - write it once (e.g. `app.UseHttp(asp => asp.UseMessageHandlers())`), host it on
Cloud Run via `WebApplicationBuilder.UseBenzene<Startup>()` or on Cloud Functions Gen2 via `class
Function : GoogleCloudFunctionHost<Startup> { }`. See `examples/Google` for the concrete
demonstration.

## When to use this package
- Deploying a Benzene HTTP service to Cloud Functions Gen2 specifically. If Cloud Run is your
  target (recommended - simpler, no Functions Framework dependency at all), you don't need this
  package; just `Benzene.AspNet.Core` in a plain container.

## Dependencies on other Benzene packages
- **Benzene.GoogleCloud.Functions.Core** - `GoogleCloudStartUpRunner.Bootstrap<TStartUp>()`.
- **Benzene.AspNet.Core** - `AspNetContext`, `AspNetApplication`, `IAspApplicationBuilder`, and the
  `UseHttp(...)` extensions this package's whole design exists to make work without a live
  `IApplicationBuilder`.
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`, `MicrosoftServiceResolverFactory`.

## Important conventions
- No custom request/response mapping code at all - `HttpContext` flows straight into
  `Benzene.AspNet.Core`'s real, already-tested ASP.NET Core adapter. Cloud Functions Gen2's .NET
  Functions Framework genuinely hosts your function inside a real ASP.NET Core Kestrel server, so
  this is architecturally correct reuse, not a shortcut - confirmed by reading the framework's own
  behavior, not assumed.
