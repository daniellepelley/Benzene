# Worker Service Setup

Benzene can run as a plain, long-running .NET Worker Service — no cloud function trigger, no
ASP.NET Core, just the .NET generic host (`IHostBuilder`) hosting one or more background
`IBenzeneWorker`s. There are two ways to get there, and which one you need depends on what else
your worker has to do:

| You want... | Use |
|---|---|
| A custom background loop (polling a database, a timer, a queue you talk to yourself), with the same `BenzeneStartUp` shape used by AWS/Azure/ASP.NET Core | **Part A** — `BenzeneStartUp` + `Benzene.HostedService` |
| A Kafka consumer (`Benzene.Kafka.Core`) or a bare `HttpListener`-based HTTP endpoint (`Benzene.SelfHost.Http`) as part of the same process | **Part B** — `BenzeneWorkerStartup`/`BenzeneHostedServiceStartup` |

Part A is the modern, recommended shape — it's the one exercised by
`test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs` and documented in
[Unified Hosting Model](hosting). Part B exists because `Benzene.Kafka.Core.UseKafka` and
`Benzene.SelfHost.Http.UseHttp` were written against an older, worker-specific startup interface
that predates the unified model and haven't been ported to it — see
[Why two startup shapes?](#why-two-startup-shapes) below for exactly why.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Part A: a custom background worker (recommended)

### 1. Create the project

```bash
mkdir MyWorker && cd MyWorker
dotnet new worker -f net10.0
```

### 2. Install the NuGet packages

```bash
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.Core.MessageHandlers --prerelease
```

`Benzene.HostedService` brings in `Benzene.SelfHost` and `Benzene.Microsoft.Dependencies`
(and so `BenzeneStartUp`) transitively.

### 3. Define a message handler

As with every other Benzene host, business logic lives in a message handler, not in the worker
loop itself:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

[Message("heartbeat")]
public class HeartbeatMessageHandler : IMessageHandler<HeartbeatMessage>
{
    public Task HandleAsync(HeartbeatMessage message)
    {
        Console.WriteLine($"heartbeat at {DateTimeOffset.UtcNow:O}");
        return Task.CompletedTask;
    }
}

public class HeartbeatMessage
{
}
```

### 4. Write the `IBenzeneWorker`

`IBenzeneWorker` (`Benzene.Abstractions.Hosting`) is deliberately minimal — just
`StartAsync`/`StopAsync`, the same shape `IHostedService` uses:

```csharp
public interface IBenzeneWorker
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

To dispatch into the message-handler pipeline from your own polling loop, wrap a middleware
pipeline over `BenzeneMessageContext` in a `BenzeneMessageApplication` (`Benzene.Core.MessageHandlers.BenzeneMessage`)
— the same transport-agnostic building block AWS's direct `BenzeneMessage` entry point and Azure's
Event Hub bridge use internally — and call its `HandleAsync(IBenzeneMessageRequest, IServiceResolverFactory)`
on each tick:

```csharp
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.MessageHandlers.BenzeneMessage;

public class HeartbeatWorker : IBenzeneWorker, IDisposable
{
    private readonly IServiceResolverFactory _serviceResolverFactory;
    private readonly BenzeneMessageApplication _application;
    private Timer? _timer;

    public HeartbeatWorker(IServiceResolverFactory serviceResolverFactory, BenzeneMessageApplication application)
    {
        _serviceResolverFactory = serviceResolverFactory;
        _application = application;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(
            _ => _application.HandleAsync(
                new BenzeneMessageRequest { Topic = "heartbeat", Body = "{}" },
                _serviceResolverFactory),
            null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}
```

This mirrors how Benzene's own built-in workers are shaped — `BenzeneKafkaWorker` and
`BenzeneHttpWorker` (see Part B) both follow the identical pattern: take an
`IServiceResolverFactory`, run a loop, and dispatch each unit of work through a middleware pipeline
wrapped in a small application class. Swap the `Timer` here for whatever your real polling source
is (a database query, an internal queue, a SDK's long-poll call).

### 5. Define your `StartUp`

`BenzeneStartUp` is the same platform-neutral base class used by
[AWS Lambda](getting-started-aws), [Azure Functions](azure-functions), and
[ASP.NET Core](asp-net-core). Register your worker via `UseWorker`, the `Benzene.SelfHost`
extension that's a no-op on every platform except this one:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddBenzeneMessage()
            .AddMessageHandlers(typeof(HeartbeatMessageHandler).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var pipeline = app.Create<BenzeneMessageContext>()
            .UseMessageHandlers()
            .Build();

        var application = new BenzeneMessageApplication(pipeline);

        app.UseWorker(workers => workers.Add(
            resolverFactory => new HeartbeatWorker(resolverFactory, application)));
    }
}
```

`workers.Add(Func<IServiceResolverFactory, IBenzeneWorker>)` can be called more than once — each
registered factory becomes its own `IBenzeneWorker`, and all of them are started/stopped together
by a `CompositeBenzeneWorker` the host builds for you.

### 6. Wire up `Program.cs`

```csharp
using Benzene.HostedService;
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
```

Make sure this `UseBenzene<TStartUp>()` comes from `Benzene.HostedService` (`using Benzene.HostedService;`)
— **`Benzene.Azure.Function.Core` declares an extension method with the exact same name and
signature**, `IHostBuilder UseBenzene<TStartUp>(this IHostBuilder)`, but it registers a scoped
`IAzureFunctionApp` instead of a singleton `IHostedService` wrapping your workers. They're
structurally identical (same three-step `GetConfiguration`/`ConfigureServices`/`Configure` dance
against their own platform's `IBenzeneApplicationBuilder`) but genuinely two different methods in
two different packages, not overloads of one — only reference `Benzene.HostedService` in a plain
worker project so there's no ambiguity.

### 7. Run it locally

```bash
dotnet run
```

You should see a `heartbeat at ...` line every 30 seconds.

### 8. Testing

There's no `BenzeneTestHost.Build*`/`Send*Async` helper for a worker-only `StartUp` — a worker
isn't request/response-shaped, so there's nothing to "send" to it. Build the real host and drive
its lifecycle directly instead, exactly as in [Testing Benzene](testing-benzene#worker--generic-host):

```csharp
var host = new HostBuilder().UseBenzene<StartUp>().Build();
var hostedServices = host.Services.GetServices<IHostedService>().ToList();

foreach (var service in hostedServices)
{
    await service.StartAsync(CancellationToken.None);
}

// ... assert on your worker's observable side effect ...

foreach (var service in hostedServices)
{
    await service.StopAsync(CancellationToken.None);
}
```

If you'd rather not spin up the full generic host at all — e.g. for a fast unit test of just the
worker wiring — `Benzene.SelfHost.InlineSelfHostedStartUp` builds an `IBenzeneWorker` directly from
inline `ConfigureServices`/`Configure` actions, without `IHostBuilder`:

```csharp
var worker = new InlineSelfHostedStartUp()
    .ConfigureServices(services => services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage()
        .AddMessageHandlers(typeof(HeartbeatMessageHandler).Assembly)))
    .Configure(app =>
    {
        var pipeline = app.Create<BenzeneMessageContext>().UseMessageHandlers().Build();
        var application = new BenzeneMessageApplication(pipeline);
        app.Add(resolverFactory => new HeartbeatWorker(resolverFactory, application));
    })
    .Build();

