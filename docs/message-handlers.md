# Message Handlers

Message handlers are the components that receive and process a message. They should be exactly one message handler for every topic that the service handles.

The topic and types are used to generate things like Open API documentation, code, configuration, etc. They are the front facing contract for the whole service.

They support dependency injection, so it is best practice to keep the code inside them very light and pass any business logic to a service or other type of class which can be injected in.

The response is wrapped in a ServiceResult object which allows contains information about the status of the process (Success or Failure), the payload if successful or the error if something has gone wrong.

New message handlers are discovered using reflection so they will be picked up automatically.

### Message Attribute

This defines the topic that is mapped to the message handler.

```csharp
[Message("demo-topic")]
```

A message handler most contain a single method, which takes in the message as a parameter and returns a response wrapped in a result.
[Message Results](message-result)

```csharp
Task<IServiceResult<TResponse>> HandleAsync(TMessage message)
```

---

### Request / Response

```csharp
    [Message("hello:world")]
    public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage, HelloWorldResponse>
    {
        private readonly IHelloWorldService _helloWorldService;
        private readonly ILogger _logger;
        public HelloWorldMessageHandler(IHelloWorldService helloWorldService, ILogger logger)
        {
            _logger = logger;
            _helloWorldService = helloWorldService;
        }
        public async Task<IServiceResult<HelloWorldResponse>> HandleAsync(HelloWorldMessage message)
        {
            _logger.LogInformation("Hello World");
            return await _helloWorldService.GetAsync(message);
        }
    }
``` 

### Event (Fire and Forget)
*Note: When there is no response on a message handler, an “Accepted” response will be returned to show that the event was handled.*



```csharp
[Message("hey:world")]
public class HeyWorldMessageHandler : IMessageHandler<HelloWorldMessage>
{
    private readonly IHelloWorldService _helloWorldService;
    private readonly ILogger _logger;
    public HeyWorldMessageHandler(IHelloWorldService helloWorldService, ILogger logger)
    {
        _logger = logger;
        _helloWorldService = helloWorldService;
    }
    public async Task HandleAsync(HelloWorldMessage message)
    {
        _logger.LogInformation("Hey World");
        await _helloWorldService.GetAsync(message);
    }
}
```