# Distributed Tracing with OpenTelemetry

Set up end-to-end distributed tracing across two Benzene services with OpenTelemetry, so a single request shows up as one connected trace instead of a pile of disconnected spans.

## Problem Statement

You're running more than one Benzene service (say, an ASP.NET Core API that queues work for an SQS-backed worker) and need to:
- See a single request as one trace, spanning both services, in a real tracing backend (Jaeger, an OTel Collector, etc.)
- Get this largely for free from Benzene's built-in `Activity` instrumentation, rather than hand-rolling spans in every handler
- Understand exactly where trace continuity currently works and where it doesn't — Benzene's W3C trace context support is deliberately partial today, and pretending otherwise will cost you a confusing debugging session

This cookbook builds a real, worked example: an ASP.NET Core API that accepts an HTTP request and forwards it to an SQS queue, and a worker that consumes that queue. Both export to Jaeger via the OTLP protocol.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (to run Jaeger locally)
- Basic familiarity with [ASP.NET Core Integration](../asp-net-core.md) and [Monitoring & Diagnostics](../monitoring.md)
- An SQS queue to send to/consume from (a local one via [LocalStack](https://github.com/localstack/localstack) is fine for testing)

## Installation

Benzene's packages are published as prerelease (`-alpha`) versions, so `--prerelease` is required until 1.0.

On the ASP.NET Core API project:

```bash
dotnet add package Benzene.AspNet.Core --prerelease
dotnet add package Benzene.Diagnostics --prerelease
dotnet add package Benzene.Clients.Aws --prerelease
dotnet add package Benzene.OpenTelemetry --prerelease
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

On the SQS worker project:

```bash
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.Aws.Sqs --prerelease
dotnet add package Benzene.Diagnostics --prerelease
dotnet add package Benzene.OpenTelemetry --prerelease
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

`Benzene.OpenTelemetry` only depends on `OpenTelemetry`/`OpenTelemetry.Api` — it registers no exporter itself, so `OpenTelemetry.Exporter.OpenTelemetryProtocol` (for `AddOtlpExporter()`) is a separate, plain OpenTelemetry package you add yourself.

## Step-by-Step Implementation

### 1. Run an OTLP backend locally

Jaeger's all-in-one image accepts OTLP directly, so there's no separate collector needed for local testing:

```yaml
# docker-compose.yaml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"   # UI: http://localhost:16686
      - "4317:4317"     # OTLP gRPC receiver
      - "4318:4318"     # OTLP HTTP receiver
```

```bash
docker compose up -d
```

For a real deployment, swap Jaeger for an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) that fans out to whatever backend you actually run (Jaeger, Tempo, Honeycomb, Application Insights via its OTLP ingestion, ...) — nothing else in this cookbook changes.

### 2. Add automatic `Activity` spans to both services

`AddDiagnostics()` (from `Benzene.Diagnostics`) wraps every middleware in every pipeline in a `System.Diagnostics.Activity` span automatically — no explicit call needed per middleware, on either service:

```csharp
services.UsingBenzene(x => x
    .AddDiagnostics()
    .AddMessageHandlers(typeof(CreateOrderMessageHandler).Assembly));
```

`ActivitySource.StartActivity` is a documented no-op when nothing is listening, so this has no real cost until step 4 wires up an exporter.

### 3. Continue the trace across the HTTP boundary: `UseW3CTraceContext()`

On the API service, add `UseW3CTraceContext()` as the **first** middleware in the HTTP pipeline. It reads an inbound `traceparent`/`tracestate` header and starts the pipeline's root `Activity` with the parsed remote context as its parent, so a trace started by whatever called this API continues here instead of starting over:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;

public class StartUp : BenzeneStartUp
{
    // ... GetConfiguration / ConfigureServices as above ...

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http
            .UseW3CTraceContext()
            .UseMessageHandlers());
    }
}
```

Everything added after `UseW3CTraceContext()` inherits its `Activity` as the ambient `Activity.Current` parent, so every automatically-wrapped middleware span from `AddDiagnostics()` nests correctly under the remote trace. It falls back to a normal root span when the header is missing or fails to parse, so it's always safe to add — including here, even though (see the [ASP.NET Core Integration](../asp-net-core.md#w3c-trace-context) guide) ASP.NET Core's own hosting layer usually already extracts `traceparent` before your middleware runs. Keeping it explicit makes the same `StartUp` class behave identically if this handler is later also exposed through API Gateway or an Azure Functions HTTP trigger, neither of which have that built-in extraction.

### 4. Propagate the trace to the SQS worker: `WithW3CTraceContext()`

The API service's handler doesn't call the worker directly — it puts a message on an SQS queue via `Benzene.Clients`. Register the outbound client with `WithW3CTraceContext()`, which stamps `Activity.Current`'s `traceparent`/`tracestate` onto the outgoing message's headers:

```csharp
using Amazon.SQS;
using Benzene.Clients.Aws;
using Benzene.Clients.Aws.Sqs;
using Benzene.Clients.TraceContext;
using Benzene.Core.Middleware;

