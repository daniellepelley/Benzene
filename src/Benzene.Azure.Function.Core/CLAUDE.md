# Benzene.Azure.Function.Core

## What this package does
The foundation package for hosting Benzene inside an **Azure Functions isolated-worker** process.
It owns the app-builder abstraction every Azure Functions trigger adapter plugs into, and the
isolated-worker hosting glue that wires Benzene into `Program.cs`. Every other
`Benzene.Azure.Function.*` package (AspNet, EventHub, Kafka, ServiceBus, CosmosDb, QueueStorage,
BlobStorage, EventGrid, Timer) depends on this one and registers its entry point through the
`IAzureFunctionAppBuilder` defined here. It is not Azure-SDK-specific — it deliberately references
no Azure service SDK, only `Microsoft.Azure.Functions.Worker.*` for the host integration.

## Key types/interfaces
- `IAzureFunctionApp` / `AzureFunctionApp` — the built app the trigger function methods dispatch
  to. `HandleAsync<TRequest, TResponse>(request)` and `HandleAsync<TRequest>(request)` look up the
  registered entry point application whose request (and response) types match and run it. No match
  throws a `BenzeneException` that names the requested request/response shape and lists the
  registered entry points (e.g. "Wire the matching Use...() extension … in your StartUp's Configure
  method"), so a forgotten `UseHttp(...)`/`UseServiceBus(...)` is self-diagnosing rather than an
  opaque failure.
- `IAzureFunctionAppBuilder` / `AzureFunctionAppBuilder` — collects entry point application
  factories (`Add(...)`), creates per-context middleware pipeline builders (`Create<TContext>()`),
  and builds the `IAzureFunctionApp`. This is the `app` every `Use...()` trigger extension extends.
- `InlineAzureFunctionStartUp` — fluent `ConfigureServices(...)` / `Configure(...)` / `Build()`
  entry point used by tests and small standalone hosts to build an `IAzureFunctionApp` without a
  dedicated `BenzeneStartUp` subclass. Every `test/Benzene.Core.Test/Azure/*PipelineTest.cs`
  drives this.
- **Isolated-worker hosting glue** (added for the cross-platform `BenzeneStartUp` unification):
  - `HostBuilderExtensions.UseBenzene<TStartUp>()` — the `IHostBuilder` entry point that hooks a
    `BenzeneStartUp` into the Functions generic host.
  - `FunctionsWorkerApplicationBuilderExtensions.UseBenzene()` — worker middleware wiring on
    `IFunctionsWorkerApplicationBuilder`, called from `ConfigureFunctionsWebApplication(...)` in
    `Program.cs`.
  - `AzureFunctionAppBuilderExtensions.UseBenzeneInvocation()` — opt-in middleware that populates
    `IBenzeneInvocation` from the `FunctionContext.InvocationId`; requires the worker middleware
    above (see `docs/azure-functions.md`'s `IBenzeneInvocation` troubleshooting entry).

## When to use this package
- Referenced directly by any project hosting Benzene on Azure Functions (it's the base every
  trigger adapter needs), and by tests via `InlineAzureFunctionStartUp`.
- You rarely call its types by hand beyond `Program.cs`'s `UseBenzene<TStartUp>()`; the trigger
  packages' `Use...()` extensions do the `Add(...)`/`Create<TContext>()` wiring for you.

## Dependencies on other Benzene packages
- **Benzene.Core** / **Benzene.Core.Middleware** — the pipeline engine (`EntryPointMiddlewareApplication`,
  `BenzeneException`), which the entry point applications extend.
- **Benzene.Abstractions.Pipelines** — `IBenzeneApplicationBuilder`, the platform-neutral builder the
  trigger extensions also target so one `BenzeneStartUp` works across AWS/Azure/ASP.NET Core.
- **Benzene.HealthChecks** / **Benzene.Http** / **Benzene.Microsoft.Dependencies** — shared host wiring.
- **Microsoft.Azure.Functions.Worker / .Sdk** — the isolated-worker host (no Azure *service* SDK).

## Important conventions
- Isolated-worker only — no `Microsoft.Azure.WebJobs` (the old in-process model) anywhere.
- Dispatch is by request (and response) type, so two trigger types coexist in one app as long as
  their request types differ. When two triggers share a request type (e.g. two `[QueueTrigger]`
  functions, both `QueueStorageMessage[]`), disambiguate with an optional **discriminator key**:
  register each via `Use...(..., name: "queue-a")` and dispatch with the matching
  `Handle...(name, …)` — `AzureFunctionApp.HandleAsync<T>(request, name)` matches on key then type,
  and a `null` name keeps the type-only first-match behaviour (fully backward-compatible). See
  `AzureFunctionAppDispatchTest.cs`.
- Coverage: `AzureUnifiedStartUpTest.cs` (host-builder glue), `AzureFunctionAppErrorMessageTest.cs`
  (the no-entry-point diagnostic), and every trigger package's `*PipelineTest.cs` exercise this
  builder end to end.
