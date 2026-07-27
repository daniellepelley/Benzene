# Testing Benzene

You can build an in-memory host from your real production `StartUp` class (the same one deployed
to the cloud) and use it to send requests or messages straight into the pipeline. Configuration
and service registrations can be overridden, so you can introduce mocks or point at a locally
running component such as a database. This is the recommended approach to testing services, since
it exercises the whole pipeline end to end rather than individual middleware in isolation.

## Testing a `BenzeneStartUp`-based app (recommended)

If your `StartUp` derives from `BenzeneStartUp` (see [AWS Lambda Setup](getting-started-aws.md) /
[Azure Functions Setup](azure-functions.md)), `BenzeneTestHost` builds a test host from it directly —
one API regardless of which platform(s) your `Configure` method wires up. Boot the real app, override
any registration with a fake, name the host with a single `Build*` line, then push a native event in
through the front door and assert on the response:

```csharp
var response = await BenzeneTestHost.Create<StartUp>()                          // boot the REAL app
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object)) // swap in a fake
    .BuildAwsLambdaHost()                                                       // the ONE host-specific line
    .SendApiGatewayAsync(HttpBuilder.Create("POST", "/orders", order));         // native event in, native response out

Assert.Equal(201, response.StatusCode);
```

Only the `BuildAwsLambdaHost()`/`SendApiGatewayAsync(...)` lines change per host/transport; everything
else is identical across AWS, Azure, GCP, and the workers.

### Override Configuration

```csharp
var app = BenzeneTestHost.Create<StartUp>()
    .WithConfiguration("some-key", "some-value")
    .BuildAzureFunctionApp();
```

`WithConfiguration` overrides sit on top of whatever `StartUp.GetConfiguration()` returns, applied
before `ConfigureServices` runs — useful for pointing dependencies at a locally running component
(e.g. via Docker) without touching real configuration files or environment variables.

### Override Services

```csharp
var app = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
    .BuildAzureFunctionApp();
```

`WithServices` actions run immediately after `StartUp.ConfigureServices`, so they can replace any
registration the StartUp made — the standard way to swap in fakes and mocks.

### AWS Lambda

`BuildAwsLambdaHost()` builds an `IAwsLambdaEntryPoint` — the same construction
[`AwsLambdaHost<TStartUp>`](getting-started-aws.md) performs for a real deployment. The `Send*Async`
extensions dispatch straight off it, so no wrapper is needed for the common case:

```csharp
var response = await BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
    .BuildAwsLambdaHost()
    .SendBenzeneMessageAsync(MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" }));
```

`SendBenzeneMessageAsync` works against any StartUp that wires up `UseBenzeneMessage(...)`. If your
StartUp also wires up API Gateway, SQS, or SNS, the matching `Send*Async` extension from that
transport's `*.TestHelpers` package works the same way, off the same host — each takes the shared
`HttpBuilder`/`MessageBuilder` (topic/route + payload + headers) and returns the transport's native
response:

| Transport | `*.TestHelpers` package | Send helper | Native response |
| --- | --- | --- | --- |
| API Gateway | `Benzene.Aws.Lambda.ApiGateway.TestHelpers` | `SendApiGatewayAsync(HttpBuilder.Create(...))` | `APIGatewayProxyResponse` |
| SQS | `Benzene.Aws.Lambda.Sqs.TestHelpers` | `SendSqsAsync(MessageBuilder.Create(...))` | `SQSBatchResponse` |
| BenzeneMessage | `Benzene.Core.MessageHandlers.TestHelpers` (+ the `IAwsLambdaEntryPoint` overload in `Benzene.Aws.Lambda.Core.TestHelpers`) | `SendBenzeneMessageAsync(MessageBuilder.Create(...))` | `BenzeneMessageResponse` |

If you need to send the same event to many handlers, or want to reuse one host across several sends,
wrap the entry point once in `AwsLambdaBenzeneTestHost` (from `Benzene.Aws.Lambda.Core.TestHelpers`)
and call `Send*Async` on the wrapper — the same extensions are available on both the wrapper and the
raw entry point:

```csharp
using var host = new AwsLambdaBenzeneTestHost(
    BenzeneTestHost.Create<StartUp>()
        .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
        .BuildAwsLambdaHost());

var response = await host.SendBenzeneMessageAsync(
    MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" }));
```

