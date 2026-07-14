# Work spec: Phase 2 slice 1 — platform-neutral StartUp (additive)

**Status: implemented and merged to `main` — every type below exists exactly as specified**
(verified 2026-07-14: `IBenzeneApplicationBuilder`, `BenzeneApplicationBuilder`, `BenzeneStartUp`,
`AwsLambdaHost<TStartUp>`, `AwsLambdaApplicationBuilder`/`UseAwsLambda`, `WorkerApplicationBuilder`/
`UseWorker`, `IHostBuilder.UseBenzene<TStartUp>()` all present with the signatures specified below;
`test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs` covers the shared-StartUp/cross-platform-no-op
scenarios this spec called for). `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` is green
(674 passed, 4 skipped). The three "follow-up slices (do NOT start)" listed at the bottom of this
document have **also** since shipped — see `work/phase2-azure-isolated-worker-spec.md` (Azure
isolated-worker rewrite, done) and `test/Benzene.Core.Test/Hosting/AspNetUnifiedStartUpTest.cs` /
`BenzeneInvocationTest.cs` (ASP.NET `UseHttp` adapter and the `IBenzeneInvocation` feature bag, both
done). This document is kept for historical/design record only; no further action needed.

Executable work order for the first slice of the startup unification described in
`work/cross-platform-design-review.md` §2. All research is done; this document contains
every signature and decision needed. **Everything in this slice is additive — do not
modify or remove any existing public type.** Target branch:
`claude/benzene-cross-platform-design-ra60co` (push there, then fast-forward `main`).

## Goal

One `StartUp` class, written once against a platform-neutral builder, runs unchanged on
(a) AWS Lambda and (b) the .NET generic host (console/Kubernetes worker). Azure and
ASP.NET adapters are LATER slices — out of scope here. The unified `UseHttp` verb is
also out of scope (needs a shared HTTP context design); platform-scoped blocks
(`UseAwsLambda(...)`, `UseWorker(...)`) are the bridge for now.

## Existing types you will build on (verbatim, verified)

- `IStartUp<TContainer, TConfiguration, TAppBuilder>` — `src/Benzene.Abstractions.Pipelines/Hosting/IStartUp.cs`:
  `GetConfiguration()`, `ConfigureServices(TContainer, TConfiguration)`, `Configure(TAppBuilder, TConfiguration)`.
- `IMiddlewarePipelineBuilder<TContext> : IRegisterDependency` — `src/Benzene.Abstractions.Middleware/`:
  `Use(Func<IServiceResolver, IMiddleware<TContext>>)`, `Create<TNewContext>()`, `Build()`.
  `IRegisterDependency` has `void Register(Action<IBenzeneServiceContainer> action)`.
- `MiddlewarePipelineBuilder<TContext>` — `src/Benzene.Core.Middleware/`; ctor takes `IBenzeneServiceContainer`.
- `IBenzeneWorker` — `src/Benzene.Abstractions.Pipelines/Hosting/IBenzeneWorker.cs`: `StartAsync/StopAsync(CancellationToken)`.
- `IBenzeneWorkerStartup : IRegisterDependency` — `src/Benzene.SelfHost/IBenzeneWorkerStartup.cs`:
  `void Add(Func<IServiceResolverFactory, IBenzeneWorker> func)`, `Create<TNewContext>()`,
  `IBenzeneWorker Create(IServiceResolverFactory)`. Implemented by `BenzeneWorkerStartup2`
  (`src/Benzene.SelfHost/BenzeneWorkerBuilder.cs`) which composes workers via `CompositeBenzeneWorker`.
- `BenzeneHostedServiceAdapter : IHostedService` — `src/Benzene.HostedService/BenzeneHostedServiceStartup.cs` — wraps an `IBenzeneWorker`.
- AWS: `AwsLambdaEntryPoint` (`src/Benzene.Aws.Lambda.Core/`) ctor takes
  `(IMiddlewarePipeline<AwsEventStreamContext>, IServiceResolverFactory)`; `IAwsLambdaEntryPoint`
  has `Task<Stream> FunctionHandlerAsync(Stream, ILambdaContext)` and is `IDisposable`.
  See `InlineAwsLambdaStartUp.Build()` in the same folder for the exact construction pattern.
