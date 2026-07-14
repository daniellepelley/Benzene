# Cross-Platform Design Review

**Goal:** Benzene should be very easy to use and offer developers the *same* experience whichever platform they run on — AWS Lambda, Azure Functions, a standalone console app, or hosted in Kubernetes.

This review assesses the current design against that goal and proposes a target design plus a phased roadmap. It covers startup, logging, tracing, routing, schema/spec, testing, tooling, packaging, CI and docs. All findings were verified against the code; file paths are cited throughout. Since the library is pre-1.0 (alpha packages), breaking changes are treated as acceptable where they buy consistency, and are flagged **(breaking)**.

> **2026-07-14 audit pass** — re-verified every claim in this document against the current codebase
> (not against the two follow-on specs, though their findings are consistent with this pass:
> `work/phase2-startup-unification-spec.md`, `work/phase2-azure-isolated-worker-spec.md`, both marked
> done 2026-07-14). Headline result: **far more of this review's target design has shipped than the
> original phasing implied**, including two independent tracks the review explicitly called out as
> parallelizable (§10: "The MEL rebase and the Activity/W3C work are independent of [the StartUp
> unification] and can proceed in parallel") that turned out to be fully done too. Section-by-section
> status is recorded inline below; summary:
> - **§2 Startup unification** — all four adapters (AWS Lambda, generic host/worker, ASP.NET Core,
>   Azure Functions isolated-worker) plus `IBenzeneInvocation` feature bag are shipped. The
>   implementation is **additive, not the breaking rewrite this review proposed**: `AwsLambdaStartUp`,
>   `AwsLambdaStartUp<TContainer>`/`IDependencyInjectionAdapter`, and `UseApiGateway` (AWS's HTTP verb)
>   are all still present, unrenamed, undeleted, no `[Obsolete]` bridge. `UseHttp` shipped for ASP.NET
>   Core, Azure Functions AspNet, and SelfHost, but **not** for AWS API Gateway. `Platform` is a plain
>   `string`, not the proposed `IHostingPlatform`. Mismatched verbs (e.g. calling `UseWorker(...)` on
>   an AWS host) **silently no-op** rather than throwing the proposed `BenzeneHostingException` — this
>   is documented as intentional in `docs/hosting.md`, not an oversight. `RunAwsLambdaAsync` and the
>   minimal-API `MapMessage`/`MapHttp` sugar were not built.
> - **§3 Logging** — the full breaking MEL rebase happened, not just the "Phase 1 interim fix": the
>   `IBenzeneLogger`/`IBenzeneLogAppender`/`BenzeneLogLevel` stack, and the `Benzene.Serilog`/
>   `Benzene.Log4Net`/`Benzene.Microsoft.Logging` packages, no longer exist anywhere in `src/`.
>   `ExceptionHandlerMiddleware` now logs via `ILogger` before invoking the exception handler.
> - **§4 Tracing** — also fully rebased: `Benzene.Diagnostics` now has `BenzeneDiagnostics.ActivitySource`/
>   `Meter`, `ActivityMiddlewareWrapper` (auto-wraps every middleware in an `Activity`),
>   `UseW3CTraceContext()`, and `UseBenzeneEnrichment()` (the unified enrichment middleware, replacing
>   the AWS-only `WithRequestId`/`WithApplication`, now `[Obsolete]`). `Benzene.OpenTelemetry` exports
>   both. `Benzene.Datadog`, `Benzene.Zipkin`, and `Benzene.Aws.XRay` are all deleted. Gaps remain:
>   `traceparent` extraction is HTTP-only (SQS/SNS/Kafka/Event Hub inbound not implemented), and
>   `ExceptionHandlerMiddleware` logs but does not set `Activity` status on error.
> - **§5 Routing** — `BENZ001` (duplicate topic) diagnostic is re-enabled. `BENZ002`/`BENZ003` and the
>   minimal-API `MapMessage`/`MapHttp` registration are not implemented. `AddGeneratedMessageHandlers()`
>   is still not referenced by any doc or example — reflection scanning remains the only path shown.
> - **§6 Schema/spec** — `DefaultJsonSchemaProvider<TContext>` is fully implemented (`Json.Schema.Generation`,
>   camelCase, cached). The CLI is unchanged: still hand-rolled argument parsing, no `System.CommandLine`,
>   no `ISpecSource` abstraction, spec fetch still AWS-Lambda-only via `AwsLambdaSpecClient`.
> - **§7 Testing** — a unified `BenzeneTestHost`/`BenzeneTestHostBuilder` now lives in `Benzene.Testing`,
>   built directly on `BenzeneStartUp`, with `WithServices`/`WithConfiguration` plus platform `Build*`
>   extensions — but only for **two** of the four platforms today (`BuildAwsLambdaHost()` in
>   `Benzene.Aws.Lambda.Core.TestHelpers`, `BuildAzureFunctionApp()` in
>   `Benzene.Azure.Function.Core.TestHelpers`); no `BuildWorkerHost`/ASP.NET equivalent yet. The
>   `Benzene.Testing`/`Benzene.Tools` `MessageBuilder`/`HttpBuilder` duplication is resolved — the
>   `Benzene.Tools` copies are commented out in favor of `Benzene.Testing`'s. `docs/testing-benzene.md`
>   is now platform-neutral (AWS + Azure examples).
> - **§8 Packaging/CI** — `Directory.Build.props` (root + `src/`) now provides the single version
>   source (`version.txt`) and centralized package metadata; no hardcoded per-csproj `PackageVersion`
>   remains; `VERSIONING.md` is rewritten. Deviation: the review proposed gating `IsPackable=false` for
>   `*.TestHelpers`/`Benzene.Tools`; the actual `src/Directory.Build.props` deliberately packs
>   *everything* under `src/`, with a comment explaining TestHelpers/Testing/Tools are considered
>   user-facing. Package consolidation (~80 → ~35) has **not** happened — `Benzene.Abstractions.*`
>   and `Benzene.Core.*` are still separate packages (79 packages under `src/` today, down from ~80
>   only because Datadog/Zipkin/XRay were deleted). CI still only runs `Benzene.Core.Test` and
>   `Benzene.Aws.Tests`; `Benzene.Integration.Test`, `Benzene.Grpc.Test`, and example test projects are
>   still not exercised in any workflow. `deploy-asp-example.yml` still deploys an ASP.NET app via the
>   Azure Functions action and still uses the wrong `./Examples/Asp/...` path casing (there is no
>   Azure-specific deploy workflow at all now — `deploy-azure-example.yml` doesn't exist).
> - **§9 Docs/templates** — real getting-started docs now exist for Azure Functions, ASP.NET Core,
>   Worker Services, Kafka, and gRPC (`docs/azure-functions.md`, `docs/asp-net-core.md`,
>   `docs/getting-started-worker.md`, `docs/getting-started-kafka.md`, `docs/getting-started-grpc.md`),
>   plus a new `docs/hosting.md` documenting the unified StartUp model directly. `docs/index.md` no
>   longer lists Azure Functions as "Coming Soon" — only "Client SDKs" still is. `dotnet new` templates
>   were **not** built (no `/templates` directory, no template NuGet packages). The "Kakfa" typo is
>   **still present** throughout `examples/Kafka/Benzene.Examples.Kakfa*` and even leaked into
>   `docs/getting-started-kafka.md`/`docs/getting-started-worker.md`/`CHANGELOG.md`.
>
> Detailed per-section notes and roadmap-table updates are inline below.

---

## 1. Where Benzene already delivers

The handler/routing core is genuinely portable and is the library's strength:

- `IMessageHandler<TRequest, TResponse>` + `[Message(topic, version)]` (`src/Benzene.Core.MessageHandlers/MessageAttribute.cs`) are identical on every platform.
- `MessageRouter<TContext>` (`src/Benzene.Core.MessageHandlers/MessageRouter.cs`) is itself an `IMiddleware<TContext>`, so the dispatch model is uniform.
- `UseMessageHandlers(r => r.UseFluentValidation())` is the one pipeline verb that already looks the same in every example — AWS, Azure, ASP.NET, Kafka, gRPC.
- The middleware abstractions (`src/Benzene.Abstractions.Middleware/`) are clean: contravariant `IMiddleware<in TContext>`, sub-pipelines via `Create<TNewContext>()`, context converters.
- All ~95 projects uniformly target `net10.0`.

Everything *around* that core diverges by platform, with AWS far ahead and Azure/console/Kubernetes lagging. That divergence is the subject of this review.

---

## 2. Startup experience (the biggest DX gap)

> **RESOLVED 2026-07-14, additively.** The table and inconsistencies below describe the state as
> originally reviewed. As of this audit, the target design further down (`BenzeneStartUp` +
> `IBenzeneApplicationBuilder` + per-platform hosts + `IBenzeneInvocation`) is implemented and shipped
> for all four platforms (AWS Lambda, generic host/worker, ASP.NET Core, Azure Functions
> isolated-worker) — see `work/phase2-startup-unification-spec.md` and
> `work/phase2-azure-isolated-worker-spec.md` for the verification detail, and `docs/hosting.md` for
> the shipped user-facing docs. Critically, this happened **without** deleting any of the old,
> platform-specific StartUp types described below — `AwsLambdaStartUp`/`AwsLambdaStartUp<TContainer>`,
> `BenzeneWorkerStartup`/`BenzeneHostedServiceStartup`, and the plain-ASP.NET `UseBenzene(b =>
> b.UseAspNet(...))` path all still exist unchanged, side by side with the new unified path. So the
> "current state" table below is still accurate as a description of what *also* still exists, not
> just history — Benzene now has both an old per-platform StartUp story and a new unified one, not a
> replacement of the former by the latter. See the target-design section further down for exactly
> which specific breaking changes it proposed did and didn't materialize.

### Current state

| Platform | Base class | App builder (`TAppBuilder`) | Entry-point model |
|---|---|---|---|
| AWS Lambda | `AwsLambdaStartUp` (`src/Benzene.Aws.Lambda.Core/AwsLambdaStartUp.cs`) | `IMiddlewarePipelineBuilder<AwsEventStreamContext>` | The StartUp **is** the Lambda handler (`FunctionHandlerAsync`) |
| Azure Functions | `AzureFunctionStartUp` (`src/Benzene.Azure.Function.Core/`) | `AzureFunctionAppBuilder` | StartUp registers a scoped `IAzureFunctionApp` that trigger classes inject; implements legacy `IWebJobsStartup` while the example `Program.cs` uses isolated-worker `ConfigureFunctionsWorkerDefaults()` — two hosting models mixed |
| Console / worker | `BenzeneWorkerStartup` / `BenzeneHostedServiceStartup` (`src/Benzene.SelfHost/`, `src/Benzene.HostedService/`) | `IBenzeneWorkerStartup` | The StartUp **is** an `IBenzeneWorker` / `IHostedService` |
| ASP.NET / Kubernetes | **none** | `IApplicationBuilder` via `UseBenzene(b => b.UseAspNet(...))` (`src/Benzene.AspNet.Core/BenzeneExtensions.cs`) | Plain ASP.NET middleware; no Benzene StartUp at all |
| gRPC / Google Cloud | none | manual | Everything hand-wired in `Program.cs` / the function class |

Further inconsistencies:

- **Registration entry differs.** ASP.NET/gRPC call `services.UsingBenzene()` on `IServiceCollection`; Lambda/Azure/worker call `AddBenzene()`/`AddMessageHandlers()` on `IBenzeneServiceContainer` inside the base class.
- **The HTTP verb differs per platform**: `UseApiGateway` (AWS), `UseHttp` (Azure, SelfHost), `UseAspNet` (Kubernetes). Same concept, three names.
- **Autofac is first-class only on AWS**, and only via an *example* base class (`AutofacAwsStartUp` in `examples/Aws/`), because `AwsLambdaStartUp<TContainer>` takes an `IDependencyInjectionAdapter<TContainer>` constructor generic that no other platform mirrors.
- Minor: `src/Benzene.Autofac/MicrosoftDependencyInjectionAdapter.cs` is misnamed (the class of that name lives in `Benzene.Microsoft.Dependencies`); the Kafka example spells "Kakfa" throughout.

The net effect: a developer who learns Benzene on Lambda has to relearn the bootstrap on every other platform, and the ASP.NET/Kubernetes story — the most common .NET hosting model — has no guided path at all.

### Target design: one StartUp, one builder, ~5-line hosting shims

> **Status 2026-07-14: shipped, with several concrete deviations from the plan below** — noted inline
> at each point rather than silently rewritten. Net: the ergonomic goal (one `StartUp`, ~5-line
> platform shims) is real and working; the specific mechanics (generics collapsed via
> `IServiceProviderFactory<T>`, uniform `UseHttp` verb, typed `IHostingPlatform`, exceptions on
> platform mismatch, automatic invocation registration) mostly did not happen exactly as specified.

**Principle:** everything a developer writes — handlers, validation, pipeline configuration, tests — compiles unchanged across platforms. Only `Program.cs` (or the Lambda handler string) differs.

Keep `IStartUp<,,>` as an SPI, but ship **one concrete abstract base** that closes all three type parameters and make it the only documented pattern:

```csharp
// Benzene.Hosting (new package, or folded into Benzene.Core)
public abstract class BenzeneStartUp
    : IStartUp<IServiceCollection, IConfiguration, IBenzeneApplicationBuilder>
{
    public virtual IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build(); // sensible default; today this is abstract on every platform

    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    public abstract void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration);
}
```

> **Shipped as `src/Benzene.Microsoft.Dependencies/BenzeneStartUp.cs`** — one deviation:
> `GetConfiguration()` stayed `abstract`, not `virtual` with the sensible default shown above (every
> `BenzeneStartUp` subclass, including the ones in `test/Benzene.Core.Test/Hosting/`, still implements
> it itself). Lives in `Benzene.Microsoft.Dependencies`, not a new `Benzene.Hosting` package.

Rationale for closing the generics:

- `TConfiguration` is already `IConfiguration` on every platform — the generic buys nothing.
- `TContainer` should be fixed to `IServiceCollection`. Third-party containers plug in underneath via the standard `IServiceProviderFactory<T>` pattern (as ASP.NET Core does), replacing the `IDependencyInjectionAdapter<TContainer>` constructor generic. **(breaking:** `AwsLambdaStartUp<TContainer>` and the Autofac example base go away; Autofac support becomes `UseServiceProviderFactory(new AutofacServiceProviderFactory())` on any host).

> **NOT done this way.** `TContainer` is fixed to `IServiceCollection` in the new `BenzeneStartUp`,
> but the old `AwsLambdaStartUp<TContainer>` and `IDependencyInjectionAdapter<TContainer>`
> (`src/Benzene.Abstractions/DI/IDependencyInjectionAdapter.cs`) were kept, not deleted (additive
> migration, per the phase-2 spec's explicit "ADDITIVE ONLY" guardrail). There is no
> `IServiceProviderFactory<T>`/`UseServiceProviderFactory(...)` integration anywhere in `src/` —
> Autofac support for a `BenzeneStartUp`-based app has not been built; `Benzene.Autofac` still only
> plugs into the old `AwsLambdaStartUp<TContainer>` constructor-generic path.

- `TAppBuilder` becomes `IBenzeneApplicationBuilder` — a merge of what `AzureFunctionAppBuilder`, `AspApplicationBuilder` and `IBenzeneWorkerStartup` already are (all three already expose `Create<TContext>()` / `Add(...)` / `Register(...)`, so this is a rename/merge, not new machinery):

```csharp
public interface IBenzeneApplicationBuilder : IRegisterDependency
{
    IMiddlewarePipelineBuilder<TContext> Create<TContext>();
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func);
    IHostingPlatform Platform { get; } // "AwsLambda" | "AzureFunctions" | "AspNet" | "Worker"
}
```

> **Shipped, close but not identical** (`src/Benzene.Abstractions.Pipelines/Hosting/IBenzeneApplicationBuilder.cs`):
> `Create<TContext>()` and `Platform` are there; `Add(...)` isn't part of the interface (each platform's
> `Use*` extension registers what it needs directly against the concrete builder type it pattern-matches
> on, e.g. `AwsLambdaApplicationBuilder.EventPipeline`); `Platform` is a plain `string`
> (`"AwsLambda"`/`"Worker"`/`"AspNet"`/Azure's platform constant), not a typed `IHostingPlatform` —
> no such interface/enum exists anywhere in the codebase.

Transport verbs stay extension methods living in platform packages (`UseSqs` only exists when `Benzene.Aws.Lambda.Sqs` is referenced). A verb that doesn't apply to the current `Platform` throws a clear `BenzeneHostingException` ("UseSqs requires the AWS Lambda host") at build time rather than silently doing nothing.

> **Deviation, and a real design disagreement worth flagging back to this document's author:**
> mismatched verbs (e.g. calling `.UseWorker(...)` against an AWS host's builder) **silently no-op**
> today — `UseAwsLambdaApplicationBuilderExtensions.UseAwsLambda`/`WorkerApplicationBuilderExtensions.UseWorker`
> both pattern-match with `if (app is XApplicationBuilder x) { configure(...); } return app;` and do
> nothing on a type mismatch. No `BenzeneHostingException` type exists. `docs/hosting.md` documents
> this no-op behavior as the intended design ("platform-specific `Use*` extension methods ...
> pattern-match on the concrete type and no-op if you call the wrong one for the platform actually
> running") rather than the review's proposed fail-fast exception — this is a considered deviation,
> not an omission, but it does mean a developer who mistypes/misconfigures cross-platform blocks gets
> silent no-ops instead of a build-time (or even run-time) error.

**Uniform verb vocabulary (breaking renames, with `[Obsolete]` forwarders for one release):**

| Logical transport | Today | Target |
|---|---|---|
| HTTP | `UseApiGateway` / `UseHttp` / `UseAspNet` | `UseHttp(...)` everywhere |
| Queues/streams | `UseSqs`, `UseSns`, `UseKafka`, `UseEventHub` | unchanged (already consistent) |
| Direct invoke | `UseBenzeneMessage` | unchanged |
| Handlers | `UseMessageHandlers(r => r.UseFluentValidation())` | unchanged — protect this |

> **Partially done, no renames/forwarders.** `UseHttp(this IBenzeneApplicationBuilder, ...)` now
> exists for ASP.NET Core (`Benzene.AspNet.Core/BenzeneExtensions.cs`) and Azure Functions AspNet
> (`Benzene.Azure.Function.AspNet/DependencyInjectionExtensions.cs`), and `Benzene.SelfHost.Http`
> has `UseHttp(this IBenzeneWorkerStartup, ...)`. **AWS Lambda's HTTP verb was not renamed** — it is
> still `UseApiGateway` (`src/Benzene.Aws.Lambda.ApiGateway/Extensions.cs`), with no `UseHttp` verb on
> `IBenzeneApplicationBuilder` for the AWS host and no `[Obsolete]` forwarder either way. So "HTTP is
> `UseHttp(...)` everywhere" is true for 3 of 4 platforms, not AWS.

**The four Programs.** The same `StartUp : BenzeneStartUp` file is used verbatim on every platform:

```csharp
public class StartUp : BenzeneStartUp
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddScoped<IOrderService, OrderService>();

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http.UseHealthCheck("/healthcheck").UseSpec()
            .UseMessageHandlers(r => r.UseFluentValidation()))
        .UseKafka(k => k.UseMessageHandlers(r => r.UseFluentValidation()));
}
```

AWS Lambda — keeps today's "the class is the handler" ergonomics, but the shim subclasses the *host*, not the StartUp:

```csharp
public class Function : AwsLambdaHost<StartUp> { }
// function-handler: MyApp::MyApp.Function::FunctionHandlerAsync
```

> **Shipped exactly as proposed** — `AwsLambdaHost<TStartUp>` (`src/Benzene.Aws.Lambda.Core/AwsLambdaHost.cs`)
> matches this shape. `BenzeneHost.RunAwsLambdaAsync<StartUp>()` was **not** built — no `BenzeneHost`
> type exists anywhere in the codebase; the RuntimeSupport/executable style isn't offered.

(`AwsLambdaHost<TStartUp>` is today's `AwsLambdaStartUp` constructor/pipeline-caching logic moved into a host class that instantiates `TStartUp`. Also offer `await BenzeneHost.RunAwsLambdaAsync<StartUp>();` for the RuntimeSupport/executable style.)

Azure Functions — isolated worker only; delete the EOL in-process `IWebJobsStartup` path **(breaking)**:

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(w => w.UseBenzene()) // worker middleware dispatches into the pipeline
    .UseBenzene<StartUp>()
    .Build();
host.Run();
```

> **Shipped, this one genuinely as the breaking change proposed** — verified via
> `work/phase2-azure-isolated-worker-spec.md`'s 2026-07-14 audit: `Microsoft.Azure.WebJobs` is fully
> gone from `src/`, all four `Benzene.Azure.Function.*` packages are isolated-worker only, and
> `examples/Azure/Benzene.Example.Azure/Program.cs` uses `ConfigureFunctionsWebApplication()` (the
> newer ASP.NET-Core-integrated isolated-worker host builder, not the `ConfigureFunctionsWorkerDefaults()`
> shown above — a reasonable adaptation to how the isolated-worker SDK evolved, not a deviation from
> intent) + `.UseBenzene<StartUp>()`.

The isolated-worker middleware replaces the scoped `IAzureFunctionApp` indirection, so trigger functions become one-line pass-throughs (or disappear for HTTP via a catch-all proxy function).

Console / Kubernetes worker (absorbs `BenzeneWorkerStartup`/`BenzeneHostedServiceStartup`):

```csharp
await Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build().RunAsync();
```

> **Shipped as `IHostBuilder.UseBenzene<TStartUp>()` in `src/Benzene.HostedService/HostBuilderExtensions.cs`**,
> matching this shape exactly. "Absorbs" is optimistic, though: `BenzeneWorkerStartup`/
> `BenzeneHostedServiceStartup` were kept, not absorbed/deleted — this is purely additive, a second
> path alongside the old one, same as the AWS case above.

ASP.NET / Kubernetes HTTP (implemented on top of the existing `AspApplicationBuilder`):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();  // GetConfiguration + ConfigureServices
var app = builder.Build();
app.UseBenzene();               // StartUp.Configure against an AspNet-backed builder
app.Run();
```

> **Shipped, matching this shape** (`src/Benzene.AspNet.Core/BenzeneExtensions.cs`,
> `WebApplicationBuilder.UseBenzene<TStartUp>()` / `WebApplication.UseBenzene()`) — this was explicitly
> flagged in this review as the most common .NET hosting model with "no guided path at all"; that gap
> is now closed, and it's also documented (`docs/asp-net-core.md`, `docs/hosting.md`).

`UseBenzene<TStartUp>()` on `IHostBuilder` becomes the single registration entry; `services.UsingBenzene()` and `AddBenzene()` remain internal plumbing.

**Platform context without breaking portability.** Handlers must stay platform-neutral, so platform context is opt-in via DI rather than the handler signature — a feature bag modeled on `HttpContext.Features`:

```csharp
public interface IBenzeneInvocation // scoped; registered by every host shim
{
    string InvocationId { get; }  // AwsRequestId | FunctionContext.InvocationId | TraceIdentifier | Guid
    IHostingPlatform Platform { get; }
    T? GetFeature<T>() where T : class; // ILambdaContext, FunctionContext, HttpContext, ...
}
```

> **Shipped, close but two deviations** (`src/Benzene.Abstractions.Pipelines/Hosting/IBenzeneInvocation.cs`
> + `IBenzeneInvocationAccessor`, implementations in `Benzene.Core.Middleware`, `Benzene.Aws.Lambda.Core`,
> `Benzene.AspNet.Core`, `Benzene.Azure.Function.Core`): `InvocationId`/`Platform`/`GetFeature<T>()` all
> present as specified, `Platform` is `string` not `IHostingPlatform` (consistent with the deviation
> noted above), and all three platforms populate `GetFeature<T>()` with the expected native type
> (`ILambdaContext`, `HttpContext`, `FunctionContext`). The two deviations: (1) it is **not**
> "registered by every host shim" automatically — each pipeline must explicitly call
> `.UseBenzeneInvocation()`, and it does not propagate into nested per-message DI scopes (e.g. AWS's
> per-record SQS/SNS sub-pipelines) without adding it there too, per the type's own XML doc remarks;
> (2) it's scoped per-*pipeline*, not globally per-request, so a StartUp with multiple `Use*` blocks
> needs it added to each one that wants it.

A Lambda-only handler calls `invocation.GetFeature<ILambdaContext>()`; portable handlers never touch it. This replaces the current situation where `AwsEventStreamContext` carries `ILambdaContext` but Azure/ASP.NET expose nothing analogous.

---

## 3. Logging

> **RESOLVED 2026-07-14 — the full breaking rebase happened, not just the Phase 1 interim fix.**
> Verified: `IBenzeneLogger`, `IBenzeneLogAppender`, `IBenzeneLogContext`, `BenzeneLogLevel` do not
> exist anywhere under `src/` (repo-wide grep, zero hits). `Benzene.Serilog`, `Benzene.Log4Net`, and
> `Benzene.Microsoft.Logging` are not present in `src/` either — they were deleted, not shrunk to thin
> provider wrappers as the target design suggested as one option. 1,850 files reference
> `Microsoft.Extensions.Logging`. `ExceptionHandlerMiddleware<TContext>`
> (`src/Benzene.Core.Middleware/ExceptionHandlerMiddleware.cs`) now takes an `ILogger` constructor
> parameter and calls `logger.LogError(ex, "Unhandled exception caught in middleware pipeline")`
> before invoking the exception handler — the "regardless of the rebase" requirement is met. Not
> done: it does **not** set `Activity` status on the caught exception (no `SetStatus`/`AddException`
> call) — see §4's tracing note for the same gap from the other side. The AWS roadmap audit
> (`work/aws-roadmap-1.0.md`, commits `3f3b25d`/`eee1aa5`) independently confirms this rebase and its
> AWS-side effects (`WithRequestId()`/`WithApplication()` marked `[Obsolete]`, superseded by
> `Benzene.Diagnostics`'s `UseBenzeneEnrichment()` — see §4 below).

### Current state

Benzene has a custom logging stack (`src/Benzene.Abstractions/Logging/`): `IBenzeneLogger`, `IBenzeneLogAppender` (sinks), `IBenzeneLogContext` (structured scopes), and a `BenzeneLogLevel` enum duplicating Microsoft.Extensions.Logging's levels. Three problems, verified in code:

1. **Silent by default.** `AddDefaultBenzeneLogging` (`src/Benzene.Core/DI/Extensions.cs`) registers `BenzeneLogger` — a composite over an *empty* appender list — plus `NullBenzeneLogContext`. Forget the provider `Add*` call and every log line is silently dropped, with no warning.
2. **Structured context is dead even with a provider.** `AddMicrosoftLogger()` / `AddSerilog()` / `AddLog4Net()` register only the **appender**, never the matching `IBenzeneLogContext` (`MicrosoftBenzeneLogContext` / `SerilogBenzeneLogContext` exist but are registered nowhere in the framework — only one example wires its own). So `UseLogContext`/`UseLogResult` properties — `correlationId`, `requestId`, `processTime` — fall into `NullBenzeneLogContext` and are discarded in every default setup. The structured-logging feature is effectively dead code.
3. **Enrichment is asymmetric.** AWS has `WithRequestId()`/`WithApplication()` (`src/Benzene.Aws.Lambda.Core/LogContextBuilderExtensions.cs`) and `WithHttp()` (API Gateway); **Azure has no `InvocationId` equivalent at all**, and the Azure example has its logging/correlation lines commented out. Additionally, `ExceptionHandlerMiddleware` (`src/Benzene.Core.Middleware/`) swallows exceptions without logging — only the SQS transports guarantee an error log.

### Target design: rebase on Microsoft.Extensions.Logging (breaking, the right call pre-1.0)

Delete the `IBenzeneLogger`/`IBenzeneLogAppender`/`BenzeneLogLevel` stack and use `ILogger`/`ILogger<T>` throughout the framework:

- MEL is already present on every target platform (Lambda logger, Functions worker, generic host, ASP.NET), and Serilog/Log4Net/NLog/Application Insights all ship first-party MEL providers. `Benzene.Serilog`, `Benzene.Log4Net`, `Benzene.Microsoft.Logging` shrink to standard provider registration or are deleted.
- It structurally fixes problems 1 and 2: scopes become `ILogger.BeginScope`, which always flows to whatever provider the app configured; there is no separate context interface to forget to register.
- One translation layer, one level enum, and one fewer concept for users to learn.

**Interim non-breaking fix (Phase 1):** make each provider's `Add*` extension also register its `IBenzeneLogContext`, and replace the silent default with a console appender at `Information` plus a one-time startup warning ("no Benzene logger configured").

**Regardless of the rebase:** `ExceptionHandlerMiddleware` must `LogError(ex, ...)` (and set `Activity` status, see below) before mapping the exception to a result — never swallow silently.

---

## 4. Tracing and correlation

> **RESOLVED 2026-07-14 — the full target design shipped**, independently of the startup-unification
> arc (as this document itself anticipated: "The MEL rebase and the Activity/W3C work are independent
> of it and can proceed in parallel," §10). Verified: `Benzene.Diagnostics/BenzeneDiagnostics.cs` has
> the exact `ActivitySource`/`Meter` named `"Benzene"` proposed; `ActivityMiddlewareWrapper`/
> `ActivityMiddlewareDecorator<TContext>` auto-wrap every middleware (registered via `AddDiagnostics()`);
> `UseW3CTraceContext<TContext>()` extracts `traceparent`/`tracestate`; `UseBenzeneEnrichment<TContext>()`
> is the "one shared enrichment middleware" that replaces the AWS-only `WithRequestId`/`WithApplication`
> (now `[Obsolete]`) — it also covers `WithHttp()`, which is not mentioned as surviving anywhere.
> `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` exists for both `TracerProviderBuilder` and
> `MeterProviderBuilder`. `Benzene.Datadog`, `Benzene.Zipkin`, and `Benzene.Aws.XRay` are all deleted
> (confirmed absent from `src/`; the X-Ray deletion is also independently documented in
> `work/aws-roadmap-1.0.md`'s 2026-07-13 changelog). The metric names match exactly:
> `benzene.messages.processed`, `benzene.message.duration`. Two deviations from "keep `IProcessTimer`
> for one release": it wasn't scoped to one release — `IProcessTimer`/`IProcessTimerFactory` remain a
> supported, documented abstraction indefinitely (`ActivityProcessTimer` is the default implementation,
> plus `LoggingProcessTimer`/`DebugProcessTimer`/`CompositeProcessTimer` are still available, per
> `src/Benzene.Diagnostics/CLAUDE.md`) — a permanent compatibility layer, not a one-release bridge.
> Two genuine open gaps, per `Benzene.Diagnostics/CLAUDE.md`'s own documented limitations: (1)
> `traceparent` extraction is wired for HTTP-based transports only — SQS/SNS/Kafka/Event Hub inbound
> extraction "isn't implemented yet"; (2) `ExceptionHandlerMiddleware` (§3) logs the exception but does
> not set `Activity` status/record the exception on the current span.

### Current state

- **No `System.Diagnostics.Activity`, no W3C `traceparent` anywhere in `src/`.** Cross-service correlation is a custom `"correlationId"` header (`src/Benzene.Diagnostics/Correlation/`) manually forwarded by a client decorator.
- Distributed tracing is modeled through a custom `IProcessTimer`/`IProcessTimerFactory` (`src/Benzene.Diagnostics/Timers/`) with vendor backends: `Benzene.OpenTelemetry`, `Benzene.Datadog`, `Benzene.Zipkin`, `Benzene.Aws.XRay`. The OpenTelemetry package only wraps timers in spans via `TracerProvider.Default`; it emits no metrics or logs and propagates no context.
- **Three overlapping timing mechanisms**: `UseTimer(name)`, `TimerMiddleware` (callback form), and `TimerMiddlewareWrapper` (auto-wraps every middleware). Trace granularity depends entirely on how many `UseTimer("...")` calls the author sprinkled in — the AWS examples do this heavily, Azure not at all.

### Target design: Activity + OpenTelemetry as the primary story

- New core in `Benzene.Diagnostics`: a static `BenzeneDiagnostics.ActivitySource` ("Benzene") and **one** auto-wrapped `ActivityMiddleware` replacing all three timing mechanisms, starting an `Activity` per pipeline stage with tags `benzene.transport`, `benzene.topic`, `benzene.version`, `benzene.handler`.
- **W3C `traceparent` in and out**: transport adapters extract `traceparent`/`tracestate` from HTTP headers and message attributes (API Gateway, SQS/SNS, Kafka, Event Hubs) as the parent context; `Benzene.Clients.*` inject it on outbound calls. The custom `correlationId` header stays supported as a legacy fallback and is still emitted as a log scope; `UseCorrelationId()` becomes an `[Obsolete]` alias.
- `Benzene.OpenTelemetry` becomes the instrumentation package: `tracing.AddBenzeneInstrumentation()` (essentially `AddSource("Benzene")` + resource attributes) plus metrics (`benzene.messages.processed`, `benzene.message.duration`, tagged by topic/transport/result).
- **Delete `Benzene.Datadog`, `Benzene.Zipkin`, `Benzene.Aws.XRay` timer backends (breaking)** — all three vendors natively consume OTel/ADOT exporters. Keep `IProcessTimer` for one release as an adapter that opens an `Activity`.
- **Uniform enrichment keys on every platform**, attached to both log scopes and Activities: `invocationId` (from `IBenzeneInvocation`, unifying AWS `requestId` and the missing Azure invocation id), `traceId`/`spanId`, `topic`, `transport`, `handler`. One shared enrichment middleware replaces the AWS-only `WithRequestId`/`WithApplication`/`WithHttp` extensions.

---

## 5. Routing and handlers

> **Status 2026-07-14: item 2 done, items 1 and 3 still open.** `BENZ001` (duplicate topic) is
> live in `src/Benzene.CodeGen.SourceGenerators/MessageHandlerSourceGenerator.cs` — the
> `context.ReportDiagnostic(...)` call is no longer commented out. `BENZ002`/`BENZ003` were not
> added. `AddGeneratedMessageHandlers()` is still not referenced by anything outside the generator's
> own test (`test/Benzene.Core.Test/Autogen/CodeGen/SourceGenerator/MessageHandlerSourceGeneratorTest.cs`)
> — no doc or example wires it up; reflection scanning remains the only path anyone actually sees.
> `MapMessage`/`MapHttp` minimal-API-style registration (item 3) was not built — no matches anywhere
> in `src/`.

Keep `[Message(topic, version)]` + `IMessageHandler<TReq,TResp>` + `[HttpEndpoint]` as-is — this layer works and is the reason handlers are portable. Three improvements:

1. **Promote the source generator to the default path.** `MessageHandlerSourceGenerator` (`src/Benzene.CodeGen.SourceGenerators/`) already emits `AddGeneratedMessageHandlers()`, but nothing references it — docs and examples all use reflection scanning. Ship it as an analyzer reference of the platform meta-packages so templates/docs show the generated path (reflection stays as fallback). This is also a prerequisite for Lambda AOT/trimming.
2. **Re-enable compile-time diagnostics.** The duplicate-topic detection loop exists in the generator but its `ReportDiagnostic` call is commented out. Re-enable as error `BENZ001` (duplicate topic), and add `BENZ002` (handler not instantiable/registered) and `BENZ003` (`[HttpEndpoint]` route conflict). ✅ `BENZ001` done 2026-07-14 audit (see above); `BENZ002`/`BENZ003` still open.
3. **Optional minimal-API-style inline registration** for small services, built on the existing `MessageHandlerDefinition.CreateInstance` (already proven in `examples/Asp/Benzene.Example.Asp/Startup.cs`):

```csharp
app.UseMessageHandlers(r => r
    .MapMessage<CreateOrderRequest, OrderDto>("createOrder", "1.0",
        (req, IOrderService svc) => svc.CreateAsync(req))
    .MapHttp("POST", "/orders", "createOrder")
    .UseFluentValidation());
```

---

## 6. Schema and spec

> **Status 2026-07-14: the schema-generation gap is closed; the CLI/spec-retrieval gap is not.**
> `DefaultJsonSchemaProvider<TContext>` (`src/Benzene.JsonSchema/DefaultJsonSchemaProvider.cs`) is
> fully implemented — no longer a `null`-returning stub. It uses `Json.Schema.Generation`'s
> `JsonSchemaBuilder().FromType(...)` with `PropertyNameResolvers.CamelCase`, caches per request
> type in a `ConcurrentDictionary`, and resolves the handler via `IMessageHandlerDefinitionLookUp` —
> matching the target design's approach essentially exactly (it doesn't read the *active* `ISerializer`'s
> naming policy dynamically, it hardcodes camelCase, which happens to match STJ's default — a
> reasonable simplification, not a functional gap for the common case). Everything else in this
> section is **unchanged and still accurate**: `src/Benzene.CodeGen.Cli/Program.cs` still hand-rolls
> argument parsing (no `System.CommandLine` package reference anywhere in `src/Benzene.CodeGen.Cli*`);
> no `ISpecSource`/`FileSpecSource`/`HttpSpecSource`/`AssemblySpecSource`/`LambdaSpecSource`
> abstraction exists; `AwsLambdaSpecClient` (`src/Benzene.CodeGen.Cli.Core/Commands/Spec/AwsLambdaSpecClient.cs`)
> remains the only spec-fetch path, still AWS-only. `benzene spec export --assembly ...` was not
> built, despite the platform-neutral `BenzeneStartUp` prerequisite this section explicitly called
> out now being available (§2) — this is a real, actionable gap: the unification work needed to
> unblock it has landed, but the CLI work to take advantage of it hasn't started.

### Current state

- `DefaultJsonSchemaProvider<TContext>.Get(...)` **returns `null`** (`src/Benzene.JsonSchema/DefaultJsonSchemaProvider.cs`) — the default provider is a no-op stub, so generated OpenAPI/AsyncAPI documents lack real component schemas unless the user writes a custom provider. (The package's own CLAUDE.md claims "draft 7" generation that doesn't exist.) — ✅ **RESOLVED 2026-07-14**, see status note above; the package's CLAUDE.md drift ("draft 7") has also been fixed (no longer present in `src/Benzene.JsonSchema/CLAUDE.md`).
- Runtime spec generation (`src/Benzene.Schema.OpenApi/` — OpenAPI, AsyncAPI, custom "benzene" format) is exposed via a `UseSpec()` topic, which works in-process on any platform, but the `benzene` CLI's `spec` command can only fetch it from a **deployed AWS Lambda** (`AwsLambdaSpecClient`). There is no local, HTTP, or Azure path. — still true.
- The CLI (`src/Benzene.CodeGen.Cli/`) uses hand-rolled argument parsing; infrastructure codegen (Terraform, IAM, API Gateway) is AWS-only. — still true.

### Target design

- **Implement `DefaultJsonSchemaProvider`** with `Json.Schema.Generation` (JsonSchema.Net is already the dependency), generating from the handler's request/response types and honoring the active `ISerializer` naming policy (camelCase STJ default). Wire it into the OpenAPI `SpecBuilder` so specs have real schemas out of the box. — ✅ **DONE 2026-07-14.**
- **Single source of truth:** the `IMessageHandlerDefinition` + `IHttpEndpointDefinition` collections are the model; OpenAPI, AsyncAPI, markdown, terraform and client codegen read only that model (mostly true today — codify it).
- **Spec retrievable identically everywhere:**
  - Runtime: `UseSpec()` registers both the `"spec"` topic and — when `UseHttp` is present — `GET /spec`, on every platform.
  - Build time, no deployment needed: `benzene spec export --assembly bin/Release/net10.0/MyApp.dll --startup MyApp.StartUp --format openapi -o spec.json`. The CLI loads the assembly, runs `ConfigureServices`/`Configure` against a null host, and enumerates definitions. This is only possible once StartUp is platform-neutral (§2) — a concrete payoff of the unification. — ❌ **still not built**, even though its prerequisite (§2) now is.
  - CLI refactor: `ISpecSource` implementations (`FileSpecSource`, `HttpSpecSource`, `AssemblySpecSource`, existing Lambda client as `LambdaSpecSource`); replace hand-rolled parsing with **System.CommandLine** (`benzene spec export|fetch`, `benzene generate client|markdown|terraform`). — ❌ **still not done.**

---

## 7. Testing

> **Status 2026-07-14: a unified test host shipped, built on `BenzeneStartUp` as this section
> predicted, but with a different shape than the exact API sketched below, and only for 2 of 4
> platforms so far.** `BenzeneTestHost.Create<TStartUp>()` / `BenzeneTestHostBuilder<TStartUp>`
> (`src/Benzene.Testing/BenzeneTestHost.cs`) is real: `.WithServices(...)`, `.WithConfiguration(...)`
> (both single-key and bulk overloads — an addition beyond what this section proposed), then
> `.Build<THost>(factory)` runs the StartUp's `GetConfiguration`/`ConfigureServices` and hands off to
> a platform-specific factory. Platform packages provide the `Build*` extension that supplies that
> factory: `BuildAwsLambdaHost()` (`Benzene.Aws.Lambda.Core.TestHelpers`) and `BuildAzureFunctionApp()`
> (`Benzene.Azure.Function.Core.TestHelpers`) exist; **there is no `BuildWorkerHost`/ASP.NET
> equivalent yet**, so the "one test host for all platforms" goal is 2/4 platforms in practice. The
> actual shape deviates from the `host.SendMessageAsync(...)`/`host.SendHttpAsync(...)` sketch below:
> instead of one host type with uniform `Send*` methods, `Build<THost>` returns a platform-specific
> host (e.g. `IAwsLambdaEntryPoint`, `IAzureFunctionApp`), and each platform's `*.TestHelpers` package
> supplies its own `Send*Async` extensions on `IBenzeneTestHost`/the platform type (e.g.
> `SendBenzeneMessageAsync`, `SendApiGatewayAsync`, `SendSqsAsync`) — closer to the "envelope-level
> event builders feeding the same host" bullet below than to the single uniform `Send*` API in the
> code sample. The `MessageBuilder`/`HttpBuilder` duplication between `Benzene.Testing` and
> `Benzene.Tools` **is** resolved: `Benzene.Tools/MessageBuilder.cs` and `HttpBuilder.cs` are now
> entirely commented out in favor of `Benzene.Testing`'s versions (though left in place as
> commented-out files rather than deleted — a partial completion of "delete the dead commented-out
> files"; the fully-dead `Benzene.Core.Messages.TestHelpers/BenzeneTestHostExtensions.cs` file this
> section called out *was* deleted outright). `docs/testing-benzene.md` is now platform-neutral, with
> parallel AWS and Azure examples built on `BenzeneTestHost.Create<StartUp>()`.

### Current state

The `IBenzeneTestHost` abstraction (`src/Benzene.Abstractions/IBenzeneTestHost.cs`) is platform-neutral, but the full in-memory test host — `AwsLambdaBenzeneTestStartUp<TStartUp>` with `.WithServices()/.BuildHost()`, spinning your real StartUp in memory — exists **only for AWS** (`src/Benzene.Tools/Aws/`). Azure test helpers only build request objects; console/ASP.NET have nothing. `docs/testing-benzene.md` is AWS-only. `Benzene.Core.Messages.TestHelpers/BenzeneTestHostExtensions.cs` is entirely commented out.

### Target design

One test host for all platforms — possible once StartUp is shared:

```csharp
var host = BenzeneTestHost.Create<StartUp>(services => services.Replace<IOrderDbClient, FakeDb>());
var result = await host.SendMessageAsync(m => m.WithTopic("createOrder").WithBody(new CreateOrderRequest(...)));
var http   = await host.SendHttpAsync(h => h.Post("/orders").WithJsonBody(...));
```

- Lives in `Benzene.Testing`, against the existing `IBenzeneTestHost`; merge the `MessageBuilder`/`HttpBuilder` duplicates currently split between `Benzene.Testing` and `Benzene.Tools`.
- Envelope-level tests (raw API Gateway JSON, SQS batch shapes, Event Hub batches) stay in per-platform `*.TestHelpers` packages as event **builders** feeding the same host: `host.SendAwsEventAsync(ApiGatewayEvent.Post("/orders")...)`.
- Delete the dead commented-out files.

---

## 8. Packaging, versioning and CI

> **Status 2026-07-14: version-source/`Directory.Build.props`/CLAUDE.md-drift items resolved; package
> consolidation and CI coverage are NOT.** Verified: `Directory.Build.props` exists at the repo root
> and in `src/`. Root file sets `VersionPrefix` from `version.txt` (single version source — `0.0.2`
> today, no conflicting hardcoded `PackageVersion`s found anywhere in any `.csproj`), plus `Authors`/
> `PackageLicenseExpression`/`RepositoryUrl`, and defaults `IsPackable=false`. `src/Directory.Build.props`
> flips `IsPackable=true` for everything under `src/`. **Deviation from the proposal:** it does *not*
> set `TargetFramework`/`Nullable`/`LangVersion` centrally — those remain hardcoded per csproj (78
> csproj files still each declare `<TargetFramework>net10.0</TargetFramework>` individually). Also a
> deliberate, explicit deviation on `IsPackable`: rather than defaulting product packages on and
> gating `*.TestHelpers`/`Benzene.Tools`/`Benzene.Testing` off, `src/Directory.Build.props`'s own
> comment says "Everything under `src/` is a shippable package (including `*.TestHelpers`,
> `Benzene.Testing` and `Benzene.Tools`, which are user-facing test support)" — packing everything is
> the considered choice, not an oversight. `VERSIONING.md` is rewritten to the SemVer policy this
> section's target design called for. **Not done:** package consolidation — `src/` still has 79
> top-level packages (down from ~80 only because `Benzene.Aws.XRay`/`Benzene.Datadog`/`Benzene.Zipkin`
> were deleted per §4, not because of any `Abstractions.*`/`Core.*` merge — `Benzene.Abstractions.MessageHandlers`,
> `.Messages`, `.Middleware`, `.Pipelines`, `.Validation` and `Benzene.Core.MessageHandlers`, `.Messages`,
> `.Middleware` are all still separate packages). CI is unchanged: `build-benzene.yml` still only runs
> `test/Benzene.Core.Test` and `test/Benzene.Aws.Tests`; `Benzene.Integration.Test`, the new
> `Benzene.Grpc.Test`, and every `examples/*.Test` project are still not run by any workflow.
> `deploy-azure-example.yml` no longer exists at all (removed, not fixed); `deploy-asp-example.yml`
> still exists, still deploys via `Azure/functions-action@v1` for what is an ASP.NET Core app, and
> **still has the `./Examples/Asp/...` path-casing bug** this section called out (working-directory
> and package path both use the wrong case, which would break on a case-sensitive Linux runner).
> Serialization asymmetry is unchanged: `Benzene.NewtonsoftJson/JsonSerializer.cs` still writes with
> `CamelCasePropertyNamesContractResolver` but deserializes with a bare `new JsonSerializerSettings()`.

### Current state

- ~80 shippable packages; **no `Directory.Build.props`/`Directory.Build.targets`** — every csproj self-configures. No `PackageId` anywhere; no `IsPackable` gating, so `*.TestHelpers` and `Benzene.Tools` publish unintentionally. — ✅ `Directory.Build.props`/single version source done 2026-07-14; `IsPackable` gating deliberately *not* done the way proposed (see status note above).
- **Three conflicting version sources:** `version.txt` = 0.0.2, several csproj hardcode `<PackageVersion>0.0.1</PackageVersion>`, `VERSIONING.md` describes a 1.0.0 policy, and the deploy workflow overrides everything with a computed `-alpha` suffix. — ✅ resolved; `version.txt` (now `0.0.2`) is the sole source, no hardcoded csproj versions remain.
- CI (`.github/workflows/build-benzene.yml`) tests only `test/Benzene.Core.Test` and `test/Benzene.Aws.Tests`; `Benzene.Integration.Test` and all `examples/*.Test` projects are never exercised. `deploy-azure-example.yml` is entirely commented out and pins .NET 6; `deploy-asp-example.yml` deploys an ASP.NET app via the Azure *Functions* action. — still true (CI coverage), except `deploy-azure-example.yml` was deleted outright rather than fixed.
- Serialization is asymmetric: the STJ default (`src/Benzene.Core.MessageHandlers/Serialization/JsonSerializer.cs`) is camelCase + case-insensitive; the Newtonsoft adapter writes camelCase but reads with default settings; `src/` mixes both libraries. — still true, unchanged.

### Target design

- **`Directory.Build.props` at the repo root:** `TargetFramework`, `Nullable`, `LangVersion`, package metadata (authors/license/repo URL), `VersionPrefix` read from `version.txt`, and `IsPackable=false` by default with `src/Directory.Build.props` flipping it on for product packages (TestHelpers packable only as a deliberate decision, with consistent suffixes). Delete per-csproj versions; CI appends `-alpha.N` via `VersionSuffix`; rewrite `VERSIONING.md` to match. **One version source.** — ✅ **mostly done** (version source, metadata, `VERSIONING.md`); `TargetFramework`/`Nullable`/`LangVersion` centralization not done; `IsPackable` was flipped on for *everything* under `src/`, not gated to exclude TestHelpers/Tools as literally proposed here (a considered deviation, see status note).
- **Consolidation (~80 → ~35 packages):** merge the five `Benzene.Abstractions.*` into `Benzene.Abstractions`; merge `Benzene.Core.*` into `Benzene.Core`; delete Datadog/Zipkin/XRay (OTel replaces them) and the Azure in-process bits. — Datadog/Zipkin/XRay deletion ✅ done, and the Azure in-process bits ✅ done (§2); the `Abstractions.*`/`Core.*` package merges themselves ❌ **not done** — still 79 packages.
- **Meta-packages** — what docs and templates reference: `Benzene.Aws.Lambda`, `Benzene.Azure.Functions`, `Benzene.AspNet`, `Benzene.Worker`, each pulling the platform host, common transports, `Benzene.OpenTelemetry`, and the source generator. — ❌ not built; no such meta-packages exist in `src/`.
- **Serialization:** STJ is the contract; the Newtonsoft adapter mirrors the same naming/case-insensitivity settings on read and write; remove internal Newtonsoft usage from core/tools. — ❌ not done.
- **CI:** `dotnet test Benzene.sln` (full suite); delete or rewrite the dead/wrong deploy workflows; build every example per platform as smoke tests; fix path-casing (`./Examples/` vs `examples/`) that breaks Linux runners. — ❌ not done; `deploy-azure-example.yml` was deleted (partial credit) but `build-benzene.yml`'s test scope and `deploy-asp-example.yml`'s path-casing bug are unchanged.

---

## 9. Docs and templates

> **Status 2026-07-14: docs coverage substantially improved; `dotnet new` templates not built; CLAUDE.md
> drift is fixed in some packages but not others.** `docs/` now has real getting-started pages for
> Azure Functions (`azure-functions.md`), ASP.NET Core (`asp-net-core.md`), Worker Services
> (`getting-started-worker.md`), Kafka (`getting-started-kafka.md`), and gRPC (`getting-started-grpc.md`)
> — closing the "only for AWS" / "nothing for console/worker" / "no gRPC/Kafka pages" gaps. A new
> `docs/hosting.md` documents the unified `BenzeneStartUp` model directly (see §2). `docs/index.md`
> no longer lists Azure Functions as "Coming Soon" (it links `azure-functions.md` directly under a
> real "Azure" subsection); **"Client SDKs (Coming Soon)" is still there**, unresolved. There's still
> no dedicated Kubernetes-hosting page (ASP.NET Core/Worker docs cover the underlying hosting models
> Kubernetes would use, but nothing K8s-specific — manifests, probes, graceful shutdown). There is
> **still no documentation for the `benzene` dotnet CLI tool** — confirmed no CLI reference doc exists
> anywhere under `docs/`. `docs/testing-benzene.md` is now platform-neutral (§7). CLAUDE.md drift is
> **partially fixed**: `src/Benzene.JsonSchema/CLAUDE.md` no longer claims "draft 7" (now correctly
> says "draft 2020-12"); `src/Benzene.OpenTelemetry/CLAUDE.md` no longer claims "metrics collection"
> it doesn't do (accurately scoped to `AddSource`/`AddMeter` registration only). **But
> `src/Benzene.Tools/CLAUDE.md` is still stale** — it still describes the package as providing "CLI
> commands for scaffolding, code generation, project analysis" and "CLI scaffolding commands," which
> doesn't match what's actually in `src/Benzene.Tools/` today (test-host helpers:
> `AwsLambdaBenzeneTestHost`, `MessageBuilder`, `HttpBuilder`, `InlineStartUp` — no scaffolding CLI
> code at all). This is a real, still-open documentation-drift bug this review correctly flagged that
> didn't get swept up in whatever pass fixed the other two. `dotnet new` templates (`Benzene.Templates`)
> were not built — no `/templates` directory, no template package.

### Current state

`docs/` has a substantial getting-started **only for AWS**; the index lists Azure Functions and Client SDKs as "Coming Soon"; there is nothing for console/worker or Kubernetes hosting, no gRPC/Kafka pages despite working examples, and no documentation for the `benzene` dotnet tool. There are no `dotnet new` templates. Several per-package CLAUDE.md files describe features that don't exist (e.g. `Benzene.Tools` described as a scaffolding CLI; JsonSchema "draft 7"; OpenTelemetry "metrics collection").

### Target design

- **`Benzene.Templates`** (`dotnet new install Benzene.Templates`): `benzene-aws`, `benzene-azure`, `benzene-worker`, `benzene-web`. Each template = the north-star `Program` shim + the *identical* `StartUp` + one sample handler + validator + a test project using `BenzeneTestHost`, plus platform deploy scaffolding (SAM/terraform snippet, `host.json`, Dockerfile). — ❌ not built.
- **Docs as a matrix:** one shared "core concepts" section (handlers, pipeline, logging, tracing, testing, spec) plus four near-identical getting-started pages whose diff is literally `Program.cs`. Kill "Coming Soon"; add a CLI reference; rewrite `testing-benzene.md` platform-neutral; fix the CLAUDE.md drift. — ✅ four near-identical getting-started pages now exist and `testing-benzene.md` is platform-neutral; ❌ no CLI reference doc; "Coming Soon" reduced to just Client SDKs; CLAUDE.md drift fixed for `Benzene.JsonSchema`/`Benzene.OpenTelemetry` but **not** `Benzene.Tools` (still stale — see status note above).

---

## 10. Phased roadmap

Sequencing note: Phase 2's StartUp unification must land before the unified test host and the templates (both depend on a platform-neutral StartUp). The MEL rebase and the Activity/W3C work are independent of it and can proceed in parallel.

> **2026-07-14 audit note:** every row below was re-checked against the live codebase and given a
> **Status** column. Headline: **all of Phase 2 shipped except package consolidation**, most of
> Phase 1 shipped, and Phase 3 is almost entirely still open. The sequencing note above held up
> correctly in practice — the MEL rebase and Activity/W3C work did land independently and in full,
> as predicted.

### Phase 1 — quick wins (non-breaking)

| Item | Effort | Primary files | Status (2026-07-14) |
|---|---|---|---|
| `Directory.Build.props`, `IsPackable` gating, single version source | S | new `/Directory.Build.props`, `/src/Directory.Build.props`, `version.txt`, csproj cleanup, `VERSIONING.md` | ✅ Done, with a deliberate deviation on `IsPackable` gating (packs everything under `src/`, not just product packages) and `TargetFramework`/`Nullable`/`LangVersion` still not centralized — see §8. |
| Providers also register `IBenzeneLogContext`; console default appender + "no logger configured" warning | S | `src/Benzene.Serilog/`, `src/Benzene.Microsoft.Logging/`, `src/Benzene.Log4Net/`, `src/Benzene.Core/DI/Extensions.cs` | ✅ **Superseded** — this whole interim fix is moot because the full MEL rebase (Phase 2 item, below) shipped instead; `IBenzeneLogContext` and these three provider packages no longer exist. |
| `ExceptionHandlerMiddleware` logs errors | S | `src/Benzene.Core.Middleware/ExceptionHandlerMiddleware.cs` | ✅ Done — `logger.LogError(ex, ...)` before `onException(...)`. Does not set `Activity` status (see §4). |
| Re-enable duplicate-topic diagnostic (BENZ001) | S | `src/Benzene.CodeGen.SourceGenerators/MessageHandlerSourceGenerator.cs` | ✅ Done — `ReportDiagnostic` call is live. |
| Implement `DefaultJsonSchemaProvider` via `Json.Schema.Generation`; wire into spec | M | `src/Benzene.JsonSchema/`, `src/Benzene.Schema.OpenApi/` | ✅ Done — see §6. |
| CLI on System.CommandLine + File/Http spec sources | M | `src/Benzene.CodeGen.Cli/`, `src/Benzene.CodeGen.Cli.Core/` | ❌ Not started — still hand-rolled parsing, still AWS-Lambda-only spec fetch. |
| CI runs full test suite; delete dead workflows; fix "Kakfa" typos, dead files, misnamed Autofac file | S | `.github/workflows/`, `examples/Kafka/`, `src/Benzene.Autofac/`, dead files | ⚠️ **Partial** — misnamed Autofac file ✅ fixed (now `AutofacDependencyInjectionAdapter.cs`); `deploy-azure-example.yml` ✅ removed. "Kakfa" typo ❌ still present throughout `examples/Kafka/Benzene.Examples.Kakfa*` and even in `docs/getting-started-kafka.md`/`docs/getting-started-worker.md`/`CHANGELOG.md`. CI full-suite coverage ❌ still not done (Integration/Grpc/example tests still unrun). |

### Phase 2 — unification (breaking; the 0.x arc)

| Item | Effort | Primary files | Status (2026-07-14) |
|---|---|---|---|
| `BenzeneStartUp` + `IBenzeneApplicationBuilder` + `IBenzeneInvocation` | L | `src/Benzene.Abstractions.Pipelines/Hosting/`, `src/Benzene.Core.Middleware/`, new `Benzene.Hosting` | ✅ Done — lives in `Benzene.Microsoft.Dependencies`/`Benzene.Abstractions.Pipelines`/`Benzene.Core.Middleware`, not a new `Benzene.Hosting` package; see §2 for the `IHostingPlatform`/exception-vs-no-op deviations. |
| AWS `AwsLambdaHost<TStartUp>` + `RunAwsLambdaAsync`; `UseApiGateway` → `UseHttp` (obsolete forwarders) | M | `src/Benzene.Aws.Lambda.Core/`, `src/Benzene.Aws.Lambda.ApiGateway/` | ⚠️ **Partial** — `AwsLambdaHost<TStartUp>` ✅ done exactly as specified; `RunAwsLambdaAsync` ❌ not built; `UseApiGateway` → `UseHttp` rename ❌ not done at all (no rename, no forwarder — AWS's HTTP verb is still `UseApiGateway` only). |
| Azure isolated-worker rewrite: `UseBenzene<TStartUp>()`, delete `IWebJobsStartup` path, invocation-id enrichment | L | `src/Benzene.Azure.Function.Core/`, `examples/Azure/` | ✅ Done — `Microsoft.Azure.WebJobs` fully gone from `src/`; see `work/phase2-azure-isolated-worker-spec.md`. |
| Worker `UseBenzene<TStartUp>()` absorbing SelfHost/HostedService startups | M | `src/Benzene.SelfHost/`, `src/Benzene.HostedService/` | ⚠️ **Partial** — `IHostBuilder.UseBenzene<TStartUp>()` ✅ done; "absorbing" is inaccurate — `BenzeneWorkerStartup`/`BenzeneHostedServiceStartup` were kept, not removed/absorbed (additive, per the phase-2 spec's guardrails). |
| ASP.NET `builder.UseBenzene<TStartUp>()` / `app.UseBenzene()` | M | `src/Benzene.AspNet.Core/BenzeneExtensions.cs` | ✅ Done — this item was explicitly "out of scope" in the original phase-2 spec's first slice but shipped anyway; see §2. |
| MEL rebase: delete `IBenzeneLogger` stack | L | `src/Benzene.Abstractions/Logging/`, `src/Benzene.Core/`, provider packages, call sites | ✅ Done — see §3; independent of the StartUp work, exactly as the sequencing note predicted. |
| `ActivitySource` + `ActivityMiddleware`; collapse timers; `traceparent` extract/inject; delete Datadog/Zipkin/XRay | M | `src/Benzene.Diagnostics/`, `src/Benzene.OpenTelemetry/`, transport adapters, `src/Benzene.Clients*` | ✅ Done, with two remaining gaps — see §4 (non-HTTP `traceparent` extraction, `Activity` status on exception). |
| Unified `BenzeneTestHost.Create<TStartUp>()`; merge Testing/Tools | M | `src/Benzene.Testing/`, `src/Benzene.Tools/`, `*.TestHelpers` | ⚠️ **Partial** — `BenzeneTestHost.Create<TStartUp>()` ✅ done for AWS + Azure only (no Worker/ASP.NET `Build*` yet); `MessageBuilder`/`HttpBuilder` duplication ✅ resolved (Tools' copies commented out); packages themselves were not merged, `Benzene.Testing` and `Benzene.Tools` remain separate. |
| Package consolidation (Abstractions.*, Core.* merges) | M | csproj graph | ❌ **Not started** — still 79 separate packages under `src/`; see §8. |

### Phase 3 — polish (toward 1.0)

| Item | Effort | Primary files | Status (2026-07-14) |
|---|---|---|---|
| Meta-packages ×4 | S | 4 new csproj | ❌ Not started. |
| `benzene spec export --assembly` | M | `src/Benzene.CodeGen.Cli/`, `src/Benzene.Schema.OpenApi/` | ❌ Not started, despite its stated prerequisite (platform-neutral StartUp) now being available — see §6. |
| `dotnet new` templates ×4 with test projects | M | new `/templates` | ❌ Not started. |
| Minimal-API-style `MapMessage`/`MapHttp` | M | `src/Benzene.Core.MessageHandlers/` | ❌ Not started. |
| Source generator default in meta-packages; BENZ002/003 | M | `src/Benzene.CodeGen.SourceGenerators/` | ⚠️ **Partial** — `BENZ001` shipped in Phase 1 instead (see above); `BENZ002`/`BENZ003` and "default in meta-packages" (no meta-packages exist yet) both ❌ not started. |
| Docs parity matrix + CLI docs + platform-neutral testing doc | M | `/docs` | ⚠️ **Partial** — 4 near-identical getting-started pages ✅ shipped, `testing-benzene.md` ✅ platform-neutral; CLI docs ❌ still missing; "Coming Soon" reduced to just Client SDKs (Azure Functions resolved) — see §9. |
| STJ-first serialization; mirrored Newtonsoft settings | M | `src/Benzene.Core.MessageHandlers/Serialization/`, `src/Benzene.NewtonsoftJson/` | ❌ Not started — Newtonsoft read/write asymmetry unchanged, see §8. |

---

## 11. Summary of breaking changes proposed

All acceptable pre-1.0, each with a one-release `[Obsolete]` bridge where feasible:

1. `AwsLambdaStartUp` / `AzureFunctionStartUp` / `BenzeneWorkerStartup` replaced by `BenzeneStartUp` + per-platform hosts. — ⚠️ **Did not happen as a replacement.** `BenzeneStartUp` + per-platform hosts shipped, but purely additively: `AwsLambdaStartUp`, `BenzeneWorkerStartup`, `BenzeneHostedServiceStartup` are all still present, unchanged, undeprecated. Only Azure's old `IWebJobsStartup`/`AzureFunctionStartUp` path was actually deleted (item 3, below) — Azure is the one platform where this line item is literally true.
2. `AwsLambdaStartUp<TContainer>` / `IDependencyInjectionAdapter` constructor generic replaced by `IServiceProviderFactory<T>`. — ❌ **Not done.** Both types still exist unchanged; no `IServiceProviderFactory<T>` integration exists anywhere in `src/`; Autofac support for `BenzeneStartUp`-based apps was not built.
3. Azure in-process (`IWebJobsStartup`) path deleted; isolated worker only. — ✅ **Done**, verified via repo-wide grep for `Microsoft.Azure.WebJobs` returning zero hits under `src/`.
4. `UseApiGateway`/`UseAspNet` renamed to `UseHttp` (forwarders provided). — ⚠️ **Half done, no forwarders.** `UseAspNet` was in fact renamed straight to `UseHttp` (grep for `UseAspNet` in `src/Benzene.AspNet.Core/` today returns zero hits) — but with no `[Obsolete]` forwarder left behind, a harder break than proposed. `UseApiGateway` (AWS) was **not** renamed at all — still the only HTTP verb name for the AWS host, no `UseHttp` alias either way.
5. `IBenzeneLogger` stack replaced by `Microsoft.Extensions.Logging`. — ✅ **Done**, fully, no bridge/forwarder needed since the old types are gone outright (no `[Obsolete]` shim — a harder break than "each with a one-release `[Obsolete]` bridge where feasible" suggested, but consistent with pre-1.0 alpha status).
6. `Benzene.Datadog`, `Benzene.Zipkin`, `Benzene.Aws.XRay` deleted in favor of OpenTelemetry exporters; timer mechanisms collapsed into `ActivityMiddleware`. — ✅ **Done**, all three packages confirmed absent from `src/`; `ActivityMiddlewareWrapper` is the collapsed mechanism, `IProcessTimer` kept as a permanent (not one-release) compatibility adapter — see §4.
7. `Benzene.Abstractions.*` / `Benzene.Core.*` package merges. — ❌ **Not done** — still 79 separate packages under `src/`, see §8.

The measure of success: the *same* `StartUp.cs`, the same handler, the same test file, and near-identical docs pages on all four platforms — with only `Program.cs` differing.

> **2026-07-14 verdict:** this measure of success is **substantially achieved** — one `BenzeneStartUp`
> subclass genuinely runs unchanged on AWS Lambda, the generic host/worker, ASP.NET Core, and Azure
> Functions isolated-worker (verified via `test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs` and
> `AspNetUnifiedStartUpTest.cs`, and documented end-to-end in `docs/hosting.md`). "The same test file"
> is true for 2 of 4 platforms today (AWS + Azure have `BenzeneTestHost` `Build*` extensions; Worker/
> ASP.NET don't yet — §7). "Near-identical docs pages" is true (§9). What's notably different from
> this section's framing: the way there was **additive, not breaking** — of the 7 breaking changes
> listed above, only 3 (Azure's in-process deletion, the logging rebase, the observability-vendor
> deletions) actually happened as breaking changes; the other 4 either didn't happen (Autofac/
> `IServiceProviderFactory`, package consolidation) or happened as pure additions alongside the old
> API surface rather than replacements of it (StartUp types, the `UseHttp` rename). For a pre-1.0
> alpha library where this document explicitly says breaking changes are "acceptable ... where they
> buy consistency," this is a more conservative outcome than proposed — worth a deliberate decision
> (documented here, or in a follow-up) on whether the old, now-parallel StartUp APIs should be
> formally deprecated before 1.0, or whether keeping both is the intended long-term shape.