services.AddSingleton<IAmazonSQS>(new AmazonSQSClient());

services.UsingBenzene(x => x
    .AddDiagnostics()
    .AddBenzeneMessageClients(c => c
        .CreateSqsBenzeneMessageClient(
            "orders",
            configuration["ORDERS_QUEUE_URL"],
            new NullServiceResolver(),
            client => client.WithW3CTraceContext()))
    .AddMessageHandlers(typeof(CreateOrderMessageHandler).Assembly));
```

`SqsContextConverter` forwards `IBenzeneClientRequest.Headers` onto the real SQS message's `MessageAttributes` (alongside the `topic` attribute), so `traceparent`/`tracestate` genuinely go out on the wire, not just in Benzene's in-memory request object. The `NullServiceResolver` here (`Benzene.Core.Middleware`) is the client's own internal pipeline resolver, unrelated to per-request DI — it's what the test suite itself uses to construct a `SqsBenzeneMessageClient` outside of a request scope.

The handler itself is an ordinary message handler that forwards the incoming HTTP request onto the queue:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

[HttpEndpoint("POST", "/orders")]
[Message("order:create")]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly IBenzeneMessageClientFactory _clientFactory;

    public CreateOrderMessageHandler(IBenzeneMessageClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<IBenzeneResult<CreateOrderResponse>> HandleAsync(CreateOrderRequest request)
    {
        var orderId = Guid.NewGuid().ToString();
        var client = _clientFactory.Create();

        var result = await client.SendMessageAsync<ProcessOrderMessage, Void>(
            new BenzeneClientRequest<ProcessOrderMessage>(
                "order:process",
                new ProcessOrderMessage { OrderId = orderId, CustomerId = request.CustomerId },
                new Dictionary<string, string>()));

        return result.IsSuccessful
            ? BenzeneResult.Ok(new CreateOrderResponse { OrderId = orderId })
            : BenzeneResult.ServiceUnavailable<CreateOrderResponse>("Failed to queue order");
    }
}
```

### 5. Export both services' spans via OpenTelemetry

`AddDiagnostics()` produces `Activity` spans; `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` registers Benzene's `ActivitySource` (named `"Benzene"`) against an OTel `TracerProviderBuilder` so those spans actually go somewhere. Do this on **both** services — each is a separate OTLP exporter pointing at the same backend:

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;
using Benzene.OpenTelemetry;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddBenzeneInstrumentation()
    .AddOtlpExporter(otlp => otlp.Endpoint = new Uri("http://localhost:4317"))
    .Build();
```

Register this as a singleton disposed with the host (e.g. `services.AddSingleton(tracerProvider)` built once at startup, or wire it through `OpenTelemetry.Extensions.Hosting`'s `AddOpenTelemetry().WithTracing(t => t.AddBenzeneInstrumentation().AddOtlpExporter())` if you'd rather have it participate in the generic host's lifecycle — `AddBenzeneInstrumentation()` is a plain extension on OTel's own builder types, so it composes with either approach identically).

### 6. Set up the SQS worker

The worker side is a long-running `Benzene.Aws.Sqs` polling consumer, hosted via `Benzene.HostedService`:

```csharp
using Amazon.SQS;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Benzene.HostedService;
using Benzene.SelfHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class WorkerStartUp : BenzeneHostedServiceStartup
{
    public override IConfiguration GetConfiguration()
        => new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddDiagnostics()
            .AddMessageHandlers(typeof(ProcessOrderMessageHandler).Assembly));
    }

    public override void Configure(IBenzeneWorkerStartup app, IConfiguration configuration)
    {
        var sqsClient = new AmazonSQSClient();
        var sqsClientFactory = new SqsClientFactory(sqsClient);

        app.UseSqs(new SqsConsumerConfig
        {
            QueueUrl = configuration["ORDERS_QUEUE_URL"],
            MaxNumberOfMessages = 10
        },
        sqsClientFactory,
        sqsApp => sqsApp.UseMessageHandlers());
    }
}
```

```csharp
// Program.cs
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddHostedService<WorkerStartUp>())
    .Build();

await host.RunAsync();
```

And the handler that actually processes the queued message:

```csharp
using Benzene.Abstractions.MessageHandlers;

[Message("order:process")]
public class ProcessOrderMessageHandler : IMessageHandler<ProcessOrderMessage>
{
    public Task HandleAsync(ProcessOrderMessage message)
    {
        Console.WriteLine($"Processing order {message.OrderId} for customer {message.CustomerId}");
        return Task.CompletedTask;
    }
}

public class ProcessOrderMessage
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
}
```

### The current limitation, honestly stated

> `UseW3CTraceContext()` — the middleware that lets a service pick up an inbound trace and continue it — is only wired for HTTP-based transports today (ASP.NET Core, Azure Functions' ASP.NET-style trigger, API Gateway). SQS/SNS/Kafka/Event Hub inbound extraction is not yet implemented. That means in the exact setup above: the API service's outbound client correctly stamps `traceparent` onto the SQS message (step 4), but the worker has no supported way to read it back out and parent its own `Activity` on it. The worker's spans still show up in Jaeger — they're just a second, disconnected trace, not a continuation of the API's trace. If your downstream service were itself HTTP-based (another ASP.NET Core app, an Azure Function behind the ASP.NET-style trigger, a Lambda behind API Gateway) instead of an SQS consumer, `UseW3CTraceContext()` on that side would pick the parent back up and you'd get one unbroken trace end-to-end today.

## Testing

1. Start Jaeger (`docker compose up -d`) and both services (`dotnet run` in each project).
2. Send a request to the API:

   ```bash
   curl -X POST http://localhost:5000/orders -H "Content-Type: application/json" -d '{"customerId":"cust-1"}'
   ```
3. Open the Jaeger UI at `http://localhost:16686` and select the API service. You should see a trace containing spans for `W3CTraceContext.Root`, the HTTP middleware pipeline, and `CreateOrderMessageHandler`.
4. Select the worker service. You'll see a **separate** trace for the SQS consumer pipeline and `ProcessOrderMessageHandler` — per the limitation above, this is expected today, not a bug in your setup.
5. To confirm the header did go out on the wire (even though nothing reads it back yet), inspect the SQS message: `traceparent` will be present as a string message attribute alongside `topic`.

## Troubleshooting

### No spans show up in Jaeger at all

- Confirm `AddOtlpExporter()`'s endpoint matches Jaeger's OTLP port (`4317` for gRPC, `4318` for HTTP — the default `AddOtlpExporter()` uses gRPC against `4317`).
- `ActivitySource.StartActivity` is a no-op with nothing listening — double-check the `TracerProviderBuilder` is actually built (and not garbage-collected/disposed early) before any requests come in.
- Make sure `AddBenzeneInstrumentation()` was called on the `TracerProviderBuilder` — without it, the provider has no `AddSource("Benzene")` registration and silently drops Benzene's spans while still exporting anything else you've instrumented directly.

### Two separate traces instead of one continuous trace across both services

- If both services are HTTP-based, confirm `UseW3CTraceContext()` is the **first** middleware added, and that `WithW3CTraceContext()` is on the outbound client used to call the second service.
- If the downstream service is SQS/SNS/Kafka/Event Hub-based, this is the documented current limitation above, not a misconfiguration — there's nothing further to wire up until inbound extraction lands for that transport.

### `UseW3CTraceContext()` throws or the pipeline fails to resolve

- It resolves `IMessageHeadersGetter<TContext>` for whatever context type the pipeline runs on. On HTTP-based pipelines (ASP.NET Core's `AspNetContext`, API Gateway's `ApiGatewayContext`) this is registered automatically by `UseHttp`/`UseApiGateway`. If you're adding it to a custom or lower-level pipeline, make sure the corresponding header getter is registered first.

## Variations

### Console exporter for local debugging without Jaeger

Swap `AddOtlpExporter()` for `AddConsoleExporter()` (from `OpenTelemetry.Exporter.Console`) to print spans to the console instead of running a backend — useful for quickly checking that spans nest the way you expect before wiring up a real collector.

### Exporting Benzene's metrics alongside traces

Pair `AddDiagnostics()` with [`UseBenzeneMetrics()`](../common-middleware.md#usebenzenemetrics) to record `benzene.messages.processed`/`benzene.message.duration`, and add a `MeterProviderBuilder` the same way:

```csharp
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddBenzeneInstrumentation()
    .AddOtlpExporter()
    .Build();
```

### Routing to Application Insights instead of Jaeger

Application Insights accepts OTLP directly — point `AddOtlpExporter()`'s endpoint at your Application Insights OTLP ingestion endpoint instead of standing up Jaeger/a collector, or use Azure Monitor's own OpenTelemetry exporter package if you'd rather authenticate via connection string. Everything upstream of the exporter (`AddDiagnostics()`, `UseW3CTraceContext()`, `WithW3CTraceContext()`) is unchanged. See also [Logging to Application Insights](logging-application-insights.md) for the logging side of this same backend.

## Further Reading

- [Monitoring & Diagnostics](../monitoring.md) — the full picture of tracing, timers, logging, and OpenTelemetry
- [Common Middleware](../common-middleware.md) — `UseW3CTraceContext()` and `UseBenzeneMetrics()` reference
- [ASP.NET Core Integration](../asp-net-core.md) — why `UseW3CTraceContext()` is usually redundant (but still safe) on pure ASP.NET Core
- [Request Correlation Across Services](request-correlation.md) — when to reach for the legacy correlation-ID header instead of/alongside W3C trace context
- [Handling SQS Message Failures](handling-sqs-failures.md) — retry/DLQ patterns for the same SQS worker shape used here
- [Logging to Application Insights](logging-application-insights.md) — correlating logs with the traces this cookbook produces
