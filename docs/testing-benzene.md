# Testing Benzene

You can create a virtual host for your AWS Lambda and use this to send messages into the Lambda. The configuration and the services can be overriden which means you can introduces mocks or change the configuration to point to a locally running component such as a database.

This is the recommended approach to testing services as it allows you to test the service end to end.

## Creating a TestAwsLambdaHost

```csharp
TestAwsLambdaStartUp<TStartUp>
```

This allows you to run an AWS Lambda in memory and test it end to end by sending messages into it.



```csharp
var testAwsLambdaHost = new TestAwsLambdaStartUp<LambdaEntryPoint>()
    .BuildHost();
```

### Override Configuration

You can override configuration by adding a dictionary of the configuration values you want to override. This is especially useful if you are using Docker to replace services that the Lambda depends on.



```csharp
var testAwsLambdaHost = new TestAwsLambdaStartUp<LambdaEntryPoint>()
    .WithConfiguration(new Dictionary<string, string>
    {
        { "some-key", "some-value" }
    })
    .BuildHost();
```
 

### Override Services
You can override service registration with any fakes or mocks which allows you to stub out things such as calls to other services.



```csharp
var testAwsLambdaHost = new TestAwsLambdaStartUp<LambdaEntryPoint>()
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
    .BuildHost();
```

### Sending Events

You can send events to the hosted Lambda using SendEventAsync. You can optionally include the response type, otherwise it will return a Stream.

Depending how the service is built some events may not return a response out of the Lambda.


```csharp
var directMessageRequest = new DirectMessageRequest
{
    Topic = MessageTopicNames.HeyWorld,
    Message = JsonConvert.SerializeObject(message)
};
await testAwsLambdaHost.SendEventAsync<DirectMessageResponse>(directMessageRequest);
```

### Example Test

```csharp
[Fact]
public async Task HeyWorld_DirectMessage()
{
    var mockHelloWorldService = new Mock<IHelloWorldService>();
    var testAwsLambdaHost = new TestAwsLambdaStartUp<LambdaEntryPoint>()
        .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
        .BuildHost();
    var message = new HelloWorldMessage
    {
        Name = "World"
    };
    var directMessageRequest = EventBuilder.Create("hello:world", message).AsDirectMessage();
    var response = await testAwsLambdaHost.SendEventAsync<DirectMessageResponse>(directMessageRequest);
    mockHelloWorldService.Verify(x => x.GetAsync(It.IsAny<HelloWorldMessage>()), Times.Once());
    Assert.Equal("200", response.StatusCode);
    var payload = JsonConvert.DeserializeObject<HelloWorldResponse>(response.Message);
    Assert.Equal("Hello World", payload.Message);
}
```