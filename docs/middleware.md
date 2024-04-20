Middleware in Hex is a software component that is assembled into an application pipeline to handle requests and responses. Each piece of middleware in the pipeline has a specific task and is responsible for invoking the next piece of middleware in the pipeline.

Middleware components can perform a variety of functions, such as:

- Handling requests and generating responses.
- Managing exception handling and error logging.
- Implementing caching.
- Authentication and authorization.

In Hex, the order in which middleware components are added to the pipeline is significant. The order determines the order in which the middleware components are invoked on requests, and the reverse order for the response. Therefore, the configuration of the middleware pipeline is an important aspect of application behavior and performance.

Here is a simple example of how middleware is configured in the LambdaEntryPoint class of a Hex project running in an AWS Lambda.


```csharp
app.UseDirectMessage(directMessageApp => directMessageApp
    .UseCorrelationId()
    .UseTimer("direct")
    .UseElementsLogContext()
    .UseProcessDirectMessageResponse()
    .UseHealthCheck("healthcheck", healthCheckBuilder)
    .UseMessageRouter(x => x
    .UseFluentValidation()
    )
);
```

### Inline Middleware
Simple middleware can be added inline within the pipeline itself. This is great if you want to experiment or added some simple functionality.

 
```csharp
.Use("middleware-demo", async (context, next) => 
{
    //Do something on the request
    await next();
    //Do something on the response
})
``` 

If needed you can pass in the IServiceResolver which allows you to resolve any service that has been registered by dependency injection.

In the example below ILogger is resolved so that the topic can be logged before next() is called and the request will continue down the middleware pipeline.



```csharp
.Use("middleware-demo", async (resolver, context, next) =>
{
    var logger = resolver.GetService<ILogger>();
    logger.LogInformation(context.DirectMessageRequest.Topic);
    await next();
})
```

### On Request
This is a middleware extension that will be called during the request.


```csharp
.OnRequest("request-demo", async (resolver, context) =>
{
    //Do something on the request
    var logger = resolver.GetService<ILogger>();
    logger.LogInformation(context.DirectMessageRequest.Topic);           
})
```

### On Response
This is a middleware extension that will be called during the response.


```csharp
.OnResponse("response-demo", async (context) =>
{
    //Do something on the response             
})
```