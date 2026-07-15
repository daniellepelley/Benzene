# Benzene.GoogleCloud.Functions.Http.TestHelpers

## What this package does
Test-only helpers for exercising a `BenzeneStartUp` through `Benzene.GoogleCloud.Functions.Http`
without a live Cloud Functions Framework host - Phase 0 of `work/google-cloud-roadmap-1.0.md`.
Mirrors `Benzene.Aws.Lambda.Core.TestHelpers`/`Benzene.Azure.Function.Core.TestHelpers`'s shape and
role exactly.

## Key types/interfaces
- `BenzeneTestHostExtensions.BuildGoogleCloudFunctionHost<TStartUp>()` - the
  `Benzene.Testing.BenzeneTestHostBuilder<TStartUp>` bridge. Reconstructs the same
  `GoogleCloudStartUpRunner.Bootstrap` → `ConfigureServices` → `Configure` →
  `GoogleCloudFunctionApplicationBuilder.Build` sequence `GoogleCloudFunctionHost<TStartUp>` performs
  for a real deployment, but via `BenzeneTestHostBuilder.Build(...)` so any `WithServices`/
  `WithConfiguration` overrides registered on the builder are applied before `Configure` runs -
  something the real host's constructor has no seam for. Returns a private `IHttpFunction` wrapper
  around the built entry point application (Google's own interface has no Benzene-owned equivalent
  to return directly, unlike `IAwsLambdaEntryPoint`/`IAzureFunctionApp`).
- `BenzeneTestHostExtensions.SendHttpAsync(IHttpFunction, HttpContext)` - calls `HandleAsync` and
  returns the same context, now populated with whatever the pipeline wrote to `Response`.
- `HttpContextBuilder` - a small `DefaultHttpContext`-based builder (method/path/headers/body,
  serializing bodies with `System.Text.Json` - no new NuGet dependency). Promotes/generalizes the
  builder originally hand-rolled in `examples/Google/Benzene.Examples.Google.Tests`.

## When to use this package
- Writing tests for a Google Cloud Functions Gen2 HTTP-triggered `BenzeneStartUp` that need to
  dispatch a real `HttpContext` through the full pipeline and assert on the response, without
  starting Kestrel or the Functions Framework.

## Dependencies on other Benzene packages
- **Benzene.GoogleCloud.Functions.Http** - `GoogleCloudFunctionApplicationBuilder`.
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`, `MicrosoftBenzeneServiceContainer`,
  `MicrosoftServiceResolverFactory`.
- **Benzene.Testing** - `BenzeneTestHostBuilder<TStartUp>`.

## Important conventions
- Test-only: referenced from test projects, not shipped as a runtime dependency.
- Cloud Run needs none of this - a `BenzeneStartUp` hosted on Cloud Run is a plain ASP.NET Core app
  and can be tested with the standard `WebApplicationFactory`/`Microsoft.AspNetCore.Mvc.Testing`
  approach; this package exists only for the Cloud Functions Gen2 path.