- DI: `MicrosoftBenzeneServiceContainer(IServiceCollection)` and
  `MicrosoftServiceResolverFactory(IServiceCollection | IServiceProvider)` — `src/Benzene.Microsoft.Dependencies/`.
  Note `Extensions.UsingBenzene` calls `services.AddLogging()` — replicate that in new hosts.
- Test helpers: `test/Benzene.Core.Test/` is the test project (xUnit + Moq).
  `Benzene.Tools.Aws/AwsLambdaBenzeneTestHost` + `SendBenzeneMessageAsync(MessageBuilder…)` shows how a
  BenzeneMessage invocation is serialized to the Lambda stream — copy that mechanism for the AWS host test
  (read `src/Benzene.Tools/Aws/*.cs` before writing the test).

## New types to create (exact shapes)

### 1. `src/Benzene.Abstractions.Pipelines/Hosting/IBenzeneApplicationBuilder.cs`
```csharp
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.Hosting;

/// <summary>Platform-neutral application builder passed to <see cref="BenzeneStartUp"/>.Configure.</summary>
public interface IBenzeneApplicationBuilder : IRegisterDependency
{
    /// <summary>The hosting platform identifier, e.g. "AwsLambda" or "Worker".</summary>
    string Platform { get; }

    /// <summary>Creates a middleware pipeline builder for the given context type.</summary>
    IMiddlewarePipelineBuilder<TContext> Create<TContext>();
}
```
(Benzene.Abstractions.Pipelines already references Benzene.Abstractions.Middleware — no csproj change.)

### 2. `src/Benzene.Core.Middleware/BenzeneApplicationBuilder.cs`
```csharp
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class BenzeneApplicationBuilder : IBenzeneApplicationBuilder
{
    private readonly IBenzeneServiceContainer _benzeneServiceContainer;

    public BenzeneApplicationBuilder(string platform, IBenzeneServiceContainer benzeneServiceContainer)
    {
        Platform = platform;
        _benzeneServiceContainer = benzeneServiceContainer;
    }

    public string Platform { get; }

    public void Register(Action<IBenzeneServiceContainer> action) => action(_benzeneServiceContainer);

    public IMiddlewarePipelineBuilder<TContext> Create<TContext>() =>
        new MiddlewarePipelineBuilder<TContext>(_benzeneServiceContainer);
}
```
(Check Benzene.Core.Middleware references Benzene.Abstractions.Pipelines; if not, add the ProjectReference.
If that would create a reference cycle — Abstractions.Pipelines must NOT reference Core.Middleware — it
doesn't today; verify with `grep ProjectReference src/Benzene.Abstractions.Pipelines/*.csproj`.)

### 3. `src/Benzene.Microsoft.Dependencies/BenzeneStartUp.cs`
```csharp
using Benzene.Abstractions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Microsoft.Dependencies;

/// <summary>
/// Platform-neutral application definition. Derive once; run on any Benzene host
/// (AwsLambdaHost&lt;TStartUp&gt;, IHostBuilder.UseBenzene&lt;TStartUp&gt;()).
/// </summary>
public abstract class BenzeneStartUp : IStartUp<IServiceCollection, IConfiguration, IBenzeneApplicationBuilder>
{
    public abstract IConfiguration GetConfiguration();
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    public abstract void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration);
}
```
Add ProjectReference from Benzene.Microsoft.Dependencies to Benzene.Abstractions.Pipelines if missing
(IStartUp lives there; Microsoft.Dependencies currently references Benzene.Core only — check).
`Microsoft.Extensions.Configuration.Abstractions` is needed for `IConfiguration`: it's already used at
version 5.0.0 by Aws.Lambda.Core and SelfHost — add the same PackageReference (5.0.0) here. Do NOT add
any other new package.

### 4. `src/Benzene.Aws.Lambda.Core/AwsLambdaHost.cs`
```csharp
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Hosts a platform-neutral <see cref="BenzeneStartUp"/> as an AWS Lambda entry point. Subclass with
/// your StartUp (<c>public class Function : AwsLambdaHost&lt;StartUp&gt; { }</c>) and point function-handler
/// at <c>YourAssembly::YourNamespace.Function::FunctionHandlerAsync</c>.
/// </summary>
public class AwsLambdaHost<TStartUp> : IAwsLambdaEntryPoint where TStartUp : BenzeneStartUp, new()
{
    private readonly AwsLambdaEntryPoint _entryPoint;

    public AwsLambdaHost()
    {
        var startUp = new TStartUp();
        var configuration = startUp.GetConfiguration();
        var services = new ServiceCollection();
        services.AddLogging();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var eventPipeline = new MiddlewarePipelineBuilder<AwsEventStreamContext>(container);

        startUp.ConfigureServices(services, configuration);
        startUp.Configure(new AwsLambdaApplicationBuilder(eventPipeline, container), configuration);

        _entryPoint = new AwsLambdaEntryPoint(eventPipeline.Build(), new MicrosoftServiceResolverFactory(services));
    }

    public Task<Stream> FunctionHandlerAsync(Stream stream, ILambdaContext lambdaContext) =>
        _entryPoint.FunctionHandlerAsync(stream, lambdaContext);

    public void Dispose() => _entryPoint.Dispose();
}
```
Match `AwsLambdaEntryPoint`'s actual ctor/namespace by reading the file first; adjust usings to compile.

### 5. `src/Benzene.Aws.Lambda.Core/AwsLambdaApplicationBuilder.cs`
```csharp
public class AwsLambdaApplicationBuilder : BenzeneApplicationBuilder
{
    public const string PlatformName = "AwsLambda";

    public AwsLambdaApplicationBuilder(IMiddlewarePipelineBuilder<AwsEventStreamContext> eventPipeline,
        IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
        EventPipeline = eventPipeline;
    }

    public IMiddlewarePipelineBuilder<AwsEventStreamContext> EventPipeline { get; }
}

public static class AwsLambdaApplicationBuilderExtensions
{
    /// <summary>Applies AWS Lambda-specific configuration. No-op on other platforms.</summary>
    public static IBenzeneApplicationBuilder UseAwsLambda(this IBenzeneApplicationBuilder app,
        Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> configure)
    {
        if (app is AwsLambdaApplicationBuilder awsLambda)
        {
            configure(awsLambda.EventPipeline);
        }
        return app;
    }
}
```

### 6. `src/Benzene.SelfHost/WorkerApplicationBuilder.cs`
```csharp
public class WorkerApplicationBuilder : BenzeneApplicationBuilder
{
    public const string PlatformName = "Worker";
    private readonly BenzeneWorkerStartup2 _workerStartup;

    public WorkerApplicationBuilder(IBenzeneServiceContainer benzeneServiceContainer)
        : base(PlatformName, benzeneServiceContainer)
    {
        _workerStartup = new BenzeneWorkerStartup2(benzeneServiceContainer);
    }

    public IBenzeneWorkerStartup Workers => _workerStartup;

    public IBenzeneWorker CreateWorker(IServiceResolverFactory serviceResolverFactory) =>
        _workerStartup.Create(serviceResolverFactory);
}

public static class WorkerApplicationBuilderExtensions
{
    /// <summary>Applies worker-host-specific configuration. No-op on other platforms.</summary>
    public static IBenzeneApplicationBuilder UseWorker(this IBenzeneApplicationBuilder app,
        Action<IBenzeneWorkerStartup> configure)
    {
        if (app is WorkerApplicationBuilder worker)
        {
            configure(worker.Workers);
        }
        return app;
    }
}
```
(SelfHost needs a ProjectReference to whichever project holds `BenzeneApplicationBuilder` — it already
references Core via Microsoft.Dependencies; verify Core.Middleware is reachable, else add the reference.)

### 7. `src/Benzene.HostedService/HostBuilderExtensions.cs`
```csharp
public static class HostBuilderExtensions
{
    /// <summary>Runs a platform-neutral BenzeneStartUp as a hosted worker on the .NET generic host.</summary>
    public static IHostBuilder UseBenzene<TStartUp>(this IHostBuilder hostBuilder)
        where TStartUp : BenzeneStartUp, new()
    {
        var startUp = new TStartUp();
        var configuration = startUp.GetConfiguration();
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddLogging();
            var container = new MicrosoftBenzeneServiceContainer(services);
            var builder = new WorkerApplicationBuilder(container);

            startUp.ConfigureServices(services, configuration);
            startUp.Configure(builder, configuration);

            services.AddSingleton<IHostedService>(provider =>
                new BenzeneHostedServiceAdapter(builder.CreateWorker(new MicrosoftServiceResolverFactory(provider))));
        });
    }
}
```
Check `MicrosoftServiceResolverFactory` has an `IServiceProvider` ctor (it does — used in
`test/Benzene.Core.Test/Plugins/FluentValidation/FluentValidationPipelineTest.cs:43`).
Benzene.HostedService already references Microsoft.Extensions.Hosting (verify; add the Abstractions
package only if compilation requires it — prefer whatever it already uses for IHostedService).

## Tests (in `test/Benzene.Core.Test/`, new folder `Hosting/`)

Shared fixture — one StartUp used by BOTH tests (this is the point of the slice):
```csharp
public class SharedStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage()
            .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseAwsLambda(aws => aws.UseBenzeneMessage(p => p.UseMessageHandlers()))
        .UseWorker(w => w.Add(_ => new FakeWorker()));
}
```
Adapt registration calls to what `test/Benzene.Core.Test/Extras/Examples/LambdaEntryPoint.cs` and
`Core/Core/Logging/LogContextTest.cs` actually do (Defaults.Topic / ExampleRequestPayload / MessageBuilder
come from `Benzene.Test.Examples` + `Benzene.Testing`). `UseBenzeneMessage(...)` — copy the exact usage
from LogContextTest. NOTE: `SharedStartUp.ConfigureServices` receives the raw IServiceCollection; if
`UsingBenzene` on it conflicts with the host's container creation, instead register via
`app.Register(x => x.AddBenzene()...)` inside Configure — pick whichever compiles and passes, and note
the choice in the commit message.

1. **AWS host test**: instantiate `new AwsLambdaHost<SharedStartUp>()`, send a BenzeneMessage request for
   `Defaults.Topic` through `FunctionHandlerAsync`, assert an Ok/handled response. Read
   `src/Benzene.Tools/Aws/` to reuse or mimic how `AwsLambdaBenzeneTestHost.SendBenzeneMessageAsync`
   serializes the request/response — reuse those helpers if they accept an `IAwsLambdaEntryPoint`.
2. **Generic host test**: `new HostBuilder().UseBenzene<SharedStartUp>().Build()`; resolve
   `IHostedService`s, `StartAsync`/`StopAsync`, assert `FakeWorker.Started`/`Stopped` flags flipped
   (FakeWorker = tiny IBenzeneWorker in the test file recording calls; make its state observable via a
   static or injected collector — note tests may run in parallel, prefer instance-per-test via a
   thread-safe static reset or unique instance captured in the closure).
3. **Cross-platform no-op test**: on the AWS host, `UseWorker` block must not run (FakeWorker not created);
   on the generic host, `UseAwsLambda` must not run. Assert via flags/counters in SharedStartUp
   (e.g. static ints incremented inside each block; reset per test).

## Guardrails

- ADDITIVE ONLY: `AwsLambdaStartUp`, `BenzeneWorkerStartup`, `BenzeneHostedServiceStartup`,
  `InlineAwsLambdaStartUp` and all existing tests must be untouched and stay green.
- No new NuGet packages beyond `Microsoft.Extensions.Configuration.Abstractions` 5.0.0 (already in the
  solution). No sln structure changes (no new projects).
- Follow existing code style: file-scoped namespaces, XML doc comments on public types (match neighbors).
- If a signature here doesn't compile against reality, trust the code, adjust minimally, and record the
  deviation in the commit message.

## Verification & delivery

1. `dotnet build Benzene.sln` — 0 errors.
2. `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` — all green (baseline: 641 passed / 4 skipped
   before your change; your tests add to that).
3. `dotnet build Benzene.Examples.sln` — 0 errors.
4. Commit to `claude/benzene-cross-platform-design-ra60co` with a message describing the unified StartUp
   slice; push with `git push -u origin claude/benzene-cross-platform-design-ra60co`; then, if
   `git merge-base --is-ancestor origin/main HEAD` succeeds after `git fetch origin main`, also
   `git push origin HEAD:main`.

## Follow-up slices (do NOT start; listed for context)

- Unified `UseHttp` verb over a shared HTTP context (design needed in Benzene.Http).
- ASP.NET adapter (`builder.UseBenzene<TStartUp>()` / `app.UseBenzene()`), Azure isolated-worker rewrite.
- `IBenzeneInvocation` feature bag; obsolete forwarders for `UseApiGateway`→`UseHttp`.
