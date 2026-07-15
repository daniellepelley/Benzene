# Testing Benzene

You can build an in-memory host from your real production `StartUp` class (the same one deployed
to the cloud) and use it to send requests or messages straight into the pipeline. Configuration
and service registrations can be overridden, so you can introduce mocks or point at a locally
running component such as a database. This is the recommended approach to testing services, since
it exercises the whole pipeline end to end rather than individual middleware in isolation.

## Testing a `BenzeneStartUp`-based app (recommended)

If your `StartUp` derives from `BenzeneStartUp` (see [AWS Lambda Setup](getting-started-aws) /
[Azure Functions Setup](azure-functions)), `BenzeneTestHost` builds a test host from it directly —
one API regardless of which platform(s) your `Configure` method wires up.

```csharp
var host = new AwsLambdaBenzeneTestHost(
    BenzeneTestHost.Create<StartUp>()
        .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
        .BuildAwsLambdaHost());
```

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
[`AwsLambdaHost<TStartUp>`](getting-started-aws) performs for a real deployment. Wrap it in
`AwsLambdaBenzeneTestHost` (from `Benzene.Tools`) to send events into it and get typed responses
back:

```csharp
var host = new AwsLambdaBenzeneTestHost(
    BenzeneTestHost.Create<StartUp>()
        .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
        .BuildAwsLambdaHost());

var message = MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" });
var response = await host.SendBenzeneMessageAsync(message);
```

`SendBenzeneMessageAsync` works against any StartUp that wires up `UseBenzeneMessage(...)`. If your
StartUp also wires up API Gateway, SQS, or SNS, the matching `Send*Async` extension from that
transport's `*.TestHelpers` package (e.g. `SendApiGatewayAsync`, `SendSqsAsync`) works the same
way, off the same host.

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
[ASP.NET Core Integration](asp-net-core)), rather than a Benzene-specific dispatch helper. Since
your app already *is* a standard ASP.NET Core app, `WebApplicationFactory`/`TestServer` exercises
the real request pipeline (routing, model binding, middleware ordering) that a hand-rolled request
object wouldn't, and gives you a real `HttpClient` to call `PostAsync`/`GetAsync` on. Override
services the normal ASP.NET Core way, via `WithWebHostBuilder(b => b.ConfigureServices(...))`.

### Worker / generic host

A `BenzeneStartUp` that only wires up background `IBenzeneWorker`s via `UseWorker(...)` isn't
request/response-shaped, so there's no `Send*Async` to call. Build the real host and drive its
lifecycle directly:

```csharp
var host = new HostBuilder().UseBenzene<StartUp>().Build();
var hostedServices = host.Services.GetServices<IHostedService>().ToList();

foreach (var service in hostedServices) await service.StartAsync(CancellationToken.None);
// ... assert on your worker's behavior ...
foreach (var service in hostedServices) await service.StopAsync(CancellationToken.None);
```
