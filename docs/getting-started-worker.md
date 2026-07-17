# Worker Service Setup

Benzene can run as a plain, long-running .NET Worker Service — no cloud function trigger, no
ASP.NET Core, just the .NET generic host (`IHostBuilder`) hosting one or more background
`IBenzeneWorker`s. There are two ways to get there, and which one you need depends on what else
your worker has to do:

| You want... | Use |
|---|---|
| A custom background loop (polling a database, a timer, a queue you talk to yourself), with the same `BenzeneStartUp` shape used by AWS/Azure/ASP.NET Core | **Part A** — `BenzeneStartUp` + `worker.Add(...)` |
| A built-in consumer — Kafka (`Benzene.Kafka.Core`), a bare `HttpListener` endpoint (`Benzene.SelfHost.Http`), Azure Service Bus (`Benzene.Azure.ServiceBus`), or Azure Event Hubs (`Benzene.Azure.EventHub`) — as part of the same process | **Part B** — `BenzeneStartUp` + `worker.UseKafka(...)`/`worker.UseHttp(...)`/`worker.UseServiceBus(...)`/`worker.UseEventHub(...)` |

Both use the same `BenzeneStartUp` shape — the one exercised by
`test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs` and documented in
[Unified Hosting Model](hosting). They differ only in what you register inside `UseWorker(...)`:
Part A adds a worker class you wrote yourself, while Part B calls a built-in
`UseKafka`/`UseHttp`/`UseServiceBus`/`UseEventHub` extension. Those built-in extensions
hang off the `IBenzeneWorkerStartup` builder that `UseWorker(...)` hands you — see
[How `UseWorker` composes the built-in workers](#how-useworker-composes-the-built-in-workers) below
for exactly how the two builders relate.

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
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
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
receives an `IBenzeneWorkerStartup` directly (the same builder `UseWorker(...)` hands you), so it's
a lighter-weight way to register a worker without going through the generic host.

## Part B: built-in workers (Kafka, HTTP, Service Bus, Event Hub)

`Benzene.Kafka.Core` (see [Kafka Setup](getting-started-kafka)), `Benzene.SelfHost.Http`,
`Benzene.Azure.ServiceBus`, and `Benzene.Azure.EventHub` ship built-in workers rather than asking
you to write your own. Their `UseKafka`/`UseHttp`/`UseServiceBus`/`UseEventHub` extensions
target `IBenzeneWorkerStartup` — the worker-specific builder that `UseWorker(...)` hands you — so you
wire them up from the same `BenzeneStartUp` shape as Part A, just calling the built-in extensions
inside `UseWorker(...)` instead of `worker.Add(...)`:

```bash
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.SelfHost.Http --prerelease
```

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.HealthChecks;
using Benzene.Microsoft.Dependencies;
using Benzene.SelfHost;
using Benzene.SelfHost.Http;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(HeartbeatMessageHandler).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseWorker(worker => worker.UseHttp(new BenzeneHttpConfig
        {
            Url = "http://localhost:5151/",
            ConcurrentRequests = 10
        }, http => http
            .UseHealthCheck("get", "healthcheck", x => x.AddHealthCheck("live", resolver => true))
            .UseMessageHandlers()));
    }
}
```

`BenzeneHttpWorker` (registered by `.UseHttp(...)`) runs a `System.Net.HttpListener` loop internally
— genuinely a bare HTTP server with no ASP.NET Core/Kestrel underneath, suitable for a lightweight
health/metrics endpoint alongside a worker, but not a general ASP.NET Core replacement (no routing
conventions, model binding, or middleware ecosystem beyond what Benzene itself provides). Host it
exactly like Part A, with `Benzene.HostedService`'s `UseBenzene<StartUp>()`, which registers the
worker as an `IHostedService`:

```csharp
using Benzene.HostedService;
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
```

Add Kafka consumption the same way, inside the same `UseWorker(...)`, via
`Benzene.Kafka.Core`'s `worker.UseKafka<TKey, TValue>(kafkaConfig, kafka => kafka.UseMessageHandlers())` —
see [Kafka Setup](getting-started-kafka#1-self-hosted-kafka-worker-benzenekafkacore) for the full
walkthrough; `examples/Kafka/Benzene.Examples.Kakfa` combines exactly this Kafka-plus-HTTP shape in
one worker.

### Azure Service Bus (`Benzene.Azure.ServiceBus`)

Consume a Service Bus queue or topic/subscription in-process — the self-hosted counterpart of the
[Service Bus *trigger*](cookbooks/service-bus-handling) in Azure Functions. The worker runs the
SDK's `ServiceBusProcessor`, so receiving, message-lock renewal, and bounded concurrency
(`MaxConcurrentCalls`) are the processor's job; you just supply the client and the pipeline. The
caller builds the `ServiceBusClient` (connection string, managed identity, or emulator), so
Benzene never prescribes how you authenticate.

```bash
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.Azure.ServiceBus --prerelease
```

```csharp
using Azure.Messaging.ServiceBus;
using Benzene.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;

