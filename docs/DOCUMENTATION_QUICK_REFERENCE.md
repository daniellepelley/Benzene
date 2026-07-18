# Benzene Documentation Quick Reference

Quick reference for creating and maintaining Benzene documentation.

## Documentation Types

| Type | Purpose | Location | Structure |
|------|---------|----------|-----------|
| **Getting Started** | Zero to deployed tutorial | `docs/getting-started-{platform}.md` | Prerequisites → Setup → Code → Deploy → Troubleshoot |
| **Reference** | Feature/API documentation | `docs/{feature}.md` | Overview → Install → Basic → Advanced → See Also |
| **Cookbook** | Solve specific problem | `docs/cookbooks/{recipe}.md` | Problem → Prerequisites → Steps → Test → Troubleshoot → Variations |

## Benzene Conventions

### Code Structure
```csharp
// StartUp pattern
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() { }
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) { }
}

// Message Handler pattern
[Message("topic:name")]
[HttpEndpoint("GET", "/path")]
public class MyHandler : IMessageHandler<TRequest, TResponse>
{
    public Task<IBenzeneResult<TResponse>> HandleAsync(TRequest message) { }
}

// Middleware registration
app.UseAwsLambda(eventPipeline => eventPipeline
    .UseApiGateway(apiGatewayApp => apiGatewayApp
        .UseMessageHandlers()));
```

### Service Registration
```csharp
services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(MyHandler).Assembly)
    .AddHttpMessageHandlers()
    .AddDiagnostics());
```

## Package Reference

| Pattern | Packages |
|---------|----------|
| **Core** | `Benzene.Abstractions.*`, `Benzene.Core.*`, `Benzene.Http`, `Benzene.Results` |
| **AWS** | `Benzene.Aws.Lambda.*`, `Benzene.Aws.Sqs`, `Benzene.Clients.Aws` |
| **Azure** | `Benzene.Azure.Function.*`, `Benzene.AspNet.Core` |
| **Messaging (self-hosted)** | `Benzene.Kafka.Core`, `Benzene.RabbitMq`, `Benzene.Azure.ServiceBus`, `Benzene.Azure.EventHub` |
| **Validation** | `Benzene.FluentValidation`, `Benzene.DataAnnotations` |
| **Observability** | `Benzene.Diagnostics`, `Benzene.OpenTelemetry`, `Benzene.HealthChecks.*` |
| **Infrastructure** | `Benzene.Microsoft.Dependencies`, `Benzene.Cache.*`, `Benzene.Resilience`, `Benzene.Resilience.Polly` |

## Common Middleware

```csharp
// Observability
.UseW3CTraceContext()                  // W3C distributed tracing
.UseBenzeneEnrichment()                // Portable log enrichment
.UseTimer("name")                      // Named timer spans
.UseBenzeneMetrics()                   // OTel metrics

// Validation
.UseFluentValidation()                 // FluentValidation integration
.UseDataAnnotations()                  // DataAnnotations validation

// Health
.UseHealthCheck("path", builder)       // Health check endpoint

// Logging
.UseLogResult(x => x                   // Structured log scopes
    .WithCorrelationId()
    .WithTopic()
    .WithTransport())
```

## Documentation Checklist

### Before Writing
- [ ] Read existing docs for style
- [ ] Examine source code for accuracy
- [ ] Check examples for patterns
- [ ] Review tests for usage
- [ ] Identify cross-references

### While Writing
- [ ] Use clear, active voice
- [ ] Provide complete code examples
- [ ] Include all using statements
- [ ] Follow Benzene conventions
- [ ] Explain why, not just what

### Before Publishing
- [ ] Code examples are complete
- [ ] Package names are correct
- [ ] Using statements included
- [ ] Cross-references work
- [ ] Prerequisites stated
- [ ] Troubleshooting included
- [ ] Markdown formatted correctly

## Example Requests

### Getting Started Guide
```
Write a getting started guide for Benzene with AWS Lambda and SQS
```

### Reference Documentation
```
Create reference documentation for the health check system
```

### Cookbook
```
Write a cookbook for implementing distributed tracing with OpenTelemetry
```

## Common Scenarios

### AWS Lambda
```csharp
// Packages
dotnet add package Benzene.Aws.Lambda.Core --prerelease
dotnet add package Benzene.Aws.Lambda.ApiGateway --prerelease

// Entry point
public class Function : AwsLambdaHost<StartUp> { }

// Configure
app.UseAwsLambda(eventPipeline => eventPipeline
    .UseApiGateway(apiGatewayApp => apiGatewayApp
        .UseMessageHandlers()));
```

### Azure Functions
```csharp
// Packages
dotnet add package Benzene.Azure.Function.Core --prerelease
dotnet add package Benzene.Azure.Function.AspNet --prerelease

// Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker => worker.UseBenzene())
    .Build();
host.Run();

// StartUp
public class StartUp : BenzeneStartUp
{
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) =>
        app.UseHttp(http => http.UseMessageHandlers());
}
```

### ASP.NET Core
```csharp
// Packages
dotnet add package Benzene.AspNet.Core --prerelease

// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();
var app = builder.Build();
app.UseBenzene();

// StartUp
public class StartUp : BenzeneStartUp
{
    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) =>
        app.UseHttp(http => http
            .UseMessageHandlers());
}
```

## Troubleshooting Patterns

### Handler Not Found
- Check `[Message("topic")]` attribute
- Verify handler assembly registered
- Confirm topic routing configuration

### Validation Not Working
- Check validation middleware order
- Verify validator registered in DI
- Ensure validation package installed

### Logs Not Appearing
- Check log level configuration
- Verify logger provider registered
- Ensure `IncludeScopes = true` for structured logs

### Correlation Not Working
- Add `UseW3CTraceContext()`
- Check header names match
- Verify middleware order (should be early)

## File Structure

```
docs/
├── index.md                          # Main index
│
├── getting-started-aws.md            # Platform guides
├── azure-functions.md
├── asp-net-core.md
├── getting-started-kafka.md
├── getting-started-rabbitmq.md
├── getting-started-worker.md
│
├── message-handlers.md               # Core concepts
├── middleware.md
├── common-middleware.md
│
├── monitoring.md                     # Features
├── health-checks.md
├── fluent-validation.md
├── correlation-ids.md
├── testing-benzene.md
│
└── cookbooks/                        # Recipes
    ├── README.md
    ├── logging-application-insights.md
    ├── handling-sqs-failures.md
    └── ...
```

## Resources

- **Agent**: `.claude/agents/documentation-writer.md`
- **Guide**: `.claude/DOCUMENTATION_GUIDE.md`
- **Setup**: `DOCUMENTATION_WRITER_SETUP.md`
- **Source**: `src/` for implementation details
- **Examples**: `examples/` for working code
- **Tests**: `test/` for usage patterns

## Quick Links

- [Documentation Writer Setup](../DOCUMENTATION_WRITER_SETUP.md)
- [Documentation Guide](../.claude/DOCUMENTATION_GUIDE.md)
- [Cookbook Index](cookbooks/README.md)
- [Product Owners](../.claude/PRODUCT_OWNERS.md)

---

**Tip**: Always verify implementation details by reading source code. Never guess about features or configuration options.
