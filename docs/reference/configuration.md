# Configuration Reference

Benzene builds on the standard `Microsoft.Extensions.Configuration` and
`Microsoft.Extensions.DependencyInjection` stacks — there's no bespoke configuration system to
learn. This page covers how a service is configured: the start-up lifecycle, where configuration
values come from on each host, and the option classes individual packages expose.

## The `BenzeneStartUp` lifecycle

`BenzeneStartUp` (in `Benzene.Microsoft.Dependencies`) is the platform-neutral application
definition. You derive from it once and run the same class on any host — AWS Lambda, Azure
Functions, ASP.NET Core, a self-hosted worker — because it knows nothing about the transport.

```csharp
public abstract class BenzeneStartUp
{
    public abstract IConfiguration GetConfiguration();
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    public abstract void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration);
}
```

The three members run in order, once, on start-up (cold start on serverless hosts):

| Member | Runs | Responsibility |
|---|---|---|
| `GetConfiguration()` | first | Build and return the `IConfiguration` for the service. Its result is passed into both methods below. |
| `ConfigureServices(services, configuration)` | second | Register services and handlers with DI — `services.UsingBenzene(x => x.AddMessageHandlers(...))`, your repositories, validators, etc. |
| `Configure(app, configuration)` | third | Build the middleware pipeline — `app.UseAwsLambda(...)`, `app.UseHttp(...)`, and the steps inside them. |

```csharp
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(MyHandler).Assembly));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) =>
        app.UseAwsLambda(events => events
            .UseApiGateway(http => http.UseMessageHandlers()));
}
```

## Building configuration

`GetConfiguration()` returns a plain `IConfiguration`, so **any** configuration provider works —
environment variables, JSON files, AWS Systems Manager Parameter Store, AWS Secrets Manager,
Azure App Configuration, and so on:

```csharp
public override IConfiguration GetConfiguration() =>
    new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("config.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
```

Reference `Microsoft.Extensions.Configuration` and the specific provider packages you use; only
the abstractions come in transitively. Read values in `ConfigureServices`/`Configure` with
`configuration["Key"]` or bind sections with `configuration.GetSection(...).Get<T>()`.

## Where configuration comes from per host

Each host has a natural configuration source; the `BenzeneStartUp` above stays the same:

| Host | Typical source |
|---|---|
| **AWS Lambda** | Environment variables (set in the SAM template, console, or CDK/Terraform), read via `.AddEnvironmentVariables()`. See [AWS Lambda Setup](../getting-started-aws.md#configuration). |
| **Azure Functions** | `local.settings.json` locally, Function App application settings when deployed. See [Azure Functions Setup](../azure-functions.md). |
| **ASP.NET Core** | `appsettings.json` / `config.json` and environment variables via the standard host builder. See [ASP.NET Core](../asp-net-core.md). |

## Two ways to wire ASP.NET Core

ASP.NET Core supports both the platform-neutral start-up and an inline style:

```csharp
// A) Platform-neutral BenzeneStartUp — reuse the same class as Lambda/Azure:
builder.UseBenzene<StartUp>();     // runs GetConfiguration + ConfigureServices
var app = builder.Build();
app.UseBenzene();                  // runs Configure

// B) Inline — register and configure directly in Program.cs (see Getting Started):
builder.Services.UsingBenzene(x => x.AddMessageHandlers(typeof(MyHandler).Assembly));
var app = builder.Build();
app.UseBenzene(benzene => benzene.UseHttp(http => http.UseMessageHandlers()));
```

## Package configuration option classes

Some packages take a strongly-typed options object rather than raw configuration keys. Populate
these however you like — inline, or bound from `IConfiguration`.

### `CorsSettings` — `Benzene.Http`

Passed to [`UseCors(...)`](middleware.md#usecorscorssettings-corssettings).

| Property | Purpose |
|---|---|
| `AllowedDomains` | Origins allowed to call the API (`"*"` allows all — avoid in production). |
| `AllowedHeaders` | Headers echoed in `Access-Control-Allow-Headers`. |

### `SqsConsumerConfig` — `Benzene.Aws.Sqs`

Configures the SQS consumer when polling a queue directly (outside a Lambda trigger).

| Property | Default | Purpose |
|---|---|---|
| `QueueUrl` | — | URL of the queue to consume from. |
| `MaxNumberOfMessages` | — | Maximum messages fetched per receive call. |
| `WaitTimeSeconds` | `1` | Long-poll wait time per receive call. |

### `BenzeneKafkaConfig` — `Benzene.Kafka.Core`

Configures the Kafka consumer/client.

| Property | Default | Purpose |
|---|---|---|
| `ConsumerConfig` | — | The underlying Confluent `ConsumerConfig` (brokers, group ID, etc.). |
| `Topics` | — | Topics to subscribe to. |
| `ConcurrentRequests` | `5` | Number of records processed concurrently. |
| `PreserveOrderPerPartition` | `true` | Routes same-partition messages to the same dispatcher lane so they're handled in order. |
| `DrainTimeout` | `30s` | How long `StopAsync` waits for in-flight handlers before abandoning them. |
| `ConsumeExceptionRetryDelay` | `1s` | Backoff between retries after a `ConsumeException`. |
| `CatchHandlerExceptions` | `true` | Whether an unhandled handler exception is caught and logged (that lane keeps consuming) or left to stop the whole worker. |
| `CommitOnlyOnSuccess` | `false` | Whether an offset is only stored after its handler succeeds (at-least-once, redelivers on failure/crash) instead of being auto-stored as soon as it's consumed. Requires `CatchHandlerExceptions = false` and `PreserveOrderPerPartition = true`. |

### Retry options — `Benzene.Resilience`

`UseRetry(...)` takes its settings as method parameters rather than an options class — see
[`UseRetry`](middleware.md#useretry) for the full list (`numberOfRetries`, `initialDelay`,
`backoffFactor`, `shouldRetry`, …).

## See also

- [Getting Started](../getting-started.md) — configuration in a running service.
- [AWS Lambda Setup](../getting-started-aws.md#configuration) — Lambda configuration specifics.
- [Middleware Reference](middleware.md) — the steps you add in `Configure`.
- [Package Reference](packages.md) — which package each option class ships in.
