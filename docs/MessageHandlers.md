# Message Handlers

Supports dependency injection from whichever DI provider you are using.

### Message Attribute

This defines the topic that is mapped to the message handler.

```csharp
[MessageTopic("demo-topic")]
```

A message handler most contain a single method, which takes in the message as a parameter and returns a response wrapped in a result.
[Message Results](MessageResult)

```csharp
Task<IHandlerResult<TResponse>> HandleAsync(TMessage message)
```

---
#### *Example* 

```csharp
[MessageTopic("demo-topic")]
public class DemoMessageHandler : IMessageHandler<DemoMessage, DemoResponse>
{
    private readonly ILogger _logger;

    public DemoMessageHandler(ILogger<DemoMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task<IHandlerResult<DemoResponse>> HandleAsync(DemoMessage message)
    {
        _logger.LogInformation("Processing Message");
        return await HandlerResult.Ok(new DemoResponse()).AsTask();
    }
}
```