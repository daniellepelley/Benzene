# Cross-Platform Design Review

**Goal:** Benzene should be very easy to use and offer developers the *same* experience whichever platform they run on — AWS Lambda, Azure Functions, a standalone console app, or hosted in Kubernetes.

This review assesses the current design against that goal and proposes a target design plus a phased roadmap. It covers startup, logging, tracing, routing, schema/spec, testing, tooling, packaging, CI and docs. All findings were verified against the code; file paths are cited throughout. Since the library is pre-1.0 (alpha packages), breaking changes are treated as acceptable where they buy consistency, and are flagged **(breaking)**.

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

### Current state

All platforms nominally share one contract, `IStartUp<TContainer, TConfiguration, TAppBuilder>` (`src/Benzene.Abstractions.Pipelines/Hosting/IStartUp.cs`), but each platform closes it with different names, different builder types, and a different entry-point model:

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

Rationale for closing the generics:

- `TConfiguration` is already `IConfiguration` on every platform — the generic buys nothing.
- `TContainer` should be fixed to `IServiceCollection`. Third-party containers plug in underneath via the standard `IServiceProviderFactory<T>` pattern (as ASP.NET Core does), replacing the `IDependencyInjectionAdapter<TContainer>` constructor generic. **(breaking:** `AwsLambdaStartUp<TContainer>` and the Autofac example base go away; Autofac support becomes `UseServiceProviderFactory(new AutofacServiceProviderFactory())` on any host).
- `TAppBuilder` becomes `IBenzeneApplicationBuilder` — a merge of what `AzureFunctionAppBuilder`, `AspApplicationBuilder` and `IBenzeneWorkerStartup` already are (all three already expose `Create<TContext>()` / `Add(...)` / `Register(...)`, so this is a rename/merge, not new machinery):

```csharp
public interface IBenzeneApplicationBuilder : IRegisterDependency
{
    IMiddlewarePipelineBuilder<TContext> Create<TContext>();
    void Add(Func<IServiceResolverFactory, IEntryPointMiddlewareApplication> func);
    IHostingPlatform Platform { get; } // "AwsLambda" | "AzureFunctions" | "AspNet" | "Worker"
}
```

Transport verbs stay extension methods living in platform packages (`UseSqs` only exists when `Benzene.Aws.Lambda.Sqs` is referenced). A verb that doesn't apply to the current `Platform` throws a clear `BenzeneHostingException` ("UseSqs requires the AWS Lambda host") at build time rather than silently doing nothing.

**Uniform verb vocabulary (breaking renames, with `[Obsolete]` forwarders for one release):**

| Logical transport | Today | Target |
|---|---|---|
| HTTP | `UseApiGateway` / `UseHttp` / `UseAspNet` | `UseHttp(...)` everywhere |
| Queues/streams | `UseSqs`, `UseSns`, `UseKafka`, `UseEventHub` | unchanged (already consistent) |
| Direct invoke | `UseBenzeneMessage` | unchanged |
| Handlers | `UseMessageHandlers(r => r.UseFluentValidation())` | unchanged — protect this |

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

