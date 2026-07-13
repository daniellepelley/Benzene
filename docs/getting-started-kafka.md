# Kafka Setup

Benzene ships three separate Kafka integrations — a platform-neutral self-hosted consumer/producer
built on `Confluent.Kafka`, an AWS Lambda MSK/self-managed Kafka event source trigger, and an
Azure Functions Kafka trigger. They share the same `[Message]`/message-handler programming model,
but are three independent implementations, not one library reused three times — the AWS and Azure
packages do **not** depend on `Benzene.Kafka.Core`, and each maps a Kafka record onto the pipeline
in its own way. Pick the section below that matches how you're hosting the service.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Kafka broker to develop against. The examples below use the
  [`examples/Kafka/docker-compose.yaml`](../examples/Kafka/docker-compose.yaml) file, which brings
  up a single-broker Confluent Kafka cluster (`localhost:9092`) plus
  [Kafdrop](https://github.com/obsidiandynamics/kafdrop) at `http://localhost:19000` for inspecting
  topics
- For the AWS or Azure sections: the same cloud prerequisites as
  [AWS Lambda Setup](getting-started-aws) / [Azure Functions Setup](azure-functions)

## Which package do I need?

| Scenario | Package | Underlying client |
|---|---|---|
| Long-running worker/console app consuming Kafka directly | `Benzene.Kafka.Core` | `Confluent.Kafka` |
| AWS Lambda triggered by MSK or self-managed Kafka | `Benzene.Aws.Lambda.Kafka` | `Amazon.Lambda.KafkaEvents` |
| Azure Function triggered by Kafka (incl. Event Hubs' Kafka-compatible endpoint) | `Benzene.Azure.Function.Kafka` | `Microsoft.Azure.Functions.Worker.Extensions.Kafka` |

---

## 1. Self-hosted Kafka worker (`Benzene.Kafka.Core`)

This is the option for a long-running process that owns its own consumer group and polls Kafka
continuously — a Worker Service, a container, or a plain console app. Under the hood,
`BenzeneKafkaWorker<TKey, TValue>` runs a `while` loop calling `IConsumer.Consume`, dispatching each
record through the middleware pipeline with up to `ConcurrentRequests` records in flight at once
(bounded by a `SemaphoreSlim`).

### 1.1 Create the project

```bash
mkdir MyKafkaWorker && cd MyKafkaWorker
dotnet new worker -f net10.0
```

### 1.2 Install the NuGet packages

```bash
dotnet add package Benzene.Kafka.Core --prerelease
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.SelfHost --prerelease
```

`Benzene.Kafka.Core` brings in `Benzene.HostedService` and `Benzene.Core` transitively (see its
`.csproj`), but add them explicitly since you'll reference their types directly below.
`Confluent.Kafka` comes in transitively from `Benzene.Kafka.Core`.

### 1.3 Define a message handler

The Kafka consumer routes each record to a handler by matching the record's **literal Kafka topic
name** against the handler's `[Message("...")]` value — there's no colon-separated topic-id
convention here the way there is for HTTP/SQS/SNS; whatever you pass in `[Message(...)]` must be
exactly the Kafka topic string:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

[Message("hello_world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage>
{
    public Task HandleAsync(HelloWorldMessage message)
    {
        Console.WriteLine($"Hello {message.Name}!");
        return Task.CompletedTask;
    }
}

public class HelloWorldMessage
{
    public string Name { get; set; }
}
```

This is an `IMessageHandler<TRequest>` (no response type) — the right shape for a fire-and-forget
Kafka record, since nothing is written back to the broker. See
[Message Handlers](message-handlers) for the request/response shape if you need one for another
transport on the same handler.

### 1.4 Define your StartUp

Kafka consumption is wired up through `Benzene.SelfHost`'s **worker-specific** startup shape —
`BenzeneWorkerStartup` (or its `IHostedService`-ready subclass, `BenzeneHostedServiceStartup`, from
`Benzene.HostedService`) — not the platform-neutral `BenzeneStartUp` used by AWS/Azure/ASP.NET
Core. This matters because `Benzene.Kafka.Core.Extensions.UseKafka<TKey, TValue>` only extends
`IBenzeneWorkerStartup`, the interface `Configure` receives on this base class:

```csharp
using Benzene.Core.MessageHandlers.DI;
using Benzene.HostedService;
using Benzene.Kafka.Core;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneHostedServiceStartup
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
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddKafka<Ignore, string>());
    }

    public override void Configure(IBenzeneWorkerStartup app, IConfiguration configuration)
    {
        var kafkaConfig = new BenzeneKafkaConfig
        {
            ConsumerConfig = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                SecurityProtocol = SecurityProtocol.Plaintext,
                GroupId = "my-kafka-worker",
                AutoOffsetReset = AutoOffsetReset.Earliest
            },
            Topics = new[] { "hello_world" },
            ConcurrentRequests = 5
        };

        app.UseKafka<Ignore, string>(kafkaConfig, kafka => kafka.UseMessageHandlers());
    }
}
```

- `TKey`/`TValue` are the Confluent.Kafka deserialized record types — `Ignore, string` (as in
  `examples/Kafka`) is the common case where you don't care about the message key and the value is
  a JSON string; use `Confluent.Kafka`'s other built-in deserializers (or your own) for other
  shapes.
- `AddKafka<TKey, TValue>()` (called by `ConfigureServices`) registers the topic/body/headers
  getters that adapt a `ConsumeResult<TKey, TValue>` to the message-handler pipeline. Kafka record
  headers **are** mapped to Benzene message headers on this path (UTF-8 decoded) — unlike the Azure
  Functions Kafka trigger below.
- `BenzeneKafkaConfig.ConcurrentRequests` (default `5`) bounds how many records this worker
  processes concurrently via an internal semaphore.

### 1.5 Wire up `Program.cs`

`BenzeneHostedServiceStartup` already *is* an `IHostedService` (its constructor builds the whole
pipeline), so registering it is a one-liner:

```csharp
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddHostedService<StartUp>())
    .Build();

