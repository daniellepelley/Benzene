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
- **EventBridge**: EventBridge-specific middleware in `Benzene.Aws.Lambda.EventBridge`

Each of these allows you to define a specific sub-pipeline for that event source, while still being able to share common logic and message handlers via `UseHttpToBenzeneMessage`/`UseBenzeneMessage`.

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
