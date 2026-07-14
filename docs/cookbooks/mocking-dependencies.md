# Mocking External Dependencies

Test message handlers in isolation by swapping their real dependencies — databases, HTTP clients,
cloud SDKs — for mocks, while still exercising the full Benzene pipeline.

## Problem Statement

You want to test a handler's behaviour without touching real infrastructure:
- Replace a repository, gateway, or SDK client with a fake
- Still run the message through the real pipeline (routing, deserialization, validation) so the
  test reflects production
- Assert both on the response and on how the dependency was called

## Prerequisites

- A Benzene service with a `StartUp` (see [Testing Benzene](../testing-benzene))
- A test project with a test framework (xUnit here) and a mocking library (Moq here)
- `Benzene.Testing` (and the `*.TestHelpers` package for your transport)

```bash
dotnet add package Benzene.Testing --prerelease
dotnet add package Benzene.Aws.Lambda.Sqs.TestHelpers --prerelease   # example: AWS
dotnet add package Moq
```

## The approach

Benzene's `BenzeneTestHost` builds an in-memory host from your **real production `StartUp`**, then
lets you override service registrations with `WithServices(...)`. Those overrides run immediately
after `StartUp.ConfigureServices`, so they replace whatever the StartUp registered — the standard
way to drop in a mock. The message still flows through the real pipeline end to end.

## Step-by-Step Implementation

### 1. A handler with a dependency

```csharp
[Message("order:get")]
public class GetOrderHandler : IMessageHandler<GetOrderMessage, OrderDto>
{
    private readonly IOrderService _orderService;

    public GetOrderHandler(IOrderService orderService) => _orderService = orderService;

    public async Task<IBenzeneResult<OrderDto>> HandleAsync(GetOrderMessage message)
        => await _orderService.GetAsync(message.Id);
}
```

### 2. Build a test host with the dependency mocked

```csharp
using Benzene.Tools;      // AwsLambdaBenzeneTestHost
using Benzene.Testing;    // BenzeneTestHost, MessageBuilder
using Moq;
using Xunit;

public class GetOrderHandlerTests
{
    [Fact]
    public async Task GetOrder_ReturnsOrder_FromService()
    {
        // Arrange: mock the dependency
        var orderService = new Mock<IOrderService>();
        orderService
            .Setup(x => x.GetAsync("123"))
            .ReturnsAsync(BenzeneResult.Ok(new OrderDto { Id = "123" }));

        // Build the real StartUp, but swap in the mock
        var host = new AwsLambdaBenzeneTestHost(
            BenzeneTestHost.Create<StartUp>()
                .WithServices(services => services.AddScoped(_ => orderService.Object))
                .BuildAwsLambdaHost());

        // Act: send a message through the real pipeline
        var message = MessageBuilder.Create("order:get", new GetOrderMessage { Id = "123" });
        var response = await host.SendBenzeneMessageAsync(message);

        // Assert: on the response …
        Assert.Equal("200", response.StatusCode);

        // … and on how the dependency was used
        orderService.Verify(x => x.GetAsync("123"), Times.Once());
    }
}
```

`WithServices` runs after `ConfigureServices`, so `AddScoped(_ => orderService.Object)` overrides
the real `IOrderService` registration. `SendBenzeneMessageAsync` works for any StartUp that wires
up `UseBenzeneMessage(...)`; if your StartUp also wires API Gateway/SQS/SNS, use that transport's
`Send*Async` helper (`SendApiGatewayAsync`, `SendSqsAsync`, …) off the same host.

### 3. Override configuration too (optional)

If a dependency reads configuration, override values with `WithConfiguration` — applied before
`ConfigureServices` runs, so registrations see your test values:

```csharp
var host = BenzeneTestHost.Create<StartUp>()
    .WithConfiguration("FeatureFlags:NewPricing", "true")
    .WithServices(s => s.AddScoped(_ => orderService.Object))
    .BuildAwsLambdaHost();
```

## Testing per transport

The same host builder serves every transport; only the dispatch helper differs:

| Transport | Build | Dispatch |
|---|---|---|
| Benzene message (AWS) | `.BuildAwsLambdaHost()` (wrap in `AwsLambdaBenzeneTestHost`) | `SendBenzeneMessageAsync(message)` |
| API Gateway | same host | `SendApiGatewayAsync(...)` |
| SQS | same host | `SendSqsAsync(message)` → `SQSBatchResponse` |
| Azure Functions | `.BuildAzureFunctionApp()` | `HandleHttpRequest(...)`, `HandleEventHub(...)` |
| ASP.NET Core | `Program` + `WebApplicationFactory` | a real `HttpClient` |

See [Testing Benzene](../testing-benzene) for the full matrix, including ASP.NET Core and worker
hosts.

## Troubleshooting

### The real dependency is still used

**Problem**: The mock isn't taking effect.

**Solution**: `WithServices` overrides work by *last registration wins* for the resolved service.
Register the mock against the **same interface** the handler depends on (`IOrderService`), and use
a matching lifetime (`AddScoped` for scoped services). Confirm you're building from the same
`StartUp` the handler is registered by.

### `SendBenzeneMessageAsync` isn't found / doesn't dispatch

**Problem**: No response comes back.

**Solution**: `SendBenzeneMessageAsync` requires the StartUp to wire `UseBenzeneMessage(...)`. If
your service only exposes, say, API Gateway, dispatch with `SendApiGatewayAsync` (from
`Benzene.Aws.Lambda.ApiGateway.TestHelpers`) instead.

## Variations

### Testing the service, not the handler

If your handler is thin (delegating to a service, as recommended), you can also unit-test the
service directly with no host at all — reserve the test-host approach for verifying the handler
*and pipeline* (routing, validation, serialization) together.

### Fakes instead of mocks

Nothing requires Moq — register a hand-written in-memory fake (e.g. an `InMemoryOrderDbClient`)
via `WithServices` just the same. Benzene's own examples use in-memory implementations this way.

## Further Reading

- [Testing Benzene](../testing-benzene) - the complete testing guide
- [Message Handlers](../message-handlers) - why handlers stay thin and injectable
- [Integration Testing Lambda Functions](testing-lambda-functions.md) - end-to-end Lambda tests
- [Package Reference](../reference/packages#testing-support) - the testing packages