await host.RunAsync();
```

Run it locally against the compose stack:

```bash
docker compose -f docker-compose.yaml up -d   # from examples/Kafka
dotnet run
```

### 1.6 Producing messages

To send Kafka messages from another Benzene service (rather than a plain `Confluent.Kafka`
producer), build a `KafkaBenzeneMessageClient` from a middleware pipeline over
`KafkaSendMessageContext`:

```csharp
using Benzene.Core.Middleware;
using Benzene.Kafka.Core.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Logging.Abstractions;

var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
var producer = new ProducerBuilder<string, string>(producerConfig).Build();

var pipeline = new MiddlewarePipelineBuilder<KafkaSendMessageContext>(benzeneServiceContainer)
    .UseKafkaClient(producer)
    .Build();

var client = new KafkaBenzeneMessageClient(pipeline, NullLogger<KafkaBenzeneMessageClient>.Instance, serviceResolver);

await client.SendMessageAsync<object, Benzene.Abstractions.Results.Void>("hello_world", new { Name = "World" });
```

`KafkaContextConverter` JSON-serializes the payload as the Kafka message value and forwards
`IBenzeneClientRequest.Headers` onto the outbound `Message.Headers` (UTF-8 encoded) — the same
mechanism correlation-ID/trace-context decorators rely on to reach the wire. A plain
`IProducer<string, string>.ProduceAsync(...)` call works exactly as well if you don't need that;
see `examples/Kafka/Benzene.Examples.Kakfa.Producer` for a minimal producer console app built this
way.

### 1.7 Testing

There's no `BenzeneTestHost` support for Kafka — `Benzene.Testing` has no `Send*Async` extension
for it (unlike SQS/SNS/API Gateway on AWS). The pattern used in
`examples/Kafka/Benzene.Examples.Kafka.Test` instead runs the worker for real against a live broker
and polls for the effect:

1. `KafkaSetUp` deletes/recreates the test's Kafka topics against the compose broker and builds a
   real `Confluent.Kafka` producer.
2. `WorkerSetUp` starts a real `StartUp` instance (`await _worker.StartAsync(...)`) on a background
   thread.
3. The test publishes a message with `KafkaSetUp.SendAsync(topic, message)`, then polls
   (`ResultPoller.Poll(delayMs, times, () => <condition>, "failure message")`) until the handler's
   observable side effect (e.g. a row in an in-memory store) appears, since consumption is
   asynchronous.
4. `WorkerSetUp.TearDownAsync()`/`KafkaSetUp.TearDownAsync()` stop the worker and delete the topics
   afterward.

This is a real integration test against Docker Kafka, not an in-memory fake — budget for the
broker's startup time in CI.

---

## 2. AWS Lambda (MSK / self-managed Kafka)

`Benzene.Aws.Lambda.Kafka` handles the Lambda event source mapping for MSK or self-managed Kafka
clusters. It's a thin adapter over `Amazon.Lambda.KafkaEvents`'s `KafkaEvent` and does **not** use
`Confluent.Kafka` or `Benzene.Kafka.Core` at all — records already arrive deserialized as part of
the Lambda invocation payload.

Add it to an existing [AWS Lambda](getting-started-aws) `StartUp`:

```bash
dotnet add package Benzene.Aws.Lambda.Kafka --prerelease
```

```csharp
public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    => app.UseAwsLambda(eventPipeline => eventPipeline
        .UseKafka(kafkaApp => kafkaApp
            .UseCorrelationId()
            .UseMessageHandlers(router => router.UseFluentValidation())));
