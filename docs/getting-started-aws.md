# Getting Started: Benzene on AWS Lambda

Benzene runs efficiently in AWS Lambda, supporting multiple event sources (API Gateway, SQS,
SNS, Kafka, S3) through a single middleware pipeline. This guide starts from an empty folder
and ends with a deployed Lambda function handling API Gateway, SQS, and SNS events.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An AWS account, with the [AWS CLI](https://aws.amazon.com/cli/) and
  [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html)
  configured, if you want to deploy

## 1. Create the project

```bash
mkdir MyFunction && cd MyFunction
dotnet new classlib -f net10.0
```

## 2. Install the NuGet packages

Benzene's packages are published as prerelease (`-alpha`) versions, so `--prerelease` is
required until 1.0:

```bash
dotnet add package Benzene.Aws.Lambda.Core --prerelease
dotnet add package Benzene.Aws.Lambda.ApiGateway --prerelease
```

`Benzene.Aws.Lambda.Core` brings in the middleware pipeline, message handler infrastructure,
and `BenzeneStartUp` base class transitively (via `Benzene.Microsoft.Dependencies`).
`Benzene.Aws.Lambda.ApiGateway` adds the `UseApiGateway` middleware for handling HTTP requests
via API Gateway. Add `Benzene.Aws.Lambda.Sqs`, `Benzene.Aws.Lambda.Sns`,
`Benzene.Aws.Lambda.Kafka`, or `Benzene.Aws.Lambda.S3` the same way if your function also needs
to handle those event sources (see [Supported Event Sources](#supported-event-sources) below).

You'll also need the concrete `Microsoft.Extensions.Configuration` implementation for
`GetConfiguration()` below (only its abstractions are referenced transitively):

```bash
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add package Microsoft.Extensions.Configuration.FileExtensions
```

## 3. Define a message handler

Business logic lives in message handlers, not in the Lambda entry point — this keeps it
testable and portable across hosts. See [Message Handlers](message-handlers.md) for the full
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
same topic — the same handler answers both a direct `{"topic": "hello:world", ...}` message
(e.g. from SQS/SNS) and an API Gateway `GET /hello/{name}` request. Both attributes are
discovered by reflection, so there is nothing further to register per-handler.

## 4. Define your StartUp

`BenzeneStartUp` (from `Benzene.Microsoft.Dependencies`, referenced transitively) is the
platform-neutral application definition shared by every Benzene host — the same class shape
you'd write for Azure Functions or the .NET generic host. Configure the AWS-specific event
pipeline via `UseAwsLambda(...)`, which hands you an
`IMiddlewarePipelineBuilder<AwsEventStreamContext>` to wire up event sources on:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
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
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseApiGateway(apiGatewayApp => apiGatewayApp
                .UseMessageHandlers()));
    }
}
```

> This is the platform-neutral pattern used by every Benzene host — the
> [`examples/Aws`](../examples/Aws) project follows exactly this shape. Only the AWS-specific event
> wiring lives inside `UseAwsLambda(...)`; the same `StartUp` runs unchanged on other Benzene hosts
> (see [Azure Functions Setup](azure-functions.md)).

## 5. Wire up the Lambda entry point

Subclass `AwsLambdaHost<TStartUp>` — it builds the pipeline once on cold start and implements
the Lambda entry point for you, so there's no separate handler method to write:

```csharp
using Benzene.Aws.Lambda.Core;

public class Function : AwsLambdaHost<StartUp>
{
}
```

Point your Lambda's `function-handler` at it:

```
MyFunction::MyFunction.Function::FunctionHandlerAsync
```

(replace `MyFunction` with your assembly and namespace, and `Function` if you named the class
something else)

## 6. Test locally with BenzeneTestHost

Before deploying, exercise the pipeline in-memory using the same `StartUp` class you'll deploy.
Add the test-helper packages to your test project:

```bash
dotnet add package Benzene.Testing --prerelease
dotnet add package Benzene.Tools --prerelease
dotnet add package Benzene.Aws.Lambda.ApiGateway.TestHelpers --prerelease
```

`BenzeneTestHost.Create<TStartUp>().BuildAwsLambdaHost()` runs your real `GetConfiguration()`/
`ConfigureServices()`/`Configure()` and returns the same `IAwsLambdaEntryPoint` that
`AwsLambdaHost<TStartUp>` builds for a real deployment. Wrap it in `AwsLambdaBenzeneTestHost`
(from `Benzene.Tools`) to send events into it and get typed responses back:

```csharp
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Testing;
using Benzene.Tools.Aws;

