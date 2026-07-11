# ASP.NET Core Integration

Benzene provides seamless integration with ASP.NET Core, allowing you to use its hexagonal message handling pattern within a standard web application.

## Setup

To use Benzene in an ASP.NET Core project, you need to register the dependencies and add the Benzene middleware.

### Register Services

In your `Startup.cs` or `Program.cs`, use the `UsingBenzene()` extension method on `IServiceCollection`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

    // Register Benzene services and message handlers (discovered by reflection from the given assembly)
    services.UsingBenzene(x => x.AddMessageHandlers(typeof(MyHandler).Assembly));

    // Optional: Add FluentValidation
    services.AddValidatorsFromAssembly(typeof(MyValidator).Assembly);
}
```

### Add Middleware

Add the Benzene middleware to the `IApplicationBuilder` pipeline.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting();

    app.UseBenzene(benzene => benzene
        .UseAspNet(asp => asp
            .UseCorrelationId()
            .UseMessageHandlers(router => router
                .UseFluentValidation()
            )
        )
    );

    app.UseEndpoints(endpoints => { 
        endpoints.MapControllers(); 
    });
}
```

## Routing

Benzene can handle requests based on HTTP method and path mapping them to message topics.

You can define these mappings by implementing `IHttpEndpointDefinition` or using attributes on your message handlers if you have the appropriate scanners configured.

Example manual registration:

```csharp
services.AddSingleton<IHttpEndpointDefinition>(new HttpEndpointDefinition("POST", "/orders", "order:create"));
```

## Benefits

Using Benzene with ASP.NET Core allows you to:
- Write business logic that is independent of the web host.
- Reuse the same message handlers in different environments (e.g., AWS Lambda, Azure Functions, or Self-Hosted).
- Leverage Benzene's middleware pipeline for cross-cutting concerns like correlation IDs, logging, and validation.
