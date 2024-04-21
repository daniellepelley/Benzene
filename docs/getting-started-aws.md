# Benzene

Benzene is a hexaganal framework designed for services running in serverless, containers or on physical servers. It is supports multiple cloud providers.

### Main Themes

[Message Handlers](message-handlers)


## Getting Started

```csharp
public class BareMetalLambdaEntryPoint
{
    private ServiceCollection _serviceCollection;
    private IMiddlewarePipeline<AwsEventStreamContext>? _app;

    public BareMetalLambdaEntryPoint()
    {
        _app = new AwsEventStreamApplication()
            .UseBenzeneMessage(x =>
                Extensions.UseMessageRouter<BenzeneMessageContext>(x, s =>
                    s.UseFluentValidation()));

        var services = new ServiceCollection();
        services
            .AddAwsMessageHandlers(Assembly.GetAssembly(typeof(OrderDto)));

        services.AddScoped<IOrderDbClient, OrderDbClient>();
        services.AddScoped<IOrderService, OrderService>();

        services.AddScoped<IDataContext>(x => new OrderDataContext(x.GetService<DataContext>()));

        _serviceCollection = services;
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