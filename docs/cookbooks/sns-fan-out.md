# SNS Fan-Out Pattern

Publish one event to an SNS topic and have several independent Lambda functions — each with their own SNS trigger and their own handlers — process a copy of it.

## Problem Statement

You have one event (e.g. "order created") that multiple, independently-deployed services need to react to: one Lambda sends a notification email, another updates analytics, a third reconciles inventory. Rather than the publisher knowing about every consumer, you publish once to an SNS topic and let each Lambda subscribe independently. This cookbook covers:

- Publishing a message to SNS with `Benzene.Clients.Aws`'s `SnsBenzeneMessageClient`
- Wiring an SNS-triggered Lambda to Benzene's message handler pipeline with `Benzene.Aws.Lambda.Sns`
- Subscribing multiple Lambda functions to the same SNS topic (including how much of this Benzene's Terraform code generator actually automates, and how much is plain AWS configuration)

## Prerequisites

- An SNS topic (or the ARN of one you'll create alongside your Lambdas)
- One Benzene Lambda function per consumer, each independently deployable

## Installation

Publisher (whichever service raises the event):

```bash
dotnet add package Benzene.Clients.Aws
```

Each subscriber Lambda:

```bash
dotnet add package Benzene.Aws.Lambda.Sns
```

## Step-by-Step Implementation

### 1. Publish to SNS

`Benzene.Clients.Aws.Sns.SnsBenzeneMessageClient` is the class that actually publishes — verified directly from `src/Benzene.Clients.Aws/Sns/SnsBenzeneMessageClient.cs`. It implements the same `IBenzeneMessageClient` interface as every other Benzene client:

```csharp
public class SnsBenzeneMessageClient : IBenzeneMessageClient
{
    public SnsBenzeneMessageClient(string topicArn, IAmazonSimpleNotificationService amazonSnsClient, ILogger<SnsBenzeneMessageClient> logger, IServiceResolver serviceResolver) { /* ... */ }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request) { /* ... */ }
}
```

One important gap to call out up front: **`Benzene.Clients.Aws.Sqs` and `Benzene.Clients.Aws.Lambda` both have `ClientsBuilder` registration sugar** (`CreateSqsBenzeneMessageClient(...)`, `CreateAwsLambdaBenzeneMessageClient(...)`) that build the client and register it on a `ClientsBuilder` in one call. **`Benzene.Clients.Aws.Sns` does not have an equivalent `CreateSnsBenzeneMessageClient` extension** — `src/Benzene.Clients.Aws/Sns/` only contains the client, its middleware (`SnsClientMiddleware`), and pipeline-level `UseSnsClient`/`UseSns<T>` extensions for building a raw send pipeline, not a `ClientsBuilder`-registration helper. If you want the equivalent convenience for SNS you either construct `SnsBenzeneMessageClient` directly, or wire it onto a `ClientsBuilder`/`SingleClientsBuilder` by hand.

**Direct construction** (simplest, and exactly what `test/Benzene.Core.Test/Clients/Aws/Sqs/SqsBenzeneMessageClientTest.cs` does for the SQS equivalent):

```csharp
using Amazon.SimpleNotificationService;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Messages.MessageSender;
using Microsoft.Extensions.Logging;

var client = new SnsBenzeneMessageClient(
    topicArn: "arn:aws:sns:eu-west-2:123456789012:order-events",
    amazonSnsClient: new AmazonSimpleNotificationServiceClient(),
    logger: loggerFactory.CreateLogger<SnsBenzeneMessageClient>(),
    serviceResolver: serviceResolver);

var result = await client.SendMessageAsync<OrderCreatedMessage, Benzene.Abstractions.Results.Void>(
    new BenzeneClientRequest<OrderCreatedMessage>(
        topic: "order:created",
        message: new OrderCreatedMessage { OrderId = order.Id, Total = order.Total },
        headers: new Dictionary<string, string>()));
```

`SnsContextConverter` (used internally by the client) puts your `topic` onto the SNS message's `topic` message attribute — this is what each subscriber's `SnsMessageTopicGetter` reads to route to the right handler on the way in.

**Registering it for DI**, since there's no `CreateSnsBenzeneMessageClient` sugar, use `ClientsBuilder.WithMessageClient` (the same building block the SQS/Lambda sugar methods use internally) directly:

```csharp
services.UsingBenzene(x => x
    .AddBenzeneMessageClients(clients => clients
        .WithMessageClient("order-events", resolver =>
            new SnsBenzeneMessageClient(
                configuration["ORDER_EVENTS_TOPIC_ARN"],
                resolver.GetService<IAmazonSimpleNotificationService>(),
                resolver.GetService<ILogger<SnsBenzeneMessageClient>>(),
                resolver))));
```

or, if you only ever publish to this one topic, the simpler `SingleClientsBuilder` via `AddBenzeneMessageClient`:

```csharp
services.UsingBenzene(x => x
    .AddBenzeneMessageClient(clients => clients
        .WithMessageClient(resolver =>
            new SnsBenzeneMessageClient(
                configuration["ORDER_EVENTS_TOPIC_ARN"],
                resolver.GetService<IAmazonSimpleNotificationService>(),
                resolver.GetService<ILogger<SnsBenzeneMessageClient>>(),
                resolver))));
```

### 2. Give each consuming Lambda its own SNS-triggered pipeline

Each subscriber is a completely separate Lambda function/deployment. Wire `UseSns` into its middleware pipeline the same way `examples/Aws/Benzene.Examples.Aws/StartUp.cs` does:

**Lambda 1 — sends the customer a notification email:**

```csharp
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Sns;
using Benzene.Core.MessageHandlers;
using Benzene.FluentValidation;

public class NotificationStartUp : AwsLambdaStartUp
{
    public override IConfiguration GetConfiguration() => /* ... */;

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(SendOrderConfirmationEmailHandler).Assembly));
    }

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app.UseSns(snsApp => snsApp
            .UseMessageHandlers(router => router.UseFluentValidation()));
    }
}

[Message("order:created")]
public class SendOrderConfirmationEmailHandler : IMessageHandler<OrderCreatedMessage, Void>
{
    public async Task<IBenzeneResult<Void>> HandleAsync(OrderCreatedMessage request)
    {
        await _emailService.SendOrderConfirmationAsync(request.OrderId);
        return BenzeneResult.Accepted<Void>();
    }
}
```

**Lambda 2 — a completely separate deployable, updates analytics from the same event:**

```csharp
public class AnalyticsStartUp : AwsLambdaStartUp
{
    // ...same shape, different assembly of handlers...

    public override void Configure(IMiddlewarePipelineBuilder<AwsEventStreamContext> app, IConfiguration configuration)
    {
        app.UseSns(snsApp => snsApp
            .UseMessageHandlers());
    }
}

[Message("order:created")]
public class RecordOrderAnalyticsHandler : IMessageHandler<OrderCreatedMessage, Void>
{
    public async Task<IBenzeneResult<Void>> HandleAsync(OrderCreatedMessage request)
    {
        await _analyticsClient.TrackAsync("order_created", request.OrderId);
        return BenzeneResult.Accepted<Void>();
    }
}
```

Both handlers key off the same `[Message("order:created")]` topic — because they live in separate Lambda functions with separate SNS subscriptions, each function receives its own independent copy of the message from SNS and runs it through its own pipeline. This is fan-out: the publisher sent one message; both Lambdas received and processed it, each doing something different.

A few things to know about `Benzene.Aws.Lambda.Sns` from reading `SnsLambdaHandler`/`SnsApplication` directly:
- **It's fire-and-forget.** `SnsApplication : MiddlewareMultiApplication<SNSEvent, SnsRecordContext>` has no return value — unlike SQS's `SqsApplication`, there is no `BatchItemFailures`-style partial-failure reporting for SNS. A handler exception here does not get individually reported back per-record the way it does for SQS.
- **Routing is by the `topic` message attribute**, extracted by `SnsMessageTopicGetter` (`SnsUtils.GetFromAttributes(context, "topic")`) — set by whatever publishes to the topic (as shown in step 1).
- Each record in a batch runs through `TransportMiddlewarePipeline<SnsRecordContext>("sns", pipeline)`, so `ICurrentTransport` reports `"sns"` inside your handler if you need transport-specific behavior.

### 3. Subscribe both Lambdas to the same SNS topic

This is where Benzene's Terraform code generation (`Benzene.CodeGen.Terraform`) actually helps, and where you should be clear about what it does and doesn't cover.

`TerraformLambdaBuilder.BuildCodeFiles(TerraformLambdaSettings)` (see `docs/terraform.md`) generates the Lambda function and IAM role. If you also pass a `TopicsMap` (`IDictionary<string, string[]>` — SNS topic key to the list of Benzene message topics that Lambda cares about), it additionally generates the SNS-side wiring via `TerraformLambdaEventBusPermissionsBuilder`:

```csharp
using Benzene.CodeGen.Terraform;

var terraformBuilder = new TerraformLambdaBuilder();

var codeFiles = terraformBuilder.BuildCodeFiles(new TerraformLambdaSettings
{
    Name = "order-notification-func",
    EntryPoint = "OrderNotification::OrderNotification.NotificationStartUp::FunctionHandlerAsync",
    Domain = "orders",
    SubDomain = "notifications",
    TopicsMap = new Dictionary<string, string[]>
    {
        // key: the SNS topic (as named in your remote state); value: the Benzene message
        // topics this Lambda's subscription should be filtered to.
        { "order-events", new[] { "order:created" } }
    }
});

foreach (var file in codeFiles)
{
    File.WriteAllLines(file.Name, file.Lines);
}
```

This produces two additional files beyond `lambda.tf`/`iam_roles.tf`:

```hcl
# aws_lambda_permission.tf
resource "aws_lambda_permission" "order_events_invoke_order_notification_func" {
  action         = "lambda:InvokeFunction"
  function_name  = aws_lambda_function.order_notification_func.function_name
  principal      = "sns.amazonaws.com"
  statement_id   = "AllowSubscriptionToSNSResponse"
  source_arn     = data.terraform_remote_state.sns.outputs.order_events
}

# aws_sns_topic_subscription.tf
resource "aws_sns_topic_subscription" "order_notification_func_order_events_subscription" {
  topic_arn              = data.terraform_remote_state.sns.outputs.order_events
  protocol               = "lambda"
  endpoint               = aws_lambda_function.order_notification_func.arn
  endpoint_auto_confirms = true
  filter_policy          = jsonencode({"topic" = ["order:created"]})
}
```

Run the same generation for the analytics Lambda (a different `TerraformLambdaSettings.Name`, same `"order-events"` topic key) and you get a second, independent `aws_sns_topic_subscription` pointing at the same topic ARN — that's the fan-out: two subscriptions, one topic, each Lambda auto-confirmed and permissioned independently. The `filter_policy` here is generated from your `TopicsMap` values and uses SNS message-attribute filtering on `topic`, so a Lambda only receives the message-topics it declared, even if other unrelated Benzene message topics are published to the same SNS topic ARN.

What this generator does **not** do: it doesn't create the `aws_sns_topic` itself (`data.terraform_remote_state.sns` implies the topic is managed elsewhere), and — as covered in [Handling SQS Message Failures](handling-sqs-failures.md) — it generates no SQS resources, so if you want an SQS buffer in front of a subscriber instead of a direct Lambda subscription, that's entirely hand-written Terraform (`protocol = "sqs"` instead of `"lambda"`, plus your own queue and SQS-triggered Lambda).

## Testing

`test/Benzene.Core.Test/Aws/Sns/SnsMessagePipelineTest.cs` is the reference for exercising the subscriber side without a live topic — it builds an `SNSEvent` with `MessageBuilder.Create(topic, payload).AsSns()` and runs it through `SnsApplication.HandleAsync`:

```csharp
var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
    .ConfigureServices(services => services
        .ConfigureServiceCollection()
        .UsingBenzene(x => x.AddSns()))
    .Configure(app => app
        .OnResponse("Check Response", context => { messageResult = context.MessageResult; })
        .UseMessageHandlers())
    .Build(x => new SnsApplication(x));

var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSns();
await host.SendAsync(request);

Assert.True(messageResult.IsSuccessful);
```

For the publisher side, `test/Benzene.Core.Test/Clients/Aws/Sqs/SqsBenzeneMessageClientTest.cs` shows the equivalent pattern for `SqsBenzeneMessageClient` (mock `IAmazonSQS`, assert on `SendMessageAsync` being called with the expected `MessageAttributes["topic"]`); the same shape applies to `SnsBenzeneMessageClient` with a mocked `IAmazonSimpleNotificationService`.

## Troubleshooting

### A new Lambda subscription never receives anything

Check the `filter_policy` on its `aws_sns_topic_subscription` — if it's filtering on `topic` and the publisher didn't set that message attribute (or set a different value), SNS drops the message for that subscription silently; it's not a Benzene-level failure at all.

### A handler exception in one subscriber doesn't show up anywhere useful

Unlike SQS, `SnsApplication` has no partial-batch-failure reporting — check your Lambda's own logs/CloudWatch, and note that SNS itself has a separate, subscription-level retry policy (`RedrivePolicy` on the `aws_sns_topic_subscription`, distinct from an SQS queue's redrive policy) if you need SNS to retry delivery to a misbehaving Lambda endpoint.

### Subscription stuck in "pending confirmation"

`endpoint_auto_confirms = true` in the generated `aws_sns_topic_subscription` relies on AWS auto-confirming `protocol = "lambda"` subscriptions; this doesn't apply if you hand-write a subscription with a different protocol (e.g. `https`), which needs an explicit confirmation handshake that Benzene does not handle.

## Variations

### SNS-to-SQS fan-out for durability

If a subscriber needs message durability/backpressure (rather than raw SNS-to-Lambda, which has no queue in front of it), subscribe an SQS queue to the topic instead of the Lambda directly (`protocol = "sqs"`), then trigger that subscriber's Lambda from the queue with `Benzene.Aws.Lambda.Sqs` as covered in [Handling SQS Message Failures](handling-sqs-failures.md). This gets you SQS's partial-batch-failure reporting and DLQ support for that specific subscriber, at the cost of an extra hop.

### Filtering multiple message topics through one SNS subscription

`TopicsMap`'s value is a `string[]`, so a single Lambda/subscription pair can filter on several Benzene message topics from the same SNS topic ARN — pass all the topics that Lambda's handlers care about and let the generated `filter_policy` do the routing before the message ever reaches your Lambda.

## Further Reading

- [Terraform Code Generation](../terraform.md) — `TerraformLambdaBuilder`/`TerraformLambdaSettings` in full
- [Handling SQS Message Failures](handling-sqs-failures.md) — partial batch failures and DLQs for SQS-based subscribers
- [Message Handlers](../message-handlers.md) — `[Message]` topic routing
- [Clients](../clients.md) — `IBenzeneMessageClient`, `ClientsBuilder`, and the client decorator chain
- [AWS SNS message filtering](https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html)
