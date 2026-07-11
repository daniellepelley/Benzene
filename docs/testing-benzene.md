# Testing Benzene

You can create a virtual host for your AWS Lambda and use this to send messages into the Lambda. The configuration and the services can be overriden which means you can introduces mocks or change the configuration to point to a locally running component such as a database.

This is the recommended approach to testing services as it allows you to test the service end to end.

## Creating a Test Host

```csharp
AwsLambdaBenzeneTestStartUp<TStartUp>
```

This allows you to run an AWS Lambda in memory and test it end to end by sending messages into it, using your real production `StartUp` class (the same one deployed to Lambda).

```csharp
var testHost = new AwsLambdaBenzeneTestStartUp<StartUp>()
    .BuildHost();
```

### Override Configuration

You can override configuration by adding a dictionary of the configuration values you want to override. This is especially useful if you are using Docker to replace services that the Lambda depends on.

```csharp
var testHost = new AwsLambdaBenzeneTestStartUp<StartUp>()
    .WithConfiguration(new Dictionary<string, string>
    {
        { "some-key", "some-value" }
    })
    .BuildHost();
```

### Override Services

You can override service registration with any fakes or mocks which allows you to stub out things such as calls to other services.

```csharp
var testHost = new AwsLambdaBenzeneTestStartUp<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
    .BuildHost();
```

### Sending Messages

You can send a message to the hosted Lambda using `SendBenzeneMessageAsync`, built with `MessageBuilder`. The response comes back as a `BenzeneMessageResponse`, containing the status code and the serialized body.

```csharp
var message = MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" });
var response = await testHost.SendBenzeneMessageAsync(message);
```

### Example Test

```csharp
[Fact]
public async Task HeyWorld_BenzeneMessage()
{
    var mockHelloWorldService = new Mock<IHelloWorldService>();
    var testHost = new AwsLambdaBenzeneTestStartUp<StartUp>()
        .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
        .BuildHost();

    var message = MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" });
    var response = await testHost.SendBenzeneMessageAsync(message);

    mockHelloWorldService.Verify(x => x.GetAsync(It.IsAny<HelloWorldMessage>()), Times.Once());
    Assert.Equal("200", response.StatusCode);
    var payload = JsonConvert.DeserializeObject<HelloWorldResponse>(response.Body);
    Assert.Equal("Hello World", payload.Message);
}
```
