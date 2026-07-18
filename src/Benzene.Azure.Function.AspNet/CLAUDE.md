# Benzene.Azure.Function.AspNet

## What this package does
The **HTTP trigger** adapter for Benzene's Azure Functions isolated-worker host. It bridges the
ASP.NET Core `HttpRequest`/`IActionResult` shape that the isolated worker's
`Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` integration delivers into Benzene's
middleware pipeline, so `[HttpEndpoint(...)]`-attributed handlers and `.UseMessageHandlers()`
route-based dispatch work exactly as they do under `Benzene.AspNet.Core` and AWS API Gateway. It is
for **HTTP-triggered Azure Functions only** — not App Service, Container Apps, or AKS hosting (those
use `Benzene.AspNet.Core`).

## Key types/interfaces
- `AspNetContext : IHttpContext` — wraps the incoming `HttpRequest` and carries the handler's
  `ContentResult` back out; transport shape only.
- `AspNetHttpRequestAdapter` / `AspNetResponseAdapter` — map the request into the pipeline and the
  pipeline's result into an `IActionResult`.
- Mappers: `AspNetMessageTopicGetter` (topic from the matched `[HttpEndpoint]` route via the route
  finder — HTTP routes on method+path, not a message property), `AspNetMessageBodyGetter`,
  `AspNetMessageHeadersGetter`, `AspNetMessageVersionGetter`, `AspNetMessageMessageHandlerResultSetter`,
  `AspNetHeadersToBodyGetter`, and `AspNetContextRequestEnricher`.
- `AspNetApplication` — the entry point application (`EntryPointMiddlewareApplication`) the trigger
  function dispatches to.
- `DependencyInjectionExtensions.UseHttp(...)` — the wiring, on both `IAzureFunctionAppBuilder` and
  the platform-neutral `IBenzeneApplicationBuilder` (no-op off Azure Functions); registers the
  mappers via `AddAspNet()`.
- `Extensions.HandleHttpRequest(this IAzureFunctionApp, HttpRequest)` — the dispatch helper the
  `[HttpTrigger]` function method calls; returns `Task<IActionResult>`.

## When to use this package
- Handling HTTP-triggered Azure Functions with Benzene. Add it alongside
  `Benzene.Azure.Function.Core` (see `docs/azure-functions.md`, the primary getting-started guide).
- CORS is available here via the portable `Benzene.Http.Cors.CorsMiddleware` (`.UseCors(...)`), the
  same middleware ASP.NET Core and AWS API Gateway use — it is not an Azure-specific gap.

## Dependencies on other Benzene packages
- **Benzene.Azure.Function.Core** — the host/app builder this plugs into.
- **Benzene.Core.MessageHandlers** — routing and mapper infrastructure.
- **Benzene.Http** — the `[HttpEndpoint]` route model and CORS middleware.
- Uses a `FrameworkReference` to `Microsoft.AspNetCore.App` (not the EOL 2.1.x NuGet packages) plus
  `Microsoft.Azure.Functions.Worker.Extensions.Http`/`.Http.AspNetCore`.

## Important conventions
- The route prefix caveat bites here: Azure Functions defaults to prefixing HTTP routes with
  `/api`. The example strips it with an `OnRequest` step; see `docs/azure-functions.md`'s
  Troubleshooting entry.
- Test helpers (`TestHttpRequest`, `HttpBuilderExtensions`) live in
  `Benzene.Azure.Function.AspNet.TestHelpers`, not in this production package.
- Coverage: `AspNetPipelineTest.cs` and, for the unified host-builder path, `AzureUnifiedStartUpTest.cs`.