var host = new AwsLambdaBenzeneTestHost(
    BenzeneTestHost.Create<StartUp>()
        .WithServices(services => services.AddScoped(_ => mockSomeDependency.Object))
        .BuildAwsLambdaHost());

var request = HttpBuilder.Create("GET", "/hello/world");
var response = await host.SendApiGatewayAsync(request);
```

`WithServices(...)` runs immediately after `ConfigureServices`, so it's the standard way to
swap in a mock or fake for a test; `WithConfiguration(...)` does the same for configuration
values. This works for any transport your `StartUp` wires up — `SendSqsAsync`/`SendSnsAsync`
come from the matching `Benzene.Aws.Lambda.Sqs.TestHelpers`/`Benzene.Aws.Lambda.Sns.TestHelpers`
packages, and a topic-routed `BenzeneMessage` (no specific transport) can be sent directly via
`SendBenzeneMessageAsync` from `Benzene.Core.MessageHandlers.TestHelpers`, if your `Configure`
wires up `UseBenzeneMessage(...)`. See [Testing Benzene](testing-benzene.md) for the full pattern,
including configuration/service overrides.

## 7. Deploy with SAM

Add a minimal `template.yaml` alongside your `.csproj`:

```yaml
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31

Globals:
  Function:
    Timeout: 30
    MemorySize: 1024
    # .NET 10 has no AWS-managed Lambda runtime yet - dotnet8 is the current managed
    # runtime and works fine for a net10.0 project, since it targets a compatible Lambda ABI.
    Runtime: dotnet8
    Architectures:
      - arm64

Resources:
  MyFunction:
    Type: AWS::Serverless::Function
    Metadata:
      BuildMethod: dotnet8
    Properties:
      FunctionName: my-function
      Handler: MyFunction::MyFunction.Function::FunctionHandlerAsync
      CodeUri: ./
      Events:
        HttpApi:
          Type: HttpApi
```

Then build and deploy:

```bash
sam build
sam deploy --guided
```

`sam deploy --guided` walks you through stack name, region, and parameter values on first run,
then remembers them in `samconfig.toml` for subsequent deploys. Once deployed, SAM prints the
API Gateway URL — `GET` it at `/hello/world` to confirm the handler above responds.

See [`examples/Aws/Benzene.Examples.Aws/template.yaml`](../examples/Aws/Benzene.Examples.Aws/template.yaml)
for a fuller example covering SQS, SNS, and an optional MSK/Kafka event source.

## Supported Event Sources

Benzene provides specialized middleware for various AWS event sources, each configured inside
the same `Configure` method, on the same `eventPipeline` (an
`IMiddlewarePipelineBuilder<AwsEventStreamContext>`) shown in step 4 — a single Lambda function
can handle several event sources at once, each routed to its own sub-pipeline based on the
shape of the incoming payload:

- **API Gateway**: `eventPipeline.UseApiGateway(...)`, in `Benzene.Aws.Lambda.ApiGateway`
  (REST API and HTTP API events, CORS, custom authorizers)
- **SQS**: `eventPipeline.UseSqs(...)`, in `Benzene.Aws.Lambda.Sqs` (batch processing,
  partial-batch-failure reporting)
- **SNS**: `eventPipeline.UseSns(...)`, in `Benzene.Aws.Lambda.Sns` (fan-out notifications,
  fire-and-forget)
- **EventBridge**: `eventPipeline.UseEventBridge(...)`, in `Benzene.Aws.Lambda.EventBridge`
  (event-bus rules, scheduled rules, AWS service events; fire-and-forget)
- **DynamoDB Streams**: `eventPipeline.UseDynamoDb(...)`, in `Benzene.Aws.Lambda.DynamoDb`
  (change-data-capture: table INSERT/MODIFY/REMOVE events, ordered, partial-batch-failure
  checkpointing)
- **Kafka**: `eventPipeline.UseKafka(...)`, in `Benzene.Aws.Lambda.Kafka` (MSK and
  self-managed Kafka)
- **S3**: `eventPipeline.UseS3(...)`, in `Benzene.Aws.Lambda.S3` (object-created/removed
  event notifications, fire-and-forget)
- **Kinesis**: `eventPipeline.UseKinesisStream(...)`, in `Benzene.Aws.Lambda.Kinesis` (Data
  Streams; the whole batch is processed as one ordered *stream*, not fanned out per record)

### SQS

```csharp
eventPipeline.UseSqs(sqsApp => sqsApp
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

SQS messages are processed in batches; the message body is deserialized to your request type
and message attributes are mapped to headers. `SqsApplication` supports reporting partial-batch
failures back to SQS, so only the records that actually failed are retried/redriven to a
dead-letter queue rather than the whole batch. See
[AWS IAM Permissions](aws-iam-permissions.md) for the execution-role permissions the SQS event
source mapping needs (`sqs:ReceiveMessage`, `sqs:DeleteMessage`, `sqs:GetQueueAttributes`).

The topic is normally read from a `topic` message attribute, set by a Benzene client. If a
queue's producer isn't a Benzene client and never sets one (a raw SQS send, a queue fed by
another system), call `.UsePresetTopic("orders.created")` before `.UseMessageHandlers()` in that
queue's pipeline to route every message on it to a fixed topic instead — see
[Common Middleware: UsePresetTopic](common-middleware.md#usepresettopic).

### SNS

```csharp
eventPipeline.UseSns(snsApp => snsApp
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

SNS invokes your function via a resource-based Lambda permission — no extra
execution-role IAM is needed to receive notifications (see
[AWS IAM Permissions](aws-iam-permissions.md)). The topic is resolved from a `topic` message
attribute (or the SNS topic ARN, depending on delivery configuration) and routed to the
matching message handler, same as every other transport. There is no response to write back —
SNS delivery is fire-and-forget.

**Unsafe by default:** a handler that returns a failure result (rather than throwing) is silently
accepted — the invocation reports success and SNS never retries it. Set `SnsOptions.RaiseOnFailureStatus
= true` if you want failure results retried too (requires an idempotent handler, since SNS
redelivery has no dedup) — see [SNS Fan-Out Pattern](cookbooks/sns-fan-out.md#configuring-exception-and-retry-behavior-with-snsoptions).

### EventBridge

```csharp
eventPipeline.UseEventBridge(eventBridgeApp => eventBridgeApp
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

The event's `detail-type` is the message topic — EventBridge's native routing key, so
`[Message("order.created")]` handles events published with that detail-type — and `detail` is
the message body. Envelope metadata (`source`, `id`, `account`, `region`, `time`) is exposed as
`eventbridge-`-prefixed headers, and Benzene wire headers embedded by the outbound EventBridge
client (see [Clients](clients.md)) are lifted from the reserved `_benzeneHeaders` key inside
`detail`. Like SNS, EventBridge invokes the function via a resource-based Lambda permission and
delivery is fire-and-forget. The `aws_cloudwatch_event_rule`/target/permission wiring can be
generated from your `[Message]` topics — see [Terraform Code Generation](terraform.md).

### DynamoDB Streams

```csharp
eventPipeline.UseDynamoDb(dynamoDb => dynamoDb
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

DynamoDB Streams deliver ordered change-data-capture records when items in a table are inserted,
modified, or removed. The topic is `"{tableName}:{eventName}"` — a handler declares
`[Message("orders:INSERT")]` — and the body is the record's image unmarshalled from DynamoDB's
AttributeValue format into plain JSON, so your handler deserializes an ordinary POCO
(`NewImage` when present, falling back to `OldImage` for REMOVE events, then `Keys` for
`KEYS_ONLY` stream views). Envelope metadata is exposed as `dynamodb-`-prefixed headers
(`dynamodb-event-name`, `dynamodb-table`, `dynamodb-sequence-number`, ...).

Because stream records within a shard are ordered, the batch is processed **sequentially and
stops at the first failure**: the failed record's sequence number is returned as a partial batch
failure, and (with `ReportBatchItemFailures` enabled on the event source mapping) Lambda
checkpoints there and redelivers from that record. This is deliberately different from the SQS
adapter's concurrent batch processing.

### Kafka

```csharp
eventPipeline.UseKafka(kafkaApp => kafkaApp
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

Works for both MSK and self-managed Kafka. Kafka record headers are mapped to Benzene
message headers, and partition/offset are available on `KafkaContext`. See
[AWS IAM Permissions](aws-iam-permissions.md) for the MSK-specific permissions your
execution role needs — these are more involved than the other event sources since MSK
event source mappings require VPC connectivity.

### S3

```csharp
eventPipeline.UseS3(s3App => s3App
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

Like SNS, S3 invokes via a resource-based permission plus a bucket notification
configuration — no extra execution-role IAM needed to receive. S3 event notifications
are fire-and-forget: no response is written back, since S3 doesn't expect one.

### Kinesis (streaming)

Kinesis is different from the transports above: it's a **streaming** source, so the whole batch
is handed to your pipeline as one ordered stream rather than fanned out into a handler per record.
This preserves per-shard ordering and lets you window and aggregate — the things a fan-out model
throws away.

```csharp
eventPipeline.UseKinesisStream(kinesis => kinesis
    .UseStream<KinesisEventRecord>(async (records, ct) =>
    {
        // records within a partition key are in shard order
        await foreach (var partition in records.PartitionBy(r => r.Kinesis.PartitionKey, ct))
        {
            foreach (var record in partition.Value)
            {
                var payload = record.Kinesis.GetDataAsString();
                // ... process
            }
        }
    }));
```

The handler receives an `IAsyncEnumerable<KinesisEventRecord>` — pull records lazily (backpressure),
decode each with `record.Kinesis.GetData()` / `GetDataAsString()`, and use the stream operators
`PartitionBy(...)` (restore per-shard order the poller batched together) and `Window(n)` (fixed-size
batches). Processing is fire-and-forget; the Lambda poller checkpoints the shard on success. This is
the AWS counterpart to the Azure Event Hubs streaming binding. See
[AWS IAM Permissions](aws-iam-permissions.md) for the stream-consumer permissions the event source
mapping needs (`kinesis:GetRecords`, `kinesis:GetShardIterator`, `kinesis:DescribeStream`,
`kinesis:ListShards`).

## IAM Permissions

Each event source above has different IAM requirements — some need explicit
execution-role permissions (SQS, Kafka), others invoke your function via a
resource-based permission and need none. See
[AWS IAM Permissions Reference](aws-iam-permissions.md) for a minimal policy per
package, with the specific SDK call in Benzene's source that drives each requirement.

## Configuration

`GetConfiguration()` runs once on cold start, before any services are registered, and its
result is passed into both `ConfigureServices` and `Configure`. Anything built on top of
`Microsoft.Extensions.Configuration` works here — the example above reads environment
variables (the natural fit for Lambda, where you set configuration via the function's
environment variables in the console, SAM template, or CDK/Terraform), but `AddJsonFile(...)`,
AWS Systems Manager Parameter Store providers, or AWS Secrets Manager providers all work the
same way.

## Health Checks

Add a health check topic so you (or a monitoring system) can confirm the function's
dependencies are reachable, without a real request hitting your business handlers:

```csharp
var healthChecks = new IHealthCheck[] { new SimpleHealthCheck() };

eventPipeline.UseApiGateway(apiGatewayApp => apiGatewayApp
    .UseHealthCheck("healthcheck", "POST", "/healthcheck", healthChecks)
    .UseMessageHandlers());
```

See [Health Checks](health-checks.md) for writing your own `IHealthCheck` (e.g. one that checks
database connectivity) and the full set of `UseHealthCheck` overloads.

## Observability

- **Invocation identity**: add `.UseBenzeneInvocation()` on the outer `eventPipeline`, before
  splitting into transports, to expose an `IBenzeneInvocation` for the duration of the
  invocation — `InvocationId` is set to the AWS Lambda request ID, and
  `GetFeature<ILambdaContext>()` returns the native Lambda execution context if you need it
  (e.g. for `RemainingTime`):

  ```csharp
  app.UseAwsLambda(eventPipeline => eventPipeline
      .UseBenzeneInvocation()
      .UseApiGateway(apiGatewayApp => apiGatewayApp
          .UseMessageHandlers()));
  ```

  This flows into a single-request pipeline like API Gateway, but **not** into SQS/SNS/Kafka's
  per-message batch dispatch, since each message in a batch gets its own nested DI scope today.
  See `Benzene.Aws.Lambda.Core.BenzeneInvocationExtensions`.
- **Tracing**: `services.UsingBenzene(x => x.AddDiagnostics())` wraps every middleware in a
  `System.Diagnostics.Activity` span automatically — no per-middleware opt-in needed. Add
  `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` to export those spans (and
  `UseBenzeneMetrics()`'s counters) to a real backend via OpenTelemetry.
- **Cross-service correlation**: `eventPipeline.UseApiGateway(a => a.UseW3CTraceContext()...)`
  (W3C `traceparent` propagation) — see [Correlation IDs](correlation-ids.md).
- **Log enrichment**: `UseBenzeneEnrichment()` attaches `invocationId`/`traceId`/`spanId`/
  `topic`/`transport`/`handler` to the logging scope in one call, portable across every
  Benzene host.

See [Monitoring & Diagnostics](monitoring.md) for the full picture, including logging providers
and named timers.

## Bare Metal Entry Point

If you prefer more control than `BenzeneStartUp`/`AwsLambdaHost` gives you, you can build the
pipeline and entry point by hand:

```csharp
public class BareMetalLambdaEntryPoint
{
    private readonly IMiddlewarePipeline<AwsEventStreamContext> _pipeline;
    private readonly MicrosoftServiceResolverFactory _microsoftServiceResolverFactory;

    public BareMetalLambdaEntryPoint()
    {
        var services = new ServiceCollection();

        var middlewarePipelineBuilder = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services))
            .UseBenzeneMessage(x => x
                .UseMessageHandlers(s => s.UseFluentValidation()));

        services.UsingBenzene(x => x.AddMessageHandlers(Assembly.GetExecutingAssembly()));

        _microsoftServiceResolverFactory = new MicrosoftServiceResolverFactory(services.BuildServiceProvider());
        _pipeline = middlewarePipelineBuilder.Build();
    }

    public async Task<Stream> FunctionHandler(Stream input, ILambdaContext lambdaContext)
    {
        using var serviceResolver = _microsoftServiceResolverFactory.CreateScope();

        var context = new AwsEventStreamContext(input, lambdaContext);
        await _pipeline.HandleAsync(context, serviceResolver);
        return context.Response;
    }
}
```

## Troubleshooting

**"IBenzeneInvocation was requested before the pipeline's UseBenzeneInvocation() middleware
populated it for this invocation."** — you injected `IBenzeneInvocation` somewhere, but
`.UseBenzeneInvocation()` was never called on a pipeline upstream of it. Add it on the outer
`eventPipeline`, before `UseApiGateway(...)`/`UseSqs(...)`/etc. Note it doesn't reach into
SQS/SNS/Kafka's per-message batch dispatch (each message gets its own nested DI scope), so
don't expect it to resolve inside those handlers even after adding it upstream.

**Handler never gets called / 404 from API Gateway** — check that `[HttpEndpoint("METHOD",
"/path")]` matches exactly, including case and any route parameters (`{name}`), and that the
handler's assembly was passed to `AddMessageHandlers(...)` (or that you called the
no-argument `AddMessageHandlers()` overload, which scans the calling assembly).

**SQS/SNS message never routes to a handler** — both transports resolve the topic from a
message attribute (`topic` for SNS by convention, or an explicit attribute set by the
producer), not the message body. Confirm the producer sets that attribute, and that a handler
exists with a matching `[Message("...")]` topic.

**Deployment fails or the function returns nothing** — double check the `function-handler`
string matches `Assembly::Namespace.ClassName::FunctionHandlerAsync` exactly (including the
namespace), and that your `Function : AwsLambdaHost<StartUp>` class has a public parameterless
constructor (inherited automatically unless you add one of your own).

**Cold starts are slow** — `AwsLambdaHost<TStartUp>`'s constructor runs `GetConfiguration()`,
`ConfigureServices()`, and `Configure()` exactly once per execution environment, so cold-start
cost is dominated by whatever your `ConfigureServices` does (e.g. opening a DB connection
eagerly). Prefer lazy/on-demand initialization inside your services over eager work in
`ConfigureServices`.

**Local test host behaves differently from the deployed function** — `BuildAwsLambdaHost()`
performs the exact same construction `AwsLambdaHost<TStartUp>` does for a real deployment
(same `GetConfiguration`/`ConfigureServices`/`Configure` calls), so a divergence usually means
a `WithServices`/`WithConfiguration` override in the test masked something — remove the
override temporarily to confirm.

## See Also

- [Correlation IDs](correlation-ids.md) — the legacy `correlationId`-header approach, and why
  W3C trace context supersedes it for cross-service correlation
- [Monitoring & Diagnostics](monitoring.md) — tracing, logging, and OpenTelemetry export
- [Health Checks](health-checks.md) — writing custom `IHealthCheck`s and wiring `UseHealthCheck`
- [AWS IAM Permissions Reference](aws-iam-permissions.md) — minimum IAM policy per AWS package
- [Testing Benzene](testing-benzene.md) — the full `BenzeneTestHost` pattern, including
  configuration/service overrides and Azure/ASP.NET Core equivalents
- [`examples/Aws`](../examples/Aws) — a complete, runnable project covering API Gateway, SQS,
  SNS, Kafka, health checks, and validation