```

As with the worker path, the handler's `[Message("...")]` value must match the Kafka topic name
literally (`KafkaMessageTopicGetter` reads it straight off the Lambda event record). Kafka record
headers are mapped to Benzene headers, and partition/offset are available on `KafkaContext`. Kafka
events are fire-and-forget — no response is written back, and a Lambda invocation carrying multiple
records processes all of them (flattened across topic-partitions) through the pipeline before
returning.

See [AWS Lambda Setup](getting-started-aws#kafka) for how this fits alongside other AWS event
sources in the same function, and
[AWS IAM Permissions Reference](aws-iam-permissions#kafka-trigger-benzeneawslambdakafka) for the
MSK-specific execution-role permissions — these are more involved than other AWS event sources
because MSK event source mappings require VPC connectivity.

## 3. Azure Functions Kafka trigger

`Benzene.Azure.Function.Kafka` wraps the isolated-worker `KafkaTrigger` binding from
`Microsoft.Azure.Functions.Worker.Extensions.Kafka`. It works against any Kafka-compatible
endpoint the trigger binding supports, including Azure Event Hubs' Kafka-compatible endpoint —
despite its package description mentioning Event Hubs specifically, the source has no Event
Hubs-specific code; it's a generic `KafkaRecord`/`KafkaRecord[]` adapter.

Add it to an existing [Azure Functions](azure-functions) `StartUp`:

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Kafka
dotnet add package Benzene.Azure.Function.Kafka --prerelease
```

```csharp
public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    => app.UseKafka(kafka => kafka.UseMessageHandlers());
```

Then add a Kafka trigger function that injects `IAzureFunctionApp` and dispatches to it — unlike
HTTP, there's no single catch-all trigger Benzene can generate for you, since the trigger binding
needs its own attribute-declared topic/broker list per function. `Benzene.Azure.Function.Kafka`'s
own `KafkaApplication`/`HandleKafkaEvents` take a `KafkaRecord[]` (the type from
`Microsoft.Azure.Functions.Worker.Extensions.Kafka` itself — verified from this repo's source); the
exact shape of your `[KafkaTrigger]`-bound parameter depends on which overload of that Microsoft
binding you use, so check its docs/samples for the trigger attribute signature that gives you
`KafkaRecord[]` (or something you can map to it) directly:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Kafka;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kafka;

public class KafkaFunction
{
    private readonly IAzureFunctionApp _app;

    public KafkaFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("kafka")]
    public async Task Run([KafkaTrigger("BrokerList", "hello_world")] KafkaRecord[] events)
    {
        await _app.HandleKafkaEvents(events);
    }
}
```

As with the worker and AWS paths, `[Message("...")]` must match the Kafka topic name literally
(`KafkaMessageTopicGetter.GetTopic` reads `KafkaEvent.Topic` directly). **Kafka record headers are
not mapped on this path** — `KafkaMessageHeadersGetter` always returns an empty dictionary today,
regardless of what the broker sent — so header-based middleware (correlation IDs, W3C trace
context) won't see anything on Azure's Kafka trigger even though it works on the worker and AWS
paths above.

See [Azure Functions Setup](azure-functions#kafka) for how this fits alongside HTTP and Event Hubs
triggers in the same Function App.

## Troubleshooting

- **Handler never fires** — the `[Message("...")]` value must equal the literal Kafka topic name on
  all three paths (not a colon-separated topic id). Double-check `BenzeneKafkaConfig.Topics` (worker
  path) actually includes that topic, and that the broker has it (`docker compose ... kafdrop` at
  `http://localhost:19000` lets you inspect topics/messages directly).
- **`GroupId` collisions in tests** — the worker example generates a random `GroupId` per test run
  (`Guid.NewGuid().ToString()`) to avoid two test runs fighting over the same consumer group's
  offsets; reuse a stable `GroupId` only once you want durable, resumable consumption.
- **Missing headers on Azure** — see the Azure Functions note above; this is a current gap in
  `Benzene.Azure.Function.Kafka`, not a broker or configuration issue.
- **MSK connectivity failures on AWS** — almost always a VPC/security-group issue, not a Benzene
  issue; see [AWS IAM Permissions Reference](aws-iam-permissions#kafka-trigger-benzeneawslambdakafka).

## See Also

- [Unified Hosting Model](hosting)
- [Testing Benzene](testing-benzene)
- [AWS Lambda Setup](getting-started-aws)
- [Azure Functions Setup](azure-functions)
- [Message Handlers](message-handlers)
- [AWS IAM Permissions Reference](aws-iam-permissions)