### Azure Functions

`BuildAzureFunctionApp()` builds an `IAzureFunctionApp` directly — no wrapper needed, since it
already exposes typed dispatch methods per transport:

```csharp
var app = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
    .BuildAzureFunctionApp();

var request = HttpBuilder.Create("GET", "/hello/world").AsAspNetCoreHttpRequest();
var response = await app.HandleHttpRequest(request) as ContentResult;
```

`HandleEventHub(...)` and `HandleKafkaEvents(...)` work the same way for those transports.
Azure's `BenzeneMessage` bridge today only exists over Event Hub (`UseBenzeneMessage` inside
`UseEventHub`) — send a `MessageBuilder` through `.AsEventHubBenzeneMessage()` and
`HandleEventHub(...)` to exercise it. There is no bare `SendBenzeneMessageAsync` for Azure yet, the
way there is for AWS, since Azure has no direct (non-Event-Hub) `BenzeneMessageRequest` entry
point registered today.

### ASP.NET Core

For ASP.NET Core, use the framework's own [`WebApplicationFactory`](https://learn.microsoft.com/aspnet/core/test/integration-tests)
against a `Program` that calls `builder.UseBenzene<StartUp>()` / `app.UseBenzene()` (see
[ASP.NET Core Integration](asp-net-core.md)), rather than a Benzene-specific dispatch helper. Since
your app already *is* a standard ASP.NET Core app, `WebApplicationFactory`/`TestServer` exercises
the real request pipeline (routing, model binding, middleware ordering) that a hand-rolled request
object wouldn't, and gives you a real `HttpClient` to call `PostAsync`/`GetAsync` on. Override
services the normal ASP.NET Core way, via `WithWebHostBuilder(b => b.ConfigureServices(...))`.

### Message-consuming workers (Kafka, RabbitMQ, Service Bus, SQS)

A `BenzeneStartUp` that wires a message-consuming worker via `UseWorker(...)` has a `Build*Host`
bridge in that transport's `*.TestHelpers` package, so you can drive its pipeline the same way — push
a native record/message in and assert, no running broker or credentials:

| Worker | `*.TestHelpers` package | Build host | Send helper (native event via `MessageBuilder.Create(...).As*()`) |
| --- | --- | --- | --- |
| Kafka | `Benzene.Kafka.Core.TestHelpers` | `BuildKafkaHost<StartUp, TKey, TValue>()` | `SendKafkaAsync(...)` / `HandleAsync(...)` |
| RabbitMQ | `Benzene.RabbitMq.TestHelpers` | `BuildRabbitMqHost<StartUp>()` | `SendRabbitMqAsync(...)` / `HandleAsync(...)` |
| Service Bus | `Benzene.Azure.ServiceBus.TestHelpers` | `BuildServiceBusWorkerHost<StartUp>()` | `SendServiceBusAsync(...)` / `HandleAsync(...)` |
| SQS (standalone poller) | `Benzene.Aws.Sqs.TestHelpers` | `BuildSqsConsumerHost<StartUp>()` | `SendSqsAsync(...)` / `HandleAsync(...)` |

```csharp
using var host = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddSingleton<IGreeter>(spyGreeter))
    .BuildKafkaHost<StartUp, Ignore, string>();

await host.SendKafkaAsync(
    MessageBuilder.Create("hello_world", new HelloWorldMessage { Name = "World" }).AsKafkaBenzeneMessage());
```

These workers are fire-and-forget, so assert on a handler-visible side effect (a fake registered via
`WithServices`, a recorded outbound call) rather than a returned HTTP-style response.

### Worker / generic host

A `BenzeneStartUp` that only wires up non-message background `IBenzeneWorker`s via `UseWorker(...)`
isn't request/response-shaped, so there's no `Send*Async` to call. Build the real host and drive its
lifecycle directly:

```csharp
var host = new HostBuilder().UseBenzene<StartUp>().Build();
var hostedServices = host.Services.GetServices<IHostedService>().ToList();

foreach (var service in hostedServices) await service.StartAsync(CancellationToken.None);
// ... assert on your worker's behavior ...
foreach (var service in hostedServices) await service.StopAsync(CancellationToken.None);
```