await worker.StartAsync(CancellationToken.None);
```

Note this bypasses `BenzeneStartUp`/`IBenzeneApplicationBuilder` entirely — `Configure` here
receives an `IBenzeneWorkerStartup` directly (the same interface Part B's `Configure` method
receives), so it's really a lighter-weight variant of Part B's shape rather than of Part A's.

## Part B: adding Kafka or a bare HTTP listener

`Benzene.Kafka.Core` (see [Kafka Setup](getting-started-kafka)) and `Benzene.SelfHost.Http` both
predate the unified `BenzeneStartUp`/`IBenzeneApplicationBuilder` model, and their `UseKafka`/`UseHttp`
extensions only target `IBenzeneWorkerStartup` — the older, worker-specific application builder
interface — not `IBenzeneApplicationBuilder`. If you need either of them, derive from
`Benzene.HostedService.BenzeneHostedServiceStartup` (or its base, `Benzene.SelfHost.BenzeneWorkerStartup`,
if you don't need it registered as an `IHostedService` directly) instead of `BenzeneStartUp`:

```bash
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.SelfHost.Http --prerelease
```

```csharp
using Benzene.Core.MessageHandlers.DI;
using Benzene.HostedService;
using Benzene.HealthChecks;
using Benzene.SelfHost;
using Benzene.SelfHost.Http;

public class StartUp : BenzeneHostedServiceStartup
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HeartbeatMessageHandler).Assembly));

    public override void Configure(IBenzeneWorkerStartup app, IConfiguration configuration)
    {
        app.UseHttp(new BenzeneHttpConfig
        {
            Url = "http://localhost:5151/",
            ConcurrentRequests = 10
        }, http => http
            .UseHealthCheck("get", "healthcheck", x => x.AddHealthCheck("live", resolver => true))
            .UseMessageHandlers());
    }
}
```

`BenzeneHttpWorker` (registered by `.UseHttp(...)`) runs a `System.Net.HttpListener` loop internally
— genuinely a bare HTTP server with no ASP.NET Core/Kestrel underneath, suitable for a lightweight
health/metrics endpoint alongside a worker, but not a general ASP.NET Core replacement (no routing
conventions, model binding, or middleware ecosystem beyond what Benzene itself provides). Because
`BenzeneHostedServiceStartup` already implements `IHostedService` itself, registering it is a
one-liner in `Program.cs`, without `Benzene.HostedService.HostBuilderExtensions.UseBenzene<TStartUp>()`:

```csharp
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddHostedService<StartUp>())
    .Build();

await host.RunAsync();
```

Add Kafka consumption the same way, in the same `Configure` method, via
`Benzene.Kafka.Core`'s `app.UseKafka<TKey, TValue>(kafkaConfig, kafka => kafka.UseMessageHandlers())` —
see [Kafka Setup](getting-started-kafka#1-self-hosted-kafka-worker-benzenekafkacore) for the full
walkthrough; `examples/Kafka/Benzene.Examples.Kakfa` combines exactly this Kafka-plus-HTTP shape in
one worker.

### Testing

The integration test pattern for this shape is the same as [Kafka Setup](getting-started-kafka#17-testing)
describes: construct and start a real `StartUp` instance directly (`new StartUp()`, then
`await startUp.StartAsync(...)`) on a background thread, drive it with real input (an HTTP call, a
published Kafka message), and poll for the observable effect — there's no `BenzeneTestHost` shortcut
for this startup shape either.

## Why two startup shapes?

`IBenzeneApplicationBuilder` (Part A) and `IBenzeneWorkerStartup` (Part B) are different interfaces
with different capabilities:

```csharp
public interface IBenzeneApplicationBuilder : IRegisterDependency
{
    string Platform { get; }
    IMiddlewarePipelineBuilder<TContext> Create<TContext>();
}

public interface IBenzeneWorkerStartup : IRegisterDependency
{
    void Add(Func<IServiceResolverFactory, IBenzeneWorker> func);
    IMiddlewarePipelineBuilder<TNewContext> Create<TNewContext>();
    IBenzeneWorker Create(IServiceResolverFactory serviceResolverFactory);
}
```

`WorkerApplicationBuilder` (`Benzene.SelfHost`) bridges the two: it's an `IBenzeneApplicationBuilder`
(`Platform => "Worker"`) that *exposes* an `IBenzeneWorkerStartup` through its `Workers` property,
backed internally by a private `BenzeneWorkerStartup2` instance. `UseWorker` (Part A) just reaches
through to that same `Workers` property:

```csharp
public static IBenzeneApplicationBuilder UseWorker(this IBenzeneApplicationBuilder app,
    Action<IBenzeneWorkerStartup> configure)
{
    if (app is WorkerApplicationBuilder worker)
    {
        configure(worker.Workers);
    }
    return app;
}
```

`BenzeneWorkerStartup` (Part B's base class) builds its own separate `BenzeneWorkerStartup2` and
passes it directly as the `app` parameter of `Configure(IBenzeneWorkerStartup app, ...)` — so both
paths ultimately register workers against the same underlying type, just reached differently.
`Benzene.Kafka.Core.Extensions.UseKafka` and `Benzene.SelfHost.Http.Extensions.UseHttp` were written
directly against `IBenzeneWorkerStartup` (calling `.Add(...)` to register the built-in
`BenzeneKafkaWorker`/`BenzeneHttpWorker`), before `IBenzeneApplicationBuilder`/`UseWorker` existed —
so today they only compile against Part B's `Configure(IBenzeneWorkerStartup app, ...)` signature,
not Part A's `Configure(IBenzeneApplicationBuilder app, ...)`. If you write your own `IBenzeneWorker`
from scratch, you don't need either package, and Part A's unified shape is simpler and consistent
with every other Benzene host.

## Troubleshooting

- **`UseWorker` compiles but nothing happens** — `WorkerApplicationBuilderExtensions.UseWorker`
  no-ops on every `IBenzeneApplicationBuilder` that isn't a `WorkerApplicationBuilder` (e.g. if
  you're actually running under `AwsLambdaApplicationBuilder`). This is deliberate — it's what lets
  a single `StartUp` mix `UseAwsLambda(...)` and `UseWorker(...)` in one `Configure` method (see
  [Unified Hosting Model](hosting)) — but double-check you're building the host via
  `Benzene.HostedService`'s `UseBenzene<TStartUp>()`, not some other adapter, if a worker you
  registered never starts.
- **Wrong `UseBenzene<TStartUp>()` resolves** — if a project references both `Benzene.HostedService`
  and `Benzene.Azure.Function.Core`, make sure the `using` in scope is the one you intend; both
  declare an identically-shaped extension method on `IHostBuilder`.
- **Can't call `.UseKafka(...)`/`.UseHttp(...)` inside `Configure(IBenzeneApplicationBuilder app, ...)`** —
  these extend `IBenzeneWorkerStartup`, not `IBenzeneApplicationBuilder`; switch to
  `BenzeneWorkerStartup`/`BenzeneHostedServiceStartup` (Part B) if you need them.
- **Host exits immediately** — make sure `Program.cs` calls `await host.RunAsync()` (or `host.Run()`),
  not just `host.Build()`; the generic host only starts registered `IHostedService`s once run.

## See Also

- [Unified Hosting Model](hosting)
- [Testing Benzene](testing-benzene)
- [Kafka Setup](getting-started-kafka)
- [Health Checks](health-checks)
- [Message Handlers](message-handlers)
