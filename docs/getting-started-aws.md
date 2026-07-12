# AWS Lambda Setup

Benzene is designed to run efficiently in AWS Lambda, supporting multiple event sources with a unified programming model.

## Core Concepts

In Benzene, an AWS Lambda function is structured using a `StartUp` class that inherits from `AwsLambdaStartUp`. This class defines how services are configured and how the middleware pipeline is built. `AwsLambdaStartUp` itself is the Lambda entry point — there is no separate entry point class to write.

## Basic Setup

### 1. Create a StartUp class

```csharp
public class StartUp : AwsLambdaStartUp
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
        services.UsingBenzene(x => x.AddMessageHandlers(typeof(MyHandler).Assembly));
        // Register other dependencies
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app.UseTimer("aws-stream-application");

        var benzeneMessagePipeline = app.Create<BenzeneMessageContext>()
            .UseCorrelationId()
            .UseMessageHandlers(router => router.UseFluentValidation());

        app.UseBenzeneMessage(benzeneMessagePipeline);

        // Reuse the same pipeline behind API Gateway, SNS, SQS, Kafka, etc.
        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            .UseHttpToBenzeneMessage(benzeneMessagePipeline)
        );

        app.UseSqs(sqsApp => sqsApp
            .UseCorrelationId()
            .UseMessageHandlers(router => router.UseFluentValidation())
        );
    }
}
```

### 2. Configure the Lambda handler

Because `StartUp` implements `FunctionHandlerAsync(Stream, ILambdaContext)`, it is the class AWS Lambda invokes directly — point your `function-handler` at it (in `aws-lambda-tools-defaults.json`, a `serverless.template`, or your CDK/Terraform config):

```
YourAssembly::YourNamespace.StartUp::FunctionHandlerAsync
```

## Supported Event Sources

Benzene provides specialized middleware for various AWS event sources:

- **API Gateway**: `app.UseApiGateway(...)`
- **SQS**: `app.UseSqs(...)`
- **SNS**: `app.UseSns(...)`
- **Kafka**: `app.UseKafka(...)`
- **S3**: `app.UseS3(...)`, in `Benzene.Aws.Lambda.S3`

Each of these allows you to define a specific sub-pipeline for that event source, while still being able to share common logic and message handlers via `UseHttpToBenzeneMessage`/`UseBenzeneMessage`. All of them are added inside the same `Configure` method, on the same `app` (an `IMiddlewarePipelineBuilder<AwsEventStreamContext>`) shown in Basic Setup above — a single Lambda function can handle several event sources at once, each routed to its own sub-pipeline based on the shape of the incoming payload.

### SNS

```csharp
app.UseSns(snsApp => snsApp
    .UseCorrelationId()
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

SNS invokes your function via a resource-based Lambda permission — no extra
execution-role IAM is needed to receive notifications (see
[AWS IAM Permissions](aws-iam-permissions.md)). The SNS message's `topic` message
attribute (or the raw topic ARN, depending on delivery configuration) is used to route
to the matching message handler, same as every other transport.

### S3

```csharp
app.UseS3(s3App => s3App
    .UseCorrelationId()
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

Like SNS, S3 invokes via a resource-based permission plus a bucket notification
configuration — no extra execution-role IAM needed to receive. S3 event notifications
are fire-and-forget: no response is written back, since S3 doesn't expect one.

### Kafka

```csharp
app.UseKafka(kafkaApp => kafkaApp
    .UseCorrelationId()
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

Works for both MSK and self-managed Kafka. Kafka record headers are mapped to Benzene
message headers, and partition/offset are available on `KafkaContext`. See
[AWS IAM Permissions](aws-iam-permissions.md) for the MSK-specific permissions your
execution role needs — these are more involved than the other event sources since MSK
event source mappings require VPC connectivity.

## IAM Permissions

Each event source above has different IAM requirements — some need explicit
execution-role permissions (SQS, Kafka), others invoke your function via a
resource-based permission and need none. See
[AWS IAM Permissions Reference](aws-iam-permissions.md) for a minimal policy per
package, with the specific SDK call in Benzene's source that drives each requirement.

## Deploying with SAM

[`examples/Aws/Benzene.Examples.Aws/template.yaml`](../examples/Aws/Benzene.Examples.Aws/template.yaml)
is a working AWS SAM template for the example project, wiring up API Gateway, SQS, and
SNS (matching what `StartUp.cs` in that project actually configures), with an optional
Kafka/MSK event source mapping if you provide an existing cluster ARN. To deploy it:

```bash
cd examples/Aws/Benzene.Examples.Aws
sam build
sam deploy --guided
```

`sam deploy --guided` walks you through stack name, region, and parameter values on
first run, then remembers them in `samconfig.toml` for subsequent deploys.

## Bare Metal Entry Point

If you prefer more control than `AwsLambdaStartUp` gives you, you can build the pipeline and entry point by hand:

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

See [`examples/Aws`](../examples/Aws) for a complete, runnable project covering all of the above.