(`AwsLambdaHost<TStartUp>` is today's `AwsLambdaStartUp` constructor/pipeline-caching logic moved into a host class that instantiates `TStartUp`. Also offer `await BenzeneHost.RunAwsLambdaAsync<StartUp>();` for the RuntimeSupport/executable style.)

Azure Functions — isolated worker only; delete the EOL in-process `IWebJobsStartup` path **(breaking)**:

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(w => w.UseBenzene()) // worker middleware dispatches into the pipeline
    .UseBenzene<StartUp>()
    .Build();
host.Run();
```

The isolated-worker middleware replaces the scoped `IAzureFunctionApp` indirection, so trigger functions become one-line pass-throughs (or disappear for HTTP via a catch-all proxy function).

Console / Kubernetes worker (absorbs `BenzeneWorkerStartup`/`BenzeneHostedServiceStartup`):

```csharp
await Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build().RunAsync();
```

ASP.NET / Kubernetes HTTP (implemented on top of the existing `AspApplicationBuilder`):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();  // GetConfiguration + ConfigureServices
var app = builder.Build();
app.UseBenzene();               // StartUp.Configure against an AspNet-backed builder
app.Run();
```

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

A Lambda-only handler calls `invocation.GetFeature<ILambdaContext>()`; portable handlers never touch it. This replaces the current situation where `AwsEventStreamContext` carries `ILambdaContext` but Azure/ASP.NET expose nothing analogous.

---

## 3. Logging

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

Keep `[Message(topic, version)]` + `IMessageHandler<TReq,TResp>` + `[HttpEndpoint]` as-is — this layer works and is the reason handlers are portable. Three improvements:

1. **Promote the source generator to the default path.** `MessageHandlerSourceGenerator` (`src/Benzene.CodeGen.SourceGenerators/`) already emits `AddGeneratedMessageHandlers()`, but nothing references it — docs and examples all use reflection scanning. Ship it as an analyzer reference of the platform meta-packages so templates/docs show the generated path (reflection stays as fallback). This is also a prerequisite for Lambda AOT/trimming.
2. **Re-enable compile-time diagnostics.** The duplicate-topic detection loop exists in the generator but its `ReportDiagnostic` call is commented out. Re-enable as error `BENZ001` (duplicate topic), and add `BENZ002` (handler not instantiable/registered) and `BENZ003` (`[HttpEndpoint]` route conflict).
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

### Current state

- `DefaultJsonSchemaProvider<TContext>.Get(...)` **returns `null`** (`src/Benzene.JsonSchema/DefaultJsonSchemaProvider.cs`) — the default provider is a no-op stub, so generated OpenAPI/AsyncAPI documents lack real component schemas unless the user writes a custom provider. (The package's own CLAUDE.md claims "draft 7" generation that doesn't exist.)
- Runtime spec generation (`src/Benzene.Schema.OpenApi/` — OpenAPI, AsyncAPI, custom "benzene" format) is exposed via a `UseSpec()` topic, which works in-process on any platform, but the `benzene` CLI's `spec` command can only fetch it from a **deployed AWS Lambda** (`AwsLambdaSpecClient`). There is no local, HTTP, or Azure path.
- The CLI (`src/Benzene.CodeGen.Cli/`) uses hand-rolled argument parsing; infrastructure codegen (Terraform, IAM, API Gateway) is AWS-only.

### Target design

- **Implement `DefaultJsonSchemaProvider`** with `Json.Schema.Generation` (JsonSchema.Net is already the dependency), generating from the handler's request/response types and honoring the active `ISerializer` naming policy (camelCase STJ default). Wire it into the OpenAPI `SpecBuilder` so specs have real schemas out of the box.
- **Single source of truth:** the `IMessageHandlerDefinition` + `IHttpEndpointDefinition` collections are the model; OpenAPI, AsyncAPI, markdown, terraform and client codegen read only that model (mostly true today — codify it).
- **Spec retrievable identically everywhere:**
  - Runtime: `UseSpec()` registers both the `"spec"` topic and — when `UseHttp` is present — `GET /spec`, on every platform.
  - Build time, no deployment needed: `benzene spec export --assembly bin/Release/net10.0/MyApp.dll --startup MyApp.StartUp --format openapi -o spec.json`. The CLI loads the assembly, runs `ConfigureServices`/`Configure` against a null host, and enumerates definitions. This is only possible once StartUp is platform-neutral (§2) — a concrete payoff of the unification.
  - CLI refactor: `ISpecSource` implementations (`FileSpecSource`, `HttpSpecSource`, `AssemblySpecSource`, existing Lambda client as `LambdaSpecSource`); replace hand-rolled parsing with **System.CommandLine** (`benzene spec export|fetch`, `benzene generate client|markdown|terraform`).

---

## 7. Testing

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

### Current state

- ~80 shippable packages; **no `Directory.Build.props`/`Directory.Build.targets`** — every csproj self-configures. No `PackageId` anywhere; no `IsPackable` gating, so `*.TestHelpers` and `Benzene.Tools` publish unintentionally.
- **Three conflicting version sources:** `version.txt` = 0.0.2, several csproj hardcode `<PackageVersion>0.0.1</PackageVersion>`, `VERSIONING.md` describes a 1.0.0 policy, and the deploy workflow overrides everything with a computed `-alpha` suffix.
- CI (`.github/workflows/build-benzene.yml`) tests only `test/Benzene.Core.Test` and `test/Benzene.Aws.Tests`; `Benzene.Integration.Test` and all `examples/*.Test` projects are never exercised. `deploy-azure-example.yml` is entirely commented out and pins .NET 6; `deploy-asp-example.yml` deploys an ASP.NET app via the Azure *Functions* action.
- Serialization is asymmetric: the STJ default (`src/Benzene.Core.MessageHandlers/Serialization/JsonSerializer.cs`) is camelCase + case-insensitive; the Newtonsoft adapter writes camelCase but reads with default settings; `src/` mixes both libraries.

### Target design

- **`Directory.Build.props` at the repo root:** `TargetFramework`, `Nullable`, `LangVersion`, package metadata (authors/license/repo URL), `VersionPrefix` read from `version.txt`, and `IsPackable=false` by default with `src/Directory.Build.props` flipping it on for product packages (TestHelpers packable only as a deliberate decision, with consistent suffixes). Delete per-csproj versions; CI appends `-alpha.N` via `VersionSuffix`; rewrite `VERSIONING.md` to match. **One version source.**
- **Consolidation (~80 → ~35 packages):** merge the five `Benzene.Abstractions.*` into `Benzene.Abstractions`; merge `Benzene.Core.*` into `Benzene.Core`; delete Datadog/Zipkin/XRay (OTel replaces them) and the Azure in-process bits.
- **Meta-packages** — what docs and templates reference: `Benzene.Aws.Lambda`, `Benzene.Azure.Functions`, `Benzene.AspNet`, `Benzene.Worker`, each pulling the platform host, common transports, `Benzene.OpenTelemetry`, and the source generator.
- **Serialization:** STJ is the contract; the Newtonsoft adapter mirrors the same naming/case-insensitivity settings on read and write; remove internal Newtonsoft usage from core/tools.
- **CI:** `dotnet test Benzene.sln` (full suite); delete or rewrite the dead/wrong deploy workflows; build every example per platform as smoke tests; fix path-casing (`./Examples/` vs `examples/`) that breaks Linux runners.

---

## 9. Docs and templates

### Current state

`docs/` has a substantial getting-started **only for AWS**; the index lists Azure Functions and Client SDKs as "Coming Soon"; there is nothing for console/worker or Kubernetes hosting, no gRPC/Kafka pages despite working examples, and no documentation for the `benzene` dotnet tool. There are no `dotnet new` templates. Several per-package CLAUDE.md files describe features that don't exist (e.g. `Benzene.Tools` described as a scaffolding CLI; JsonSchema "draft 7"; OpenTelemetry "metrics collection").

### Target design

- **`Benzene.Templates`** (`dotnet new install Benzene.Templates`): `benzene-aws`, `benzene-azure`, `benzene-worker`, `benzene-web`. Each template = the north-star `Program` shim + the *identical* `StartUp` + one sample handler + validator + a test project using `BenzeneTestHost`, plus platform deploy scaffolding (SAM/terraform snippet, `host.json`, Dockerfile).
- **Docs as a matrix:** one shared "core concepts" section (handlers, pipeline, logging, tracing, testing, spec) plus four near-identical getting-started pages whose diff is literally `Program.cs`. Kill "Coming Soon"; add a CLI reference; rewrite `testing-benzene.md` platform-neutral; fix the CLAUDE.md drift.

---

## 10. Phased roadmap

Sequencing note: Phase 2's StartUp unification must land before the unified test host and the templates (both depend on a platform-neutral StartUp). The MEL rebase and the Activity/W3C work are independent of it and can proceed in parallel.

### Phase 1 — quick wins (non-breaking)

| Item | Effort | Primary files |
|---|---|---|
| `Directory.Build.props`, `IsPackable` gating, single version source | S | new `/Directory.Build.props`, `/src/Directory.Build.props`, `version.txt`, csproj cleanup, `VERSIONING.md` |
| Providers also register `IBenzeneLogContext`; console default appender + "no logger configured" warning | S | `src/Benzene.Serilog/`, `src/Benzene.Microsoft.Logging/`, `src/Benzene.Log4Net/`, `src/Benzene.Core/DI/Extensions.cs` |
| `ExceptionHandlerMiddleware` logs errors | S | `src/Benzene.Core.Middleware/ExceptionHandlerMiddleware.cs` |
| Re-enable duplicate-topic diagnostic (BENZ001) | S | `src/Benzene.CodeGen.SourceGenerators/MessageHandlerSourceGenerator.cs` |
| Implement `DefaultJsonSchemaProvider` via `Json.Schema.Generation`; wire into spec | M | `src/Benzene.JsonSchema/`, `src/Benzene.Schema.OpenApi/` |
| CLI on System.CommandLine + File/Http spec sources | M | `src/Benzene.CodeGen.Cli/`, `src/Benzene.CodeGen.Cli.Core/` |
| CI runs full test suite; delete dead workflows; fix "Kakfa" typos, dead files, misnamed Autofac file | S | `.github/workflows/`, `examples/Kafka/`, `src/Benzene.Autofac/`, dead files |

### Phase 2 — unification (breaking; the 0.x arc)

| Item | Effort | Primary files |
|---|---|---|
| `BenzeneStartUp` + `IBenzeneApplicationBuilder` + `IBenzeneInvocation` | L | `src/Benzene.Abstractions.Pipelines/Hosting/`, `src/Benzene.Core.Middleware/`, new `Benzene.Hosting` |
| AWS `AwsLambdaHost<TStartUp>` + `RunAwsLambdaAsync`; `UseApiGateway` → `UseHttp` (obsolete forwarders) | M | `src/Benzene.Aws.Lambda.Core/`, `src/Benzene.Aws.Lambda.ApiGateway/` |
| Azure isolated-worker rewrite: `UseBenzene<TStartUp>()`, delete `IWebJobsStartup` path, invocation-id enrichment | L | `src/Benzene.Azure.Function.Core/`, `examples/Azure/` |
| Worker `UseBenzene<TStartUp>()` absorbing SelfHost/HostedService startups | M | `src/Benzene.SelfHost/`, `src/Benzene.HostedService/` |
| ASP.NET `builder.UseBenzene<TStartUp>()` / `app.UseBenzene()` | M | `src/Benzene.AspNet.Core/BenzeneExtensions.cs` |
| MEL rebase: delete `IBenzeneLogger` stack | L | `src/Benzene.Abstractions/Logging/`, `src/Benzene.Core/`, provider packages, call sites |
| `ActivitySource` + `ActivityMiddleware`; collapse timers; `traceparent` extract/inject; delete Datadog/Zipkin/XRay | M | `src/Benzene.Diagnostics/`, `src/Benzene.OpenTelemetry/`, transport adapters, `src/Benzene.Clients*` |
| Unified `BenzeneTestHost.Create<TStartUp>()`; merge Testing/Tools | M | `src/Benzene.Testing/`, `src/Benzene.Tools/`, `*.TestHelpers` |
| Package consolidation (Abstractions.*, Core.* merges) | M | csproj graph |

### Phase 3 — polish (toward 1.0)

| Item | Effort | Primary files |
|---|---|---|
| Meta-packages ×4 | S | 4 new csproj |
| `benzene spec export --assembly` | M | `src/Benzene.CodeGen.Cli/`, `src/Benzene.Schema.OpenApi/` |
| `dotnet new` templates ×4 with test projects | M | new `/templates` |
| Minimal-API-style `MapMessage`/`MapHttp` | M | `src/Benzene.Core.MessageHandlers/` |
| Source generator default in meta-packages; BENZ002/003 | M | `src/Benzene.CodeGen.SourceGenerators/` |
| Docs parity matrix + CLI docs + platform-neutral testing doc | M | `/docs` |
| STJ-first serialization; mirrored Newtonsoft settings | M | `src/Benzene.Core.MessageHandlers/Serialization/`, `src/Benzene.NewtonsoftJson/` |

---

## 11. Summary of breaking changes proposed

All acceptable pre-1.0, each with a one-release `[Obsolete]` bridge where feasible:

1. `AwsLambdaStartUp` / `AzureFunctionStartUp` / `BenzeneWorkerStartup` replaced by `BenzeneStartUp` + per-platform hosts.
2. `AwsLambdaStartUp<TContainer>` / `IDependencyInjectionAdapter` constructor generic replaced by `IServiceProviderFactory<T>`.
3. Azure in-process (`IWebJobsStartup`) path deleted; isolated worker only.
4. `UseApiGateway`/`UseAspNet` renamed to `UseHttp` (forwarders provided).
5. `IBenzeneLogger` stack replaced by `Microsoft.Extensions.Logging`.
6. `Benzene.Datadog`, `Benzene.Zipkin`, `Benzene.Aws.XRay` deleted in favor of OpenTelemetry exporters; timer mechanisms collapsed into `ActivityMiddleware`.
7. `Benzene.Abstractions.*` / `Benzene.Core.*` package merges.

The measure of success: the *same* `StartUp.cs`, the same handler, the same test file, and near-identical docs pages on all four platforms — with only `Program.cs` differing.
