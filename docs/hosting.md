# Unified Hosting Model

Benzene's hosting model lets you write one `BenzeneStartUp` class and run it, unchanged, on AWS
Lambda, Azure Functions, the .NET generic host (as a background worker), or ASP.NET Core.

## Three ways Benzene starts

Every host below falls into one of three fundamentally different execution models. Which one
you're in determines who owns the process, whether anything is listening or polling, and - for
the third model only - how many events Benzene processes at once.

**1. Triggered (serverless)** - AWS Lambda, Azure Functions. Nothing is running until the
platform invokes your code for a single event. There is no Benzene-owned process, and nothing
polls - the platform's own infrastructure (API Gateway, an SQS/Event Hub/Service Bus trigger, ...)
calls into a cold or warm instance per invocation, and the instance can be frozen or torn down
between calls. See [AWS Lambda](#aws-lambda--awslambdahosttstartup) and
[Azure Functions](#azure-functions-isolated-worker-and-the-generic-worker-host--ihostbuilderusebenzenetstartup)
below. Batches (SQS records, an Event Hub/Kafka/Service Bus trigger's message batch) are dispatched
in parallel via an uncapped `Task.WhenAll` today - a separate, currently-unaddressed concern from
the worker model's own bounded concurrency described below.

**2. Embedded in an existing host** - ASP.NET Core, including gRPC. A pre-existing,
already-long-running listener (Kestrel) owns the process and its own concurrency model: one
incoming request is one async call, and Kestrel's own thread pool and connection handling decide
how many run at once. Benzene is just middleware inside that pipeline; it never starts, stops, or
paces anything about the host process itself. See
[ASP.NET Core](#aspnet-core--webapplicationbuilderusebenzenetstartup--iapplicationbuilderusebenzene).

**3. Self-hosted worker** - `Benzene.HostedService` + `Benzene.SelfHost.Http`/`Benzene.Kafka.Core`/
`Benzene.Aws.Sqs`/`Benzene.Azure.ServiceBus`/`Benzene.Azure.EventHub`/`Benzene.RabbitMq`.
Here, Benzene itself owns a long-running consumer that actively receives work (an HTTP
`HttpListener.GetContextAsync()` accept loop, a Kafka consumer's `Consume()` poll, an SQS
long-poll loop, a running Service Bus/Event Hubs SDK processor, or a RabbitMQ
`AsyncEventingBasicConsumer` push consumer) and is
responsible for keeping the process alive - there is no external infrastructure invoking you, and
no separate host already listening. This is the one mode where how many events Benzene processes
*at once* is Benzene's own decision, not the platform's - see
[Worker concurrency](#worker-concurrency) below for exactly how that's bounded and configured.

> **Deprecated: `Benzene.SelfHost.Http`.** The HTTP self-host worker is built on
> `System.Net.HttpListener`, which is materially slower than Kestrel and adds no advantage over the raw
> listener. For a self-hosted HTTP endpoint use **`Benzene.AspNet.Core`** (Kestrel, mode 2 above)
> instead — the same `BenzeneStartUp.Configure` moves across unchanged. See
> [Deprecations](deprecations.md). The other self-hosted workers (Kafka, SQS, Service Bus, Event Hub,
> RabbitMQ) are not affected — they own genuinely different transports, not a slower wrapper of an
> existing one.

## Overview

Every platform-specific getting-started guide ([AWS Lambda](getting-started-aws.md),
[Azure Functions](azure-functions.md), [ASP.NET Core](asp-net-core.md)) builds on the same foundation:
a `BenzeneStartUp` subclass that defines configuration, service registration, and middleware
pipeline setup once. A thin, platform-specific host adapter (`AwsLambdaHost<TStartUp>`,
`IHostBuilder.UseBenzene<TStartUp>()`, `WebApplicationBuilder.UseBenzene<TStartUp>()`) then wires
that single class into whatever native hosting API the platform expects.

This solves a specific problem: without it, moving a service between platforms — or supporting
more than one at once (e.g. HTTP via API Gateway *and* a background SQS consumer, both defined in
the same `Configure` method) — means duplicating startup wiring per platform, with all the drift
that implies. The unified model keeps that wiring in exactly one place, cross-platform, and pushes
only the genuinely platform-specific pieces (the event pipeline shape, the native invocation
context) behind small, explicit escape hatches like `UseAwsLambda`, `UseHttp`, and `UseWorker`.

The model has three moving parts:

- **`BenzeneStartUp`** — the abstract class you write. It never references AWS, Azure, or
  ASP.NET Core types directly.
- **`IBenzeneApplicationBuilder`** — the platform-neutral builder your `Configure` method
  receives. Each host passes in its own implementation; platform-specific `Use*` extension
  methods (`UseAwsLambda`, `UseHttp`, `UseWorker`, ...) pattern-match on the concrete type and
  no-op if you call the wrong one for the platform actually running.
- **A host adapter** — the platform-specific glue (`AwsLambdaHost<TStartUp>`,
  `UseBenzene<TStartUp>()`) that instantiates your `StartUp`, runs its three lifecycle methods,
  and adapts the result to that platform's native entry point shape.

## The `BenzeneStartUp` contract

`BenzeneStartUp` (namespace `Benzene.Microsoft.Dependencies`, package `Benzene.Microsoft.Dependencies` —
referenced transitively by every platform package) is:

```csharp
public abstract class BenzeneStartUp : IStartUp<IServiceCollection, IConfiguration, IBenzeneApplicationBuilder>
{
    public abstract IConfiguration GetConfiguration();
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    public abstract void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration);
}
```

It implements the generic `IStartUp<TContainer, TConfiguration, TAppBuilder>` interface (from
`Benzene.Abstractions.Pipelines`, namespace `Benzene.Abstractions.Hosting`) with `TContainer`
fixed to Microsoft's `IServiceCollection`, `TConfiguration` fixed to `IConfiguration`, and
`TAppBuilder` fixed to `IBenzeneApplicationBuilder`:

```csharp
public interface IStartUp<TContainer, TConfiguration, TAppBuilder>
{
    TConfiguration GetConfiguration();
    void ConfigureServices(TContainer services, TConfiguration configuration);
    void Configure(TAppBuilder app, TConfiguration configuration);
}
```

Every host adapter calls these three members in the same order, exactly once, on startup (cold
start for Lambda; process start for everything else):

1. **`GetConfiguration()`** — builds and returns the `IConfiguration` used for both of the
   following steps. Runs before any services are registered, so it cannot resolve anything from
   DI. Typically builds a `ConfigurationBuilder` from environment variables, JSON files, or a
   cloud configuration provider.
2. **`ConfigureServices(IServiceCollection services, IConfiguration configuration)`** — registers
   services with Microsoft's DI container. Almost always starts with
   `services.UsingBenzene(x => x.AddMessageHandlers(...))` to register Benzene's own
   infrastructure plus your message handlers.
3. **`Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)`** — builds the
   middleware pipeline(s) against the platform-neutral `app` builder, using platform-specific
   `Use*` extensions (`UseAwsLambda`, `UseHttp`, `UseWorker`) to reach the actual event pipeline
   for whichever platform(s) this `StartUp` targets.

A single `StartUp` can call more than one of `UseAwsLambda`/`UseHttp`/`UseWorker` in the same
`Configure` method — each is a no-op unless the concrete `IBenzeneApplicationBuilder` handed in
at runtime matches, so the same class can be deployed as, say, an AWS Lambda function that also
starts a background worker:

```csharp
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage()
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseAwsLambda(aws => aws.UseBenzeneMessage(p => p.UseMessageHandlers()))
        .UseWorker(w => w.Add(_ => new MyBackgroundWorker()));
}
```

(This exact shape is exercised in `test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs`'s
`SharedStartUp`.)

## `IBenzeneApplicationBuilder`

```csharp
public interface IBenzeneApplicationBuilder : IRegisterDependency
{
    string Platform { get; }
    IMiddlewarePipelineBuilder<TContext> Create<TContext>();
}
```

- **`Platform`** — the hosting platform identifier for the concrete builder instance:
  `"AwsLambda"`, `"AzureFunctions"`, `"AspNet"`, or `"Worker"`. This is also what
  `IBenzeneInvocation.Platform` reports for the matching invocation (see below).
- **`Create<TContext>()`** — creates a new `IMiddlewarePipelineBuilder<TContext>` sharing this
  builder's underlying DI container. Platform-specific `Use*` extensions use this internally;
  you rarely need to call it directly.
- **`Register(Action<IBenzeneServiceContainer> action)`** (from `IRegisterDependency`) — runs an
  action against the underlying Benzene service container, for middleware that needs to register
  its own dependencies as a side effect of being added to the pipeline.

Every concrete implementation derives from `Benzene.Core.Middleware.BenzeneApplicationBuilder`,
which supplies `Platform` and `Create<TContext>()` uniformly:

| Concrete builder | Platform value | Where |
|---|---|---|
| `AwsLambdaApplicationBuilder` | `"AwsLambda"` | `Benzene.Aws.Lambda.Core` |
| `AzureFunctionAppBuilder` | `"AzureFunctions"` | `Benzene.Azure.Function.Core` |
| `AspApplicationBuilder` | `"AspNet"` | `Benzene.AspNet.Core` |
| `WorkerApplicationBuilder` | `"Worker"` | `Benzene.SelfHost` |

Platform-specific `Use*` extension methods (`UseAwsLambda`, `UseHttp`, `UseWorker`) are simple
pattern matches against these concrete types:

```csharp
public static IBenzeneApplicationBuilder UseAwsLambda(this IBenzeneApplicationBuilder app,
    Action<IMiddlewarePipelineBuilder<AwsEventStreamContext>> configure)
{
    if (app is AwsLambdaApplicationBuilder awsLambda)
    {
        configure(awsLambda.EventPipeline);
    }
    return app;
}
```

This is why calling `UseAwsLambda(...)` in a `StartUp` that's actually running under
`WorkerApplicationBuilder` compiles and runs fine — it just does nothing (verified by
`UnifiedStartUpTest.UseAwsLambda_NoOpOnOtherPlatforms` /
`UseWorker_NoOpOnOtherPlatforms`).

## Per-platform host adapters

Each adapter takes on the same three responsibilities: build an `IServiceCollection`, call your
`StartUp`'s three lifecycle methods against it and a platform-specific `IBenzeneApplicationBuilder`,
then adapt the built pipeline to whatever the platform's native invocation model needs.

### AWS Lambda — `AwsLambdaHost<TStartUp>`

Package: `Benzene.Aws.Lambda.Core`.

Subclass `AwsLambdaHost<TStartUp>` directly — it *is* the Lambda entry point, so there is no
separate handler class to write:

```csharp
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => new ConfigurationBuilder()
        .AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x.AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
        => app.UseAwsLambda(eventPipeline => eventPipeline
            .UseApiGateway(apiGatewayApp => apiGatewayApp.UseMessageHandlers()));
}

public class Function : AwsLambdaHost<StartUp> { }
```

Point the Lambda's `function-handler` at `YourAssembly::YourNamespace.Function::FunctionHandlerAsync`.
`AwsLambdaHost<TStartUp>`'s constructor builds the `ServiceCollection`, calls `GetConfiguration`,
`ConfigureServices`, and `Configure` (against an `AwsLambdaApplicationBuilder`) once on cold
start, then reuses the built pipeline for every subsequent invocation via
`FunctionHandlerAsync(Stream, ILambdaContext)`. See [AWS Lambda Setup](getting-started-aws.md) for
the full walkthrough including SAM deployment and the other supported event sources (SQS, SNS,
Kafka, S3).

### Azure Functions (isolated worker) and the generic Worker host — `IHostBuilder.UseBenzene<TStartUp>()`

Both `Benzene.Azure.Function.Core` and `Benzene.HostedService` expose an
`IHostBuilder.UseBenzene<TStartUp>()` extension, and the two are structurally identical: both
build a `MicrosoftBenzeneServiceContainer`, run the `StartUp`'s three lifecycle methods against
their own platform's `IBenzeneApplicationBuilder`, and register the result with the host's
`IServiceCollection` via `hostBuilder.ConfigureServices(...)`. They differ only in what they
register the built result *as*.

**Azure Functions isolated worker** (`Benzene.Azure.Function.Core`) registers a scoped
`IAzureFunctionApp` for trigger functions to inject and dispatch through:

```csharp
public static IHostBuilder UseBenzene<TStartUp>(this IHostBuilder hostBuilder)
    where TStartUp : BenzeneStartUp, new()
{
    var startUp = new TStartUp();
    var configuration = startUp.GetConfiguration();
    return hostBuilder.ConfigureServices((_, services) =>
    {
        services.AddLogging();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new AzureFunctionAppBuilder(container);

        startUp.ConfigureServices(services, configuration);
        startUp.Configure(builder, configuration);

        services.AddScoped<IAzureFunctionApp>(serviceProvider =>
            builder.Create(new MicrosoftServiceResolverFactory(serviceProvider)));
    });
}
```

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
```

A single catch-all trigger function then injects `IAzureFunctionApp` and calls
`HandleHttpRequest(...)` (or `HandleEventHub(...)`/`HandleKafkaEvents(...)`). See
[Azure Functions Setup](azure-functions.md) for the full walkthrough, including why only the
isolated worker model (not the legacy in-process `Microsoft.Azure.WebJobs` model) is supported.

**Generic Worker host** (`Benzene.HostedService`) registers a singleton `IHostedService` instead,
so `StartUp`s that wire up background `IBenzeneWorker`s via `UseWorker(...)` start and stop with
the host's normal lifecycle:

```csharp
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
```

```csharp
var host = new HostBuilder().UseBenzene<StartUp>().Build();
host.Run();
```

Use `UseWorker(w => w.Add(resolverFactory => new MyWorker()))` in `Configure` to register one or
more `IBenzeneWorker`s (each with `StartAsync`/`StopAsync`) against the `WorkerApplicationBuilder`
this host passes in.

#### Worker concurrency

The three dispatcher-based self-hosted workers - `Benzene.Kafka.Core.BenzeneKafkaWorker<TKey,TValue>`
(a Kafka consumer), `Benzene.SelfHost.Http.BenzeneHttpWorker` (an `HttpListener` accept loop), and
`Benzene.RabbitMq.RabbitMqWorker` (a RabbitMQ `AsyncEventingBasicConsumer`) - are
where mode 3 above ("Benzene decides how many events at once") actually matters. All are built on
`Benzene.SelfHost.BoundedConcurrentDispatcher<T>`, a shared primitive on top of
`System.Threading.Channels` (part of the BCL - no new NuGet dependency):

- **`ConcurrentRequests`** (on `BenzeneKafkaConfig`/`BenzeneHttpConfig`/`RabbitMqConfig`) caps the
  maximum number of messages/requests handled concurrently.
- **`PreserveOrderPerPartition`** (`BenzeneKafkaConfig` only, default `true`) - Kafka only ever
  promises order within a partition, so by default messages from the same partition are routed to
  the same dispatcher lane and handled strictly in order, while different partitions still run
  concurrently up to `ConcurrentRequests`. Set to `false` for unordered round-robin dispatch when
  throughput matters more than per-partition order. `BenzeneHttpWorker` and `RabbitMqWorker` have no
  equivalent ordering key - requests/deliveries always round-robin across lanes (RabbitMQ makes no
  ordering promise across a queue once more than one delivery is in flight).
- **`PrefetchCount`** (`RabbitMqConfig` only, default 5) - the consumer QoS bounding how many
  unacknowledged deliveries the broker sends at once; set it at or above `ConcurrentRequests` so
  every lane can stay fed. (Kafka's fetch/`BenzeneHttpWorker`'s accept loop pull work themselves, so
  they have no prefetch knob.)
- **`DrainTimeout`** (default 30 seconds on all three configs) - how long `StopAsync` waits for
  in-flight work to finish before abandoning it and closing the consumer/listener. Each worker's
  `StopAsync` signals shutdown and awaits this drain, rather than closing the consumer/listener out
  from under in-flight handlers the way an earlier, simpler implementation did. (`RabbitMqWorker`
  additionally cancels its consumer first, so the drain only waits on already-in-flight deliveries.)

The Azure self-hosted workers - `Benzene.Azure.ServiceBus.BenzeneServiceBusWorker` (a
`ServiceBusProcessor` consuming a queue/subscription),
`Benzene.Azure.EventHub.BenzeneEventHubWorker` (an `EventProcessorClient` consuming a hub with
blob-checkpointed offsets), and `Benzene.Azure.CosmosDb.BenzeneCosmosChangeFeedWorker<TDocument>`
(a Change Feed Processor consuming a Cosmos DB container's change feed with Cosmos-lease-container
checkpoints) - are mode 3 too, but don't use `BoundedConcurrentDispatcher<T>`:
their SDK processors already own bounded concurrency natively. Service Bus concurrency is capped
by `BenzeneServiceBusConfig.MaxConcurrentCalls` (the processor's own `MaxConcurrentCalls`);
Event Hubs processes partitions concurrently with strictly one event at a time per partition
(the same ordering promise as `PreserveOrderPerPartition = true`, with no unordered opt-out);
the Cosmos change feed delivers one ordered batch at a time per lease (partition key range), with
leases running concurrently.
`StopAsync` on all three delegates to the processor's own stop, which waits for in-flight handlers.
The same is true of `Benzene.Aws.Sqs`'s `SqsConsumer`, whose "concurrency" is simply the poll
batch (`MaxNumberOfMessages`) dispatched per iteration.

This is a separate, smaller concern from the *serverless* batch adapters (SQS in
`Benzene.Aws.Lambda.Sqs`; Event Hubs/Kafka/Service Bus triggers in the `Benzene.Azure.Function.*`
packages), which today dispatch an entire batch via an uncapped `Task.WhenAll` - capping that
concurrency is a tracked performance follow-up.

### ASP.NET Core — `WebApplicationBuilder.UseBenzene<TStartUp>()` / `IApplicationBuilder.UseBenzene()`

Package: `Benzene.AspNet.Core`. Unlike the other adapters, ASP.NET Core's `Configure` step can't
run until *after* `WebApplicationBuilder.Build()` — the built `IApplicationBuilder`/`HttpContext`
pipeline doesn't exist beforehand — so this adapter splits `StartUp` construction across two
calls instead of one:

```csharp
public static WebApplicationBuilder UseBenzene<TStartUp>(this WebApplicationBuilder builder)
    where TStartUp : BenzeneStartUp, new()
{
    var startUp = new TStartUp();
    var configuration = startUp.GetConfiguration();
    startUp.ConfigureServices(builder.Services, configuration);
    builder.Services.AddSingleton(new BenzeneStartUpHolder(startUp, configuration));
    return builder;
}

public static IApplicationBuilder UseBenzene(this IApplicationBuilder app)
{
    var holder = app.ApplicationServices.GetRequiredService<BenzeneStartUpHolder>();
    var aspApplicationBuilder = new AspApplicationBuilder(app);
    aspApplicationBuilder.Register(x => x.AddBenzene());
    holder.StartUp.Configure(aspApplicationBuilder, holder.Configuration);
    return app;
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();

var app = builder.Build();
app.UseBenzene();
app.Run();
```

`WebApplicationBuilder.UseBenzene<TStartUp>()` runs `GetConfiguration`/`ConfigureServices` and
stashes the `StartUp` instance and configuration in a singleton for later. `IApplicationBuilder.UseBenzene()`
(the zero-arg overload, called after `Build()`) retrieves that holder and runs `Configure`
against a real `AspApplicationBuilder` wrapping the built `IApplicationBuilder`. Inside
`Configure`, use `app.UseHttp(http => http.UseMessageHandlers())` to wire up routes — `UseHttp`
is a no-op `IBenzeneApplicationBuilder` extension on any platform other than ASP.NET Core, same
pattern as `UseAwsLambda`/`UseWorker`. See [ASP.NET Core Integration](asp-net-core.md) for more
detail on request routing.

**Lean Kestrel (no MVC/routing baggage).** Swap `WebApplication.CreateBuilder(args)` for
`WebApplication.CreateSlimBuilder(args)` — the slim builder starts from just Kestrel + configuration
+ logging and opts you into nothing else (it's the trim/AOT-friendly host Microsoft ships for exactly
this "no framework baggage" case). Benzene layers on top unchanged, so this is the leanest Kestrel
host and the recommended replacement for the now-deprecated `Benzene.SelfHost.Http` (see
[Deprecations](deprecations.md)):

```csharp
var builder = WebApplication.CreateSlimBuilder(args).UseBenzene<StartUp>();
var app = builder.Build();
app.UseBenzene();
app.Run();
```

Kestrel only ships in the `Microsoft.AspNetCore.App` shared framework, so this still references it —
there is no supported standalone Kestrel package. If you truly need HTTP with no ASP.NET Core shared
framework at all, that's what `Benzene.SelfHost.Http` (`HttpListener`) did, and it's deprecated for
the performance reasons in [Deprecations](deprecations.md).

### gRPC on ASP.NET Core

Package: `Benzene.Grpc.AspNet`. Rides the same ASP.NET Core host adapter as `UseHttp` above — same
`WebApplicationBuilder.UseBenzene<TStartUp>()`/`app.UseBenzene()` pair, same `AspApplicationBuilder`
— just a second, independent `Use*` extension you can call alongside (or instead of) `UseHttp` in
the same `Configure` method, exactly like `UseAwsLambda`/`UseWorker` can coexist:

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddBenzeneGrpc();
    services.UsingBenzene(x => x.AddMessageHandlers(typeof(SayHelloMessageHandler).Assembly));
}

public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    => app.UseGrpc(grpc => grpc.UseMessageHandlers());
```

```csharp
var app = builder.Build();
app.MapGrpcService<GreeterService>();   // still needed - see below
app.UseBenzene();
app.Run();
```

Two things are easy to miss because gRPC's hosting model differs from Benzene's other transports:

- **`AddBenzeneGrpc()` in `ConfigureServices` is required, in addition to `UsingBenzene(...)`.** It
  registers ASP.NET Core's own gRPC services and `BenzeneInterceptor` as a server interceptor —
  `UseGrpc` alone isn't enough, because the interceptor is activated by ASP.NET Core's own
  per-request DI, not Benzene's pipeline-building container.
- **You still need `app.MapGrpcService<TService>()`** for a generated `ServiceBase`-derived class,
  even for methods `BenzeneInterceptor` will always claim — gRPC's own routing/reflection needs
  something to bind to. Methods with a matching `[GrpcMethod]`-tagged handler never actually reach
  that class's method body; unmatched methods fall through to it normally.

See [gRPC Setup](getting-started-grpc.md) for the full walkthrough, including all four RPC shapes,
metadata, status-code mapping, and the optional health check/reflection services.

## `IBenzeneInvocation` and `IBenzeneInvocationAccessor`

Package: `Benzene.Abstractions.Pipelines` (interfaces, namespace `Benzene.Abstractions.Hosting`),
implemented in `Benzene.Core.Middleware`.

`IBenzeneInvocation` is a platform-neutral bag of metadata about the current invocation, so a
handler can stay portable across hosts while still reaching native platform context when it
genuinely needs to:

```csharp
public interface IBenzeneInvocation
{
    string InvocationId { get; }
    string Platform { get; }
    T? GetFeature<T>() where T : class;
}
```

- **`InvocationId`** — an identifier unique enough to correlate logs/traces for this invocation
  (the AWS Lambda request ID, the ASP.NET Core trace identifier, or the isolated worker's
  invocation ID).
- **`Platform`** — matches `IBenzeneApplicationBuilder.Platform` for the host that populated it
  (`"AwsLambda"`, `"AspNet"`, `"AzureFunctions"`, ...).
- **`GetFeature<T>()`** — returns the native platform feature of type `T` for this invocation
  (`ILambdaContext` on AWS Lambda, `HttpContext` on ASP.NET Core, `FunctionContext` on Azure
  Functions), or `null` if this invocation's platform doesn't expose one. Handlers that never
  call `GetFeature<T>()` compile and run unchanged on every platform; only the call site that
  opts into a specific feature type is tied to that platform.

`IBenzeneInvocationAccessor` is the scoped mutable holder hosting-platform middleware uses to
populate `IBenzeneInvocation` — application code should depend on `IBenzeneInvocation` directly
and never touch the accessor.

### Enabling it: `UseBenzeneInvocation()`

Registration and middleware are added via `UseBenzeneInvocation()`, called on the pipeline
builder inside `Configure`:

```csharp
public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
    .UseAwsLambda(aws => aws
        .UseBenzeneInvocation()
        .Use("MyMiddleware", resolver => async (context, next) =>
        {
            var invocation = resolver.GetService<IBenzeneInvocation>();
            // invocation.InvocationId, invocation.Platform, invocation.GetFeature<ILambdaContext>()
            await next();
        }));
```

The platform-neutral `Benzene.Core.Middleware.BenzeneInvocationExtensions.UseBenzeneInvocation<TContext>(Func<IServiceResolver, TContext, IBenzeneInvocation> factory)`
overload takes a factory and registers `IBenzeneInvocationAccessor`/`IBenzeneInvocation` in DI;
each platform then exposes its own zero-argument overload that supplies that factory:

- AWS Lambda (`Benzene.Aws.Lambda.Core`): `IMiddlewarePipelineBuilder<AwsEventStreamContext>.UseBenzeneInvocation()` — `InvocationId` = Lambda request ID, `GetFeature<ILambdaContext>()`.
- ASP.NET Core (`Benzene.AspNet.Core`): `IMiddlewarePipelineBuilder<AspNetContext>.UseBenzeneInvocation()` — `InvocationId` = `HttpContext.TraceIdentifier`, `GetFeature<HttpContext>()`.
- Azure Functions (`Benzene.Azure.Function.Core`) needs **two** calls, because the isolated
  worker has no single request-flowing pipeline to populate the invocation from — it dispatches
  each trigger type through its own separate pipeline:
  1. `IBenzeneApplicationBuilder.UseBenzeneInvocation()` in `BenzeneStartUp.Configure`, to
     register `IBenzeneInvocation`/`IBenzeneInvocationAccessor`.
  2. `IFunctionsWorkerApplicationBuilder.UseBenzene()` in `Program.cs`
     (`.ConfigureFunctionsWebApplication(worker => worker.UseBenzene())`), to actually populate
     the accessor per invocation with `InvocationId` = the worker's invocation ID and
     `GetFeature<FunctionContext>()`. This method is worker-middleware registration, distinct
     from (and in addition to) `IHostBuilder.UseBenzene<TStartUp>()`.

### Populated per pipeline scope — not automatically nested

`IBenzeneInvocation` should be resolved as a scoped dependency. It is populated **once per
pipeline** by whichever level's `UseBenzeneInvocation()` you called — the same way log-context
enrichers like `UseLogResult`/`UseLogContext` work. Call it at whichever pipeline level you need
it resolvable from.

It **does not automatically flow into a nested sub-application that creates its own DI scope** —
for example AWS's `UseBenzeneMessage`/per-message SQS or SNS batch dispatch, where each message
in a batch gets its own scope. If you need `IBenzeneInvocation` resolvable inside that inner
pipeline too, call `UseBenzeneInvocation()` on that inner pipeline builder as well, not just the
outer one.

Resolving `IBenzeneInvocation` before any `UseBenzeneInvocation()` call has populated it for the
current scope throws a `BenzeneException` with a message explaining exactly that.

## Testing

`Benzene.Testing`'s `BenzeneTestHost.Create<TStartUp>()` builds an in-memory host from a
`BenzeneStartUp`-based app, with `WithConfiguration`/`WithServices` overrides layered on top,
without needing a real cloud host. See [Testing Benzene](testing-benzene.md) for the full guide,
including the per-platform `Build*` extensions (`BuildAwsLambdaHost()`, `BuildAzureFunctionApp()`)
and how to send events/messages into the built host.

## Autofac

`BenzeneStartUp` itself is fixed to Microsoft's `IServiceCollection` — its declaration is
`IStartUp<IServiceCollection, IConfiguration, IBenzeneApplicationBuilder>` — so the unified
cross-platform model described above is Microsoft DI-only today.

Autofac support instead works at the container level, independent of
`BenzeneStartUp`/`IBenzeneApplicationBuilder`. `Benzene.Autofac` provides:

- `AutofacBenzeneServiceContainer` — adapts `ContainerBuilder` to `IBenzeneServiceContainer`.
- `AutofacServiceResolverFactory` — adapts a built Autofac container to `IServiceResolverFactory`.
- `ContainerBuilder.UsingBenzene()` — registers Benzene's own services against a `ContainerBuilder`
  directly, the Autofac equivalent of `IServiceCollection.UsingBenzene()`.

Use these adapters wherever you build the container yourself and wire Benzene against it directly.
Because `BenzeneStartUp`/`IBenzeneApplicationBuilder` are fixed to `IServiceCollection`, the unified
hosts — including AWS Lambda via `AwsLambdaHost<TStartUp>` — run on Microsoft DI only.

## See Also

- [AWS Lambda Setup](getting-started-aws.md)
- [Azure Functions Setup](azure-functions.md)
- [ASP.NET Core Integration](asp-net-core.md)
- [gRPC Setup](getting-started-grpc.md)
- [Testing Benzene](testing-benzene.md)
- [Middleware](middleware.md)
- [Message Handlers](message-handlers.md)
