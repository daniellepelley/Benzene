# Azure Functions Setup

Benzene runs on the Azure Functions **isolated worker** model, using the same
platform-neutral `BenzeneStartUp` base class as every other Benzene host. This guide starts
from an empty folder and ends with a deployed Function App handling HTTP requests.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An Azure subscription, with the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
  configured, if you want to deploy

## 1. Create the project

```bash
mkdir MyFunction && cd MyFunction
dotnet new classlib -f net10.0
```

Add the Azure Functions isolated-worker properties to the `.csproj` (`OutputType` must be
`Exe` — a Function App is a runnable worker process, not a plain library):

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <AzureFunctionsVersion>v4</AzureFunctionsVersion>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

## 2. Install the NuGet packages

First, the standard Microsoft packages every isolated-worker Function App needs — these
must be referenced directly in your function app project (not just transitively) so the
Functions SDK's build step can discover them and generate the worker's extension manifest:

```bash
dotnet add package Microsoft.Azure.Functions.Worker
dotnet add package Microsoft.Azure.Functions.Worker.Sdk
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
```

Then Benzene's packages, published as prerelease (`-alpha`) versions, so `--prerelease` is
required until 1.0:

```bash
dotnet add package Benzene.Azure.Function.Core --prerelease
dotnet add package Benzene.Azure.Function.AspNet --prerelease
```

`Benzene.Azure.Function.Core` brings in the middleware pipeline, message handler
infrastructure, `BenzeneStartUp` base class, and the isolated-worker hosting glue,
transitively. `Benzene.Azure.Function.AspNet` adds the `UseHttp` middleware for handling HTTP
requests as ASP.NET Core `HttpRequest`/`IActionResult`. Add `Benzene.Azure.Function.EventHub`
or `Benzene.Azure.Function.Kafka` the same way if your function also needs to handle those
event sources (see [Supported Event Sources](#supported-event-sources) below) — each has a
corresponding direct Microsoft package too (`Microsoft.Azure.Functions.Worker.Extensions.EventHubs`
or `Microsoft.Azure.Functions.Worker.Extensions.Kafka`).

## 3. Define a message handler

Business logic lives in message handlers, not in the trigger function — this keeps it
testable and portable across hosts. See [Message Handlers](message-handlers) for the full
picture; the minimal shape is:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

[Message("hello:world")]
[HttpEndpoint("GET", "/hello/{name}")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldRequest, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldRequest message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}!" }));
    }
}

public class HelloWorldRequest
{
    public string Name { get; set; }
}

public class HelloWorldResponse
{
    public string Message { get; set; }
}
```

`[Message]` maps the handler to a topic; `[HttpEndpoint]` maps an HTTP method and path to that
same topic. Both attributes are discovered by reflection, so there is nothing further to
register per-handler.

## 4. Define your StartUp

`BenzeneStartUp` is the platform-neutral application definition shared by every Benzene host —
the same class shape you'd write for AWS Lambda, ASP.NET Core, or a console app. Configure the
HTTP pipeline via `UseHttp`:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddHttpMessageHandlers());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http.UseMessageHandlers());
    }
}
```

## 5. Wire up the isolated worker host

`Program.cs` registers `StartUp` with the isolated worker's `IHostBuilder`:

```csharp
using Benzene.Azure.Function.Core;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
```

`ConfigureFunctionsWebApplication()` turns on the ASP.NET Core integration (advanced HTTP
features, `HttpRequest`/`IActionResult` trigger bindings); `UseBenzene<StartUp>()` runs your
`StartUp`'s `GetConfiguration`/`ConfigureServices`/`Configure` and registers the built
`IAzureFunctionApp` for injection into trigger functions.

Then add a single catch-all HTTP trigger function that delegates to Benzene's own routing —
one Azure Function handles every route your message handlers define:

```csharp
using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

public class HttpFunction
{
    private readonly IAzureFunctionApp _app;

    public HttpFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("http")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "{*restOfPath}")] HttpRequest req)
    {
        return await _app.HandleHttpRequest(req);
    }
}
```

## 6. Configuration

`GetConfiguration()` runs once on cold start, before any services are registered, and its
result is passed into both `ConfigureServices` and `Configure`. Anything built on top of
`Microsoft.Extensions.Configuration` works here — the example above reads environment
variables (which map to Application Settings once deployed), but `AddJsonFile(...)`, Azure App
Configuration, or Azure Key Vault configuration providers all work the same way.

For local development, add a `local.settings.json` (not checked into source control — it holds
secrets and machine-specific values):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

Run it locally with:

```bash
func start
```

## 7. Deploy

Create the Function App resource (a Consumption-plan example; adjust SKU/plan for your needs):

```bash
az group create --name my-function-rg --location eastus
az storage account create --name mystorageacct --location eastus --resource-group my-function-rg --sku Standard_LRS
az functionapp create --resource-group my-function-rg --consumption-plan-location eastus \
  --runtime dotnet-isolated --functions-version 4 --name my-function-app --storage-account mystorageacct
```

Then publish:

```bash
func azure functionapp publish my-function-app
```

Once deployed, `GET` the printed URL at `/api/hello/world` to confirm the handler above
responds (the `api` prefix is the default Azure Functions route prefix — clear it via
`"routePrefix": ""` in `host.json`'s `extensions.http` section if you'd rather not have it).

## Supported Event Sources

Benzene provides specialized middleware for various Azure Functions triggers, each configured
inside the same `Configure` method, on the same platform-neutral `app` shown in step 4 — a
single `BenzeneStartUp` can wire up several trigger types at once, each with its own
sub-pipeline, exactly as with any other Benzene host:

- **HTTP**: `app.UseHttp(...)`, in `Benzene.Azure.Function.AspNet`
- **Event Hubs**: `app.UseEventHub(...)`, in `Benzene.Azure.Function.EventHub`
- **Kafka** (Event Hubs for Kafka): `app.UseKafka(...)`, in `Benzene.Azure.Function.Kafka`

### Event Hubs

```csharp
app.UseEventHub(eventHub => eventHub
    .UseBenzeneMessage(direct => direct
        .UseMessageHandlers()));
```

Requires an Event Hubs trigger function injecting `IAzureFunctionApp` and calling
`HandleEventHub(...)`, the same way `HttpFunction` above calls `HandleHttpRequest(...)`.

### Kafka

```csharp
app.UseKafka(kafka => kafka.UseMessageHandlers());
```

Works against Event Hubs' Kafka-compatible endpoint. The Kafka record's key/value are
`byte[]`; Benzene decodes the value as UTF-8 JSON the same way as every other transport.
Requires a Kafka trigger function injecting `IAzureFunctionApp` and calling
`HandleKafkaEvents(...)`.

## Notes

- **Hosting model**: only the isolated worker model is supported (not the legacy in-process
  model built on `Microsoft.Azure.WebJobs`) — the isolated worker is the model Microsoft
  recommends for all new development, and its `IHostBuilder`-based hosting matches the pattern
  every other Benzene host uses.
- **`IBenzeneInvocation`**: unlike AWS Lambda or ASP.NET Core, the isolated worker dispatches
  each trigger type through its own separate pipeline, so there's no single request-flowing
  middleware to populate `IBenzeneInvocation` from. If you need it, call
  `app.UseBenzeneInvocation()` in `Configure` and register the worker middleware in
  `Program.cs` via `.ConfigureFunctionsWebApplication(worker => worker.UseBenzene())`.

See [`examples/Azure`](../examples/Azure) for a complete, runnable project covering HTTP
routing, validation, and OpenAPI spec generation.
