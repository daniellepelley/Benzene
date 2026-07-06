# AWS Lambda Setup

Benzene is designed to run efficiently in AWS Lambda, supporting multiple event sources with a unified programming model.

## Core Concepts

In Benzene, an AWS Lambda function is typically structured using a `StartUp` class that inherits from `AwsLambdaStartUp`. This class defines how services are configured and how the middleware pipeline is built.

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
        services.AddBenzeneMessageHandlers(typeof(MyHandler).Assembly);
        // Register other dependencies
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app.UseSerilog()
           .UseCorrelationId();

        // Handle different AWS Event Sources
        app.UseApiGateway(apiGatewayApp => apiGatewayApp
            .UseMessageHandlers(router => router.UseFluentValidation())
        );

        app.UseSqs(sqsApp => sqsApp
            .UseMessageHandlers(router => router.UseFluentValidation())
        );
    }
}
```

### 2. Define the Entry Point

The entry point is the class that AWS Lambda invokes. You can use `BenzeneLambdaEntryPoint` which uses your `StartUp` class.

```csharp
public class LambdaEntryPoint : BenzeneLambdaEntryPoint<StartUp>
{
}
```

## Supported Event Sources

Benzene provides specialized middleware for various AWS event sources:

- **API Gateway**: `app.UseApiGateway(...)`
- **SQS**: `app.UseSqs(...)`
- **SNS**: `app.UseSns(...)`
- **Kafka**: `app.UseKafka(...)`
- **EventBridge**: `app.UseEventBridge(...)`

Each of these allows you to define a specific sub-pipeline for that event source, while still being able to share common logic and message handlers.

## Bare Metal Entry Point

If you prefer more control, you can implement a "bare metal" entry point:

```csharp
public class BareMetalLambdaEntryPoint
{
    private ServiceCollection _serviceCollection;
    private IMiddlewarePipeline<AwsEventStreamContext> _app;

    public BareMetalLambdaEntryPoint()
    {
        _app = new AwsEventStreamApplication()
            .UseBenzeneMessage(x => x
                .UseMessageHandlers(s => s.UseFluentValidation())
            );

        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddAwsMessageHandlers(Assembly.GetExecutingAssembly());
    }

    public async Task<Stream> FunctionHandler(Stream input, ILambdaContext lambdaContext)
    {
        var factory = new MicrosoftServiceResolverFactory(_serviceCollection.BuildServiceProvider());

        using (var serviceResolver = factory.CreateScope())
        {
            var context = new AwsEventStreamContext(input, lambdaContext);
            await _app.HandleAsync(context, serviceResolver);
            return context.Response;
        }
    }
}
```