# Logging to Application Insights

Send structured logs from your Benzene services to Azure Application Insights with correlation and custom properties.

## Problem Statement

You're running Benzene services (in Azure Functions, AWS Lambda, or containers) and need to:
- Send all logs to Application Insights for centralized monitoring
- Include correlation IDs to trace requests across services
- Add custom properties (topic, transport, handler) to filter and analyze logs
- Use structured logging for better querying in Application Insights

## Prerequisites

- A Benzene service (Azure Functions, AWS Lambda, or ASP.NET Core)
- An Application Insights resource in Azure
- The Application Insights instrumentation key or connection string

## Installation

Install the required NuGet packages:

```bash
dotnet add package Benzene.Diagnostics --prerelease
dotnet add package Microsoft.ApplicationInsights.AspNetCore
# OR for non-ASP.NET hosts:
dotnet add package Microsoft.Extensions.Logging.ApplicationInsights --prerelease
```

## Step-by-Step Implementation

### 1. Add Application Insights Configuration

Set your Application Insights connection string in configuration. For Azure Functions, add it to `local.settings.json` (local) or Function App settings (deployed):

```json
{
  "Values": {
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=your-key-here;..."
  }
}
```

For AWS Lambda, use an environment variable:

```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=your-key-here;...
```

### 2. Configure Services

In your `BenzeneStartUp` class, configure logging and diagnostics:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Diagnostics;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add Application Insights
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        });

        // Configure logging with Application Insights
        services.AddLogging(builder =>
        {
            builder.AddApplicationInsights(
                configureTelemetryConfiguration: (config) =>
                {
                    config.ConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
                },
                configureApplicationInsightsLoggerOptions: (options) =>
                {
                    // Include scopes to capture structured properties
                    options.IncludeScopes = true;
                });

            // Optional: Set minimum log level
            builder.SetMinimumLevel(LogLevel.Information);

            // Optional: Filter out noisy logs
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("System", LogLevel.Warning);
        });

        // Add Benzene diagnostics for automatic instrumentation
        services.UsingBenzene(x => x
            .AddDiagnostics()
            .AddMessageHandlers(typeof(MyHandler).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseApiGateway(apiGatewayApp => apiGatewayApp
                // Add structured log enrichment
                .UseBenzeneEnrichment()
                .UseMessageHandlers()));
    }
}
```

### 3. Use Structured Logging in Your Handlers

Inject `ILogger<T>` into your message handlers and use structured logging:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Microsoft.Extensions.Logging;

[Message("order:create")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IOrderRepository _orderRepository;

    public CreateOrderHandler(
        ILogger<CreateOrderHandler> logger,
        IOrderRepository orderRepository)
    {
        _logger = logger;
        _orderRepository = orderRepository;
    }

    public async Task<IBenzeneResult<CreateOrderResponse>> HandleAsync(CreateOrderRequest request)
    {
        // Structured logging with named properties
        _logger.LogInformation(
            "Creating order for customer {CustomerId} with {ItemCount} items",
            request.CustomerId,
            request.Items.Count);

        try
        {
            var order = await _orderRepository.CreateAsync(request);

            _logger.LogInformation(
                "Order {OrderId} created successfully for customer {CustomerId}",
                order.Id,
                request.CustomerId);

            return BenzeneResult.Ok(new CreateOrderResponse { OrderId = order.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create order for customer {CustomerId}",
                request.CustomerId);

            return BenzeneResult.ServiceUnavailable<CreateOrderResponse>("Order creation failed");
        }
    }
}
```

### 4. Query Logs in Application Insights

The structured properties are available in the `customDimensions` field. Query them with KQL:

```kql
traces
| where timestamp > ago(1h)
| where message contains "Creating order"
| extend CustomerId = customDimensions.CustomerId
| extend ItemCount = customDimensions.ItemCount
| extend Topic = customDimensions["benzene.topic"]
| extend CorrelationId = customDimensions["benzene.invocationId"]
| project timestamp, message, CustomerId, ItemCount, Topic, CorrelationId
```

Find errors for a specific customer:

```kql
traces
| where timestamp > ago(24h)
| where severityLevel >= 3  // Warning or higher
| extend CustomerId = customDimensions.CustomerId
| where CustomerId == "customer-123"
| project timestamp, message, severityLevel, customDimensions
```

Trace a request across multiple handlers:

```kql
traces
| where timestamp > ago(1h)
| extend CorrelationId = customDimensions["benzene.invocationId"]
| where CorrelationId == "correlation-id-from-request"
| order by timestamp asc
| project timestamp, message, customDimensions["benzene.topic"], customDimensions["benzene.handler"]
```

## Testing

### Local Testing

Run your function locally and check the console output. With Application Insights configured, logs should appear in both the console and Application Insights (with a slight delay for the latter).

### Verify in Application Insights

1. In Azure Portal, navigate to your Application Insights resource
2. Go to **Logs** (under Monitoring)
3. Run a query to see your recent traces:

```kql
traces
| where timestamp > ago(5m)
| order by timestamp desc
| take 20
```

### Test Correlation

1. Make a request and note the correlation ID from the response headers
2. Query Application Insights with that correlation ID:

```kql
traces
| where customDimensions["benzene.invocationId"] == "your-correlation-id"
| order by timestamp asc
```

You should see all logs from that request in order.

## Troubleshooting

### Logs Not Appearing in Application Insights

**Problem**: Logs appear locally but not in Application Insights.

**Solution**:
1. Verify the connection string is correct and accessible
2. Check the `IncludeScopes` option is enabled (required for `customDimensions`)
3. Wait 2-5 minutes for logs to appear (Application Insights has a delay)
4. Check the log level - Application Insights may filter out `Debug` level logs

### Missing Custom Properties

**Problem**: Logs appear but `customDimensions` doesn't include Benzene properties.

**Solution**:
1. Ensure `UseBenzeneEnrichment()` is in your middleware pipeline
2. Verify `IncludeScopes = true` in Application Insights logger options
3. Check that `AddDiagnostics()` is called in service configuration

### High Log Volume / Cost

**Problem**: Too many logs are being sent, increasing Azure costs.

**Solution**:
1. Increase minimum log level:
   ```csharp
   builder.SetMinimumLevel(LogLevel.Warning);
   ```
2. Add filters for noisy namespaces:
   ```csharp
   builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Error);
   ```
3. Use sampling in Application Insights:
   ```csharp
   services.AddApplicationInsightsTelemetry(options =>
   {
       options.ConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
       options.EnableAdaptiveSampling = true;
   });
   ```

## Variations

### Using Serilog Instead

For richer structured logging, use Serilog with the Application Insights sink:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.ApplicationInsights
```

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var logger = new LoggerConfiguration()
        .WriteTo.ApplicationInsights(
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
            TelemetryConverter.Traces)
        .WriteTo.Console()
        .CreateLogger();

    services.AddLogging(builder =>
    {
        builder.AddSerilog(logger, dispose: true);
    });

    services.UsingBenzene(x => x
        .AddDiagnostics()
        .AddMessageHandlers(typeof(MyHandler).Assembly));
}
```

### Adding Custom Telemetry Initializers

To add custom properties to all telemetry:

```csharp
public class BenzeneTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.GlobalProperties["environment"] = "production";
        telemetry.Context.GlobalProperties["application"] = "benzene-api";
    }
}

// Register in ConfigureServices:
services.AddSingleton<ITelemetryInitializer, BenzeneTelemetryInitializer>();
```

## Further Reading

- [Monitoring & Diagnostics](../monitoring.md) - Benzene's observability features
- [Common Middleware](../common-middleware.md) - `UseBenzeneEnrichment()` details
- [Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Application Insights for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
- [Structured Logging with Serilog](structured-logging-serilog.md)
- [Distributed Tracing with OpenTelemetry](distributed-tracing-opentelemetry.md)