public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
{
    var client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);

    app.UseWorker(worker => worker.UseServiceBus(
        new BenzeneServiceBusConfig
        {
            QueueName = "orders",              // or TopicName + SubscriptionName
            MaxConcurrentCalls = 5,
            AckMode = ServiceBusConsumerAckMode.AutoComplete,
        },
        new ServiceBusClientFactory(client),
        serviceBus => serviceBus.UseMessageHandlers()));
}
```

The topic used for handler routing comes from the message's `"topic"` application property, exactly
as in the Functions trigger — see the cookbook's [Where the topic comes from](cookbooks/service-bus-handling#2-where-the-topic-comes-from).
`AckMode` controls settlement: `AutoComplete` (default) lets the processor complete on success and
abandon when the handler throws; `Explicit` makes Benzene complete/abandon each message itself from
the handler's outcome, including a non-exception failure result.

### Azure Event Hubs (`Benzene.Azure.EventHub`)

Consume an event hub in-process — the self-hosted counterpart of the
[Event Hub *trigger*](cookbooks/event-hub-processing) in Azure Functions. The worker runs the SDK's
`EventProcessorClient`, so consumer groups, partition load balancing across worker instances, and
blob-checkpointed offsets are the processor's job. Unlike the Functions trigger — where batching and
checkpointing are entirely the runtime's — here **Benzene owns checkpointing** (per partition, every
`CheckpointInterval` successfully handled events) and the starting position for a fresh consumer
group.

```bash
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.Azure.EventHub --prerelease
```

```csharp
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Benzene.Azure.EventHub;
using Benzene.Core.MessageHandlers;

public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
{
    // The processor client decides the hub, consumer group, checkpoint container, and auth.
    // The blob container must already exist — EventProcessorClient does not create it.
    var checkpointStore = new BlobContainerClient(
        configuration["Storage:ConnectionString"], "eventhub-checkpoints");
    var processorClient = new EventProcessorClient(
        checkpointStore,
        EventHubConsumerClient.DefaultConsumerGroupName,
        configuration["EventHub:ConnectionString"],
        "telemetry");

    app.UseWorker(worker => worker.UseEventHub(
        new BenzeneEventHubConfig
        {
            CheckpointInterval = 100,
            // Fresh consumer group with no checkpoint: process the retained backlog rather than
            // only new events (the EventProcessorClient default). Kafka analog: AutoOffsetReset.
            DefaultStartingPosition = EventPosition.Earliest,
        },
        new EventProcessorClientFactory(processorClient),
        eventHub => eventHub.UseMessageHandlers()));
}
```

The topic comes from each event's `"topic"` property. Event Hubs has no per-event dead-letter, so
`CatchHandlerExceptions` (default `true`) decides what happens to a failing event: logged and the
partition keeps going (the failed event is effectively skipped once a later one checkpoints past
it), or set `false` to stop the worker without checkpointing the failure so a restart redelivers it
(at-least-once). See the cookbook's [checkpointing on failure](cookbooks/event-hub-processing#6-checkpointing-on-failure-and-why-benzene-doesnt-help-with-poison-events)
for the trade-off.

### Testing

A worker built this way isn't request/response-shaped, so there's no `BenzeneTestHost` shortcut —
drive it exactly as in [Part A's testing section](#8-testing): build the real host with
`new HostBuilder().UseBenzene<StartUp>().Build()`, start its `IHostedService`s, drive it with real
input (an HTTP call, a published Kafka message), poll for the observable effect, then stop them. The
Kafka integration-test flow in [Kafka Setup](getting-started-kafka#17-testing) follows the same shape
against a live broker.

## How `UseWorker` composes the built-in workers

`IBenzeneApplicationBuilder` (the builder `Configure` receives) and `IBenzeneWorkerStartup` (the
builder `UseWorker(...)` hands you) are different interfaces with different capabilities:

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
backed internally by a private `BenzeneWorkerBuilder` instance. `UseWorker` (Part A) just reaches
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

`Benzene.Kafka.Core.Extensions.UseKafka`, `Benzene.SelfHost.Http.Extensions.UseHttp`,
`Benzene.Azure.ServiceBus.Extensions.UseServiceBus`, and `Benzene.Azure.EventHub.Extensions.UseEventHub`
are all written directly against `IBenzeneWorkerStartup` (calling `.Add(...)` to register the built-in
`BenzeneKafkaWorker`/`BenzeneHttpWorker`/`BenzeneServiceBusWorker`/`BenzeneEventHubWorker`), so you
call them on the `worker` builder that `UseWorker(...)` hands you — not on `IBenzeneApplicationBuilder`
directly. If you write your own `IBenzeneWorker` from scratch (Part A), you don't need any of those
packages; you register it with the same `worker.Add(...)` those extensions call under the hood.

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
- **Can't call `.UseKafka(...)`/`.UseHttp(...)`/`.UseServiceBus(...)`/`.UseEventHub(...)` directly on
  `IBenzeneApplicationBuilder`** — these extend `IBenzeneWorkerStartup`, not `IBenzeneApplicationBuilder`;
  call them on the `worker` builder inside `app.UseWorker(worker => worker.UseKafka(...))` (Part B).
- **Azure worker consumes nothing, no error** — the message-handler router needs a full set of
  per-context services; `UseServiceBus`/`UseEventHub` register them for you, but if you hand-roll
  the registration make sure `AddServiceBusConsumer()`/`AddEventHubConsumer()` ran (they add the
  version getter, media-format negotiation, and request mapper alongside the topic/body/headers
  getters). A missing one makes the router throw at resolve time, which the worker logs and
  swallows — so it surfaces only as messages never being handled.
- **Host exits immediately** — make sure `Program.cs` calls `await host.RunAsync()` (or `host.Run()`),
  not just `host.Build()`; the generic host only starts registered `IHostedService`s once run.

## See Also

- [Unified Hosting Model](hosting)
- [Testing Benzene](testing-benzene)
- [Kafka Setup](getting-started-kafka)
- [Health Checks](health-checks)
- [Message Handlers](message-handlers)
