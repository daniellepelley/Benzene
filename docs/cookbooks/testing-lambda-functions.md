# Integration-Test Lambda Handlers End-to-End Without Deploying

Build a full in-memory test suite for a multi-event-source Lambda function — API Gateway and SQS —
using the same `StartUp` class you deploy, so a passing test suite means the real pipeline works.

## Problem Statement

You've written a Lambda function with `BenzeneStartUp` that handles both an API Gateway endpoint
and an SQS queue. Before deploying, you want to:

- Exercise both event sources end to end (request → routing → handler → response) without deploying
  or running SAM local
- Assert on the actual response shape API Gateway callers see (status code, body) and on the actual
  SQS partial-batch-failure behavior
- Assert on side effects — that a downstream dependency was actually called with the right
  arguments — using mocks, without spinning up real infrastructure
- Avoid the DI setup mistakes that produce a working build but a runtime `BenzeneException` the
  first time a message actually flows through the pipeline

This cookbook assumes you've read [Testing Benzene](../testing-benzene.md) (the reference doc for
`BenzeneTestHost`) and the testing section of
[Getting Started: Benzene on AWS Lambda](../getting-started-aws.md#6-test-locally-with-benzenetesthost).
Both are accurate ground truth for the `BenzeneTestHost.Create<TStartUp>().BuildAwsLambdaHost()`
API; this cookbook goes one level deeper with a complete, realistic multi-handler test suite built
on top of it, plus a troubleshooting section for setup mistakes that don't show up until runtime.

## Prerequisites

- A Lambda function project using `BenzeneStartUp` with at least one API Gateway and one SQS
  handler wired up (see [Getting Started: Benzene on AWS Lambda](../getting-started-aws.md)).
- A test project referencing the function project.
- xUnit and Moq (the test conventions used throughout Benzene's own `test/` folder).

## Installation

```bash
dotnet add package Benzene.Testing --prerelease
dotnet add package Benzene.Tools --prerelease
dotnet add package Benzene.Aws.Lambda.ApiGateway.TestHelpers --prerelease
dotnet add package Benzene.Aws.Lambda.Sqs.TestHelpers --prerelease
dotnet add package xunit
dotnet add package Moq
```

`Benzene.Testing` provides `BenzeneTestHost`/`HttpBuilder`/`MessageBuilder`. `Benzene.Tools`
provides `AwsLambdaBenzeneTestHost`, the wrapper that turns the built `IAwsLambdaEntryPoint` into
something you can send events into and get typed responses back from. The two `TestHelpers`
packages add the `SendApiGatewayAsync`/`SendSqsAsync` extensions and the
`AsApiGatewayRequest`/`AsSqs` builder conversions for their respective event sources.

## The App Under Test

A small order-processing function: `POST /orders` charges a customer through a payment gateway
port and returns the created order; an `orders:shipped` SQS message notifies the customer's
shipping notifier port. Both handlers depend on interfaces (ports) that are easy to mock in tests.

```csharp
// Handlers.cs
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

public interface IPaymentGateway
{
    Task<IBenzeneResult<string>> ChargeAsync(string customerId, int amountInCents);
}

public interface IShippingNotifier
{
    Task NotifyCustomerAsync(Guid orderId, string trackingNumber);
}

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = "";
    public int AmountInCents { get; set; }
}

public class CreateOrderResponse
{
    public Guid OrderId { get; set; } = default!;
    public string ChargeId { get; set; } = "";
}

[Message("orders:create")]
[HttpEndpoint("POST", "/orders")]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly IPaymentGateway _paymentGateway;

    public CreateOrderMessageHandler(IPaymentGateway paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    public async Task<IBenzeneResult<CreateOrderResponse>> HandleAsync(CreateOrderRequest message)
    {
        var charge = await _paymentGateway.ChargeAsync(message.CustomerId, message.AmountInCents);

        if (!charge.IsSuccessful)
        {
            return BenzeneResult.BadRequest<CreateOrderResponse>("Payment declined");
        }

        return BenzeneResult.Created(new CreateOrderResponse
        {
            OrderId = Guid.NewGuid(),
            ChargeId = charge.Payload
        });
    }
}

public class OrderShippedEvent
{
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = "";
}

[Message("orders:shipped")]
public class OrderShippedMessageHandler : IMessageHandler<OrderShippedEvent, Void>
{
    private readonly IShippingNotifier _notifier;

    public OrderShippedMessageHandler(IShippingNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task<IBenzeneResult<Void>> HandleAsync(OrderShippedEvent message)
    {
        await _notifier.NotifyCustomerAsync(message.OrderId, message.TrackingNumber);
        return BenzeneResult.Ok(new Void());
    }
}
```

```csharp
// StartUp.cs
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() =>
        new ConfigurationBuilder().AddEnvironmentVariables().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            // Required because the SQS handler below resolves ISetCurrentTransport directly
            // (SqsApplication.HandleAsync) — see Troubleshooting if you forget this.
            .AddBenzene()
            .AddMessageHandlers(typeof(CreateOrderMessageHandler).Assembly)
            .AddHttpMessageHandlers());

        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IShippingNotifier, EmailShippingNotifier>();
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseApiGateway(apiGatewayApp => apiGatewayApp
                .UseMessageHandlers())
            .UseSqs(sqsApp => sqsApp
                .UseMessageHandlers()));
    }
}
```

`UseApiGateway(...)` and `UseSqs(...)` both hang off the same `eventPipeline` — a single Lambda
function handling multiple event sources, exactly as described in
[Getting Started: Supported Event Sources](../getting-started-aws.md#supported-event-sources).

## The Test Suite

```csharp
// OrderFunctionTests.cs
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SQSEvents;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.Results;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class OrderFunctionTests
{
    private static AwsLambdaBenzeneTestHost BuildHost(
        Mock<IPaymentGateway> paymentGateway,
        Mock<IShippingNotifier> notifier)
    {
        return new AwsLambdaBenzeneTestHost(
            BenzeneTestHost.Create<StartUp>()
                .WithServices(services =>
                {
                    services.AddScoped(_ => paymentGateway.Object);
                    services.AddScoped(_ => notifier.Object);
                })
                .BuildAwsLambdaHost());
    }

    [Fact]
    public async Task CreateOrder_ChargeSucceeds_Returns201WithOrder()
    {
        var paymentGateway = new Mock<IPaymentGateway>();
        paymentGateway
            .Setup(x => x.ChargeAsync("customer-1", 2500))
            .ReturnsAsync(BenzeneResult.Ok("charge-abc"));

        using var host = BuildHost(paymentGateway, new Mock<IShippingNotifier>());

        var request = HttpBuilder
            .Create("POST", "/orders", new CreateOrderRequest { CustomerId = "customer-1", AmountInCents = 2500 });

        var response = await host.SendApiGatewayAsync(request);

        Assert.Equal(201, response.StatusCode);
        var body = AwsLambdaBenzeneTestHost.StreamToObject<CreateOrderResponse>(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response.Body)));
        Assert.Equal("charge-abc", body.ChargeId);

        paymentGateway.Verify(x => x.ChargeAsync("customer-1", 2500), Times.Once());
    }

    [Fact]
    public async Task CreateOrder_ChargeDeclined_Returns400()
    {
        var paymentGateway = new Mock<IPaymentGateway>();
        paymentGateway
            .Setup(x => x.ChargeAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(BenzeneResult.BadRequest<string>("card_declined"));

        using var host = BuildHost(paymentGateway, new Mock<IShippingNotifier>());

        var request = HttpBuilder
            .Create("POST", "/orders", new CreateOrderRequest { CustomerId = "customer-1", AmountInCents = 2500 });

        var response = await host.SendApiGatewayAsync(request);

        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task OrderShipped_NotifiesCustomer_NoBatchFailures()
    {
        var notifier = new Mock<IShippingNotifier>();
        var orderId = Guid.NewGuid();

        using var host = BuildHost(new Mock<IPaymentGateway>(), notifier);

        var message = MessageBuilder.Create("orders:shipped", new OrderShippedEvent
        {
            OrderId = orderId,
            TrackingNumber = "1Z999AA10123456784"
        });

        SQSBatchResponse response = await host.SendSqsAsync(message);

        Assert.Empty(response.BatchItemFailures);
        notifier.Verify(x => x.NotifyCustomerAsync(orderId, "1Z999AA10123456784"), Times.Once());
    }

    [Fact]
    public async Task OrderShipped_NotifierThrows_ReportsPartialBatchFailure()
    {
        var notifier = new Mock<IShippingNotifier>();
        notifier
            .Setup(x => x.NotifyCustomerAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("email provider down"));

        using var host = BuildHost(new Mock<IPaymentGateway>(), notifier);

        var message = MessageBuilder.Create("orders:shipped", new OrderShippedEvent
        {
            OrderId = Guid.NewGuid(),
            TrackingNumber = "1Z999AA10123456784"
        });

        SQSBatchResponse response = await host.SendSqsAsync(message);

        // SqsApplication reports the failing record so SQS retries/DLQs just that message,
        // not the whole batch — see handling-sqs-failures.md.
        Assert.Single(response.BatchItemFailures);
    }
}
```

A few things worth calling out:

- `BuildHost(...)` is a small private factory that centralizes the `WithServices` overrides — every
  test gets its own pair of mocks, so assertions on one test's mock can't leak into another.
- `AwsLambdaBenzeneTestHost` implements `IDisposable` (it disposes the underlying
  `IAwsLambdaEntryPoint`); the `using var host = ...` pattern above releases it after each test.
- `response.Body` on `APIGatewayProxyResponse` is a plain JSON string — deserialize it with
  whatever the rest of your test project already uses (`System.Text.Json`, `Newtonsoft.Json`, or
  `AwsLambdaBenzeneTestHost.StreamToObject<T>` wrapped around a `MemoryStream`, as above).
- The SQS test asserts on `response.BatchItemFailures`, not a return value from the handler — that
  list is exactly what `SqsLambdaHandler`/`SqsApplication` reports back to the real Lambda service
  for partial-batch retry, so asserting on it here is asserting on real AWS-facing behavior, not an
  implementation detail. See
  [Handling SQS Message Failures](handling-sqs-failures.md) for the retry/DLQ picture this feeds
  into.

## Troubleshooting

### `BenzeneException: Unable to resolve type ...ISetCurrentTransport, ...`

This is the most common first-run failure for a function that wires up SQS, SNS, or Kafka (or the
BenzeneMessage bridge via `UseBenzeneMessage(...)`). Those transports call
`serviceResolver.GetService<ISetCurrentTransport>()` directly while dispatching each
message/record — unlike `UseApiGateway(...)`, which doesn't need it — and `ISetCurrentTransport` is
only registered by `.AddBenzene()`:

```csharp
services.UsingBenzene(x => x
    .AddBenzene()               // <-- registers ISetCurrentTransport (and ICurrentTransport)
    .AddMessageHandlers(typeof(CreateOrderMessageHandler).Assembly)
    .AddHttpMessageHandlers());
```

If your `Configure` also bridges through `UseBenzeneMessage(...)` (rather than `UseSqs`/`UseSns`
directly), you additionally need `.AddBenzeneMessage()`:

```csharp
services.UsingBenzene(x => x
    .AddBenzene()
    .AddBenzeneMessage()
    .AddMessageHandlers(typeof(CreateOrderMessageHandler).Assembly));
```

Because `BuildAwsLambdaHost()` performs the exact same construction as a real deployment (see
[Getting Started: Troubleshooting](../getting-started-aws.md#troubleshooting)), this mistake
surfaces identically in a test and in production — the test failing here is exactly the point: it
catches the misconfiguration before a real SQS message ever hits the deployed function.

### `WithServices` mock never gets called / real dependency runs instead

`WithServices(...)` actions run immediately after `StartUp.ConfigureServices` (see
[Testing Benzene: Override Services](../testing-benzene.md#override-services)), so they only
override a registration your `StartUp` actually makes. If `ConfigureServices` doesn't register
`IPaymentGateway` at all (e.g. it's resolved via a concrete class with no interface registration),
there's nothing for the override to replace. Add the interface registration in `StartUp` first,
then override it in the test.

### 404 / handler never invoked from `SendApiGatewayAsync`

Same causes as a real deployment (see
[Getting Started: Troubleshooting](../getting-started-aws.md#troubleshooting)): the `HttpBuilder`
method/path must match `[HttpEndpoint("METHOD", "/path")]` exactly, and the handler's assembly must
be passed to `AddMessageHandlers(...)` (or you used the no-argument overload, which only scans the
calling assembly — pass the handler assembly explicitly if handlers live in a different project
than `StartUp`).

### SQS test always reports a batch failure, even with a passing handler

Check that `MessageBuilder.Create(topic, message)` uses the *exact* topic your `[Message("...")]`
attribute declares — `AsSqs()` puts `Topic` into a `topic` message attribute, and
`SqsMessageTopicGetter` routes purely off that attribute, not the message body. A topic typo means
the router finds no matching handler, which the pipeline reports as a failed record, not a
thrown exception.

### Assertions on a mock fail even though the test "looks" async-correct

`BuildAwsLambdaHost()` and `SendApiGatewayAsync`/`SendSqsAsync` are `async` all the way down —
missing an `await` on `host.Send...Async(...)` means your assertions run before the handler (and
therefore the mocked dependency) has actually been invoked. This is a plain C# `async`/`await`
mistake, not Benzene-specific, but it's an easy one to make when a test method forgets to declare
itself `async Task`.

### Test behaves differently from the deployed function

`BuildAwsLambdaHost()` performs the exact same `GetConfiguration()`/`ConfigureServices()`/
`Configure()` construction `AwsLambdaHost<TStartUp>` does for a real deployment — a divergence
almost always means a `WithServices`/`WithConfiguration` override in the test is masking something
real (e.g. overriding a repository that would otherwise fail to connect). Temporarily remove the
override to confirm, per
[Getting Started: Troubleshooting](../getting-started-aws.md#troubleshooting).

## Variations

### Testing the legacy `AwsLambdaStartUp`

If your function still uses the older, AWS-only `AwsLambdaStartUp` base class instead of the
platform-neutral `BenzeneStartUp` used above, use `AwsLambdaBenzeneTestStartUp<TStartUp>` instead of
`BenzeneTestHost` — see
[Testing Benzene: Testing a legacy `AwsLambdaStartUp`-based app](../testing-benzene.md#testing-a-legacy-awslambdastartup-based-app)
for the equivalent pattern.

### Asserting on invocation identity

If your pipeline adds `.UseBenzeneInvocation()` on the outer `eventPipeline` (see
[Getting Started: Observability](../getting-started-aws.md#observability)), you can inject
`IBenzeneInvocation` into a handler and assert on `InvocationId` the same way as any other
constructor dependency — it's populated per-invocation for a single-request pipeline like API
Gateway, but not inside SQS/SNS/Kafka's per-message batch dispatch.

### A topic-routed test without a specific transport

If your `Configure` wires up `UseBenzeneMessage(...)`, you can send a plain `MessageBuilder`
straight at the pipeline via `SendBenzeneMessageAsync` (from
`Benzene.Core.MessageHandlers.TestHelpers`) instead of going through a specific transport's event
shape — see [Testing Benzene: AWS Lambda](../testing-benzene.md#aws-lambda).

## Further Reading

- [Testing Benzene](../testing-benzene.md) — the full `BenzeneTestHost` reference, including
  Azure Functions, ASP.NET Core, and Worker hosts
- [Getting Started: Benzene on AWS Lambda](../getting-started-aws.md) — the platform-neutral
  `BenzeneStartUp` shape this cookbook's `StartUp` follows
- [Handling SQS Message Failures](handling-sqs-failures.md) — the retry/DLQ behavior behind the
  `BatchItemFailures` assertions above
- [Message Handlers](../message-handlers.md) — `IBenzeneResult<T>`/`BenzeneResultStatus` used in
  the handlers under test
