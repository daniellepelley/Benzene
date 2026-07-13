# AWS Lambda Setup

Benzene is designed to run efficiently in AWS Lambda, supporting multiple event sources with a
unified programming model. This guide starts from an empty folder and ends with a deployed
Lambda function handling API Gateway requests.

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
and `BenzeneStartUp` base class transitively. `Benzene.Aws.Lambda.ApiGateway` adds the
`UseApiGateway` middleware for handling HTTP requests via API Gateway. Add
`Benzene.Aws.Lambda.Sqs`, `Benzene.Aws.Lambda.Sns`, or `Benzene.Aws.Lambda.Kafka` the same way
if your function also needs to handle those event sources (see
[Supported Event Sources](#supported-event-sources) below).

You'll also need the concrete `Microsoft.Extensions.Configuration` implementation for
`GetConfiguration()` below (only its abstractions are referenced transitively):

```bash
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add package Microsoft.Extensions.Configuration.FileExtensions
```

## 3. Define a message handler

Business logic lives in message handlers, not in the Lambda entry point — this keeps it
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

`BenzeneStartUp` (from `Benzene.Microsoft.Dependencies`, referenced transitively) is the
platform-neutral application definition shared by every Benzene host — the same class shape
you'd write for Azure Functions, ASP.NET Core, or a console app. Configure the AWS-specific
event pipeline via `UseAwsLambda`:

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

## 6. Deploy with SAM

Add a minimal `template.yaml` alongside your `.csproj`:

```yaml
AWSTemplateFormatVersion: '2010-09-09'
Transform: AWS::Serverless-2016-10-31

Globals:
  Function:
    Timeout: 30
    MemorySize: 1024
    # .NET has no AWS-managed Lambda runtime that matches every TFM immediately -
    # dotnet8 is the current managed runtime and works fine for a net10.0 project,
    # since it targets a compatible Lambda ABI.
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

## Supported Event Sources

Benzene provides specialized middleware for various AWS event sources, each configured inside
the same `Configure` method, on the same `eventPipeline` (an
`IMiddlewarePipelineBuilder<AwsEventStreamContext>`) shown in step 4 — a single Lambda function
can handle several event sources at once, each routed to its own sub-pipeline based on the
shape of the incoming payload:

- **API Gateway**: `eventPipeline.UseApiGateway(...)`, in `Benzene.Aws.Lambda.ApiGateway`
- **SQS**: `eventPipeline.UseSqs(...)`, in `Benzene.Aws.Lambda.Sqs`
- **SNS**: `eventPipeline.UseSns(...)`, in `Benzene.Aws.Lambda.Sns`
- **Kafka**: `eventPipeline.UseKafka(...)`, in `Benzene.Aws.Lambda.Kafka`
- **S3**: `eventPipeline.UseS3(...)`, in `Benzene.Aws.Lambda.S3`

### SNS

```csharp
eventPipeline.UseSns(snsApp => snsApp
    .UseCorrelationId()
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

SNS invokes your function via a resource-based Lambda permission — no extra
execution-role IAM is needed to receive notifications (see
[AWS IAM Permissions](aws-iam-permissions)). The SNS message's `topic` message
attribute (or the raw topic ARN, depending on delivery configuration) is used to route
to the matching message handler, same as every other transport.

### S3

```csharp
eventPipeline.UseS3(s3App => s3App
    .UseCorrelationId()
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

Like SNS, S3 invokes via a resource-based permission plus a bucket notification
configuration — no extra execution-role IAM needed to receive. S3 event notifications
are fire-and-forget: no response is written back, since S3 doesn't expect one.

### Kafka

```csharp
eventPipeline.UseKafka(kafkaApp => kafkaApp
    .UseCorrelationId()
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

Works for both MSK and self-managed Kafka. Kafka record headers are mapped to Benzene
message headers, and partition/offset are available on `KafkaContext`. See
[AWS IAM Permissions](aws-iam-permissions) for the MSK-specific permissions your
execution role needs — these are more involved than the other event sources since MSK
event source mappings require VPC connectivity.

## IAM Permissions

Each event source above has different IAM requirements — some need explicit
execution-role permissions (SQS, Kafka), others invoke your function via a
resource-based permission and need none. See
[AWS IAM Permissions Reference](aws-iam-permissions) for a minimal policy per
package, with the specific SDK call in Benzene's source that drives each requirement.

## Configuration

`GetConfiguration()` runs once on cold start, before any services are registered, and its
result is passed into both `ConfigureServices` and `Configure`. Anything built on top of
`Microsoft.Extensions.Configuration` works here — the example above reads environment
variables (the natural fit for Lambda, where you set configuration via the function's
environment variables in the console, SAM template, or CDK/Terraform), but `AddJsonFile(...)`,
AWS Systems Manager Parameter Store providers, or AWS Secrets Manager providers all work the
same way.

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

See [`examples/Aws`](../examples/Aws) for a complete, runnable project covering all of the
above, including SQS, SNS, health checks, and validation.
