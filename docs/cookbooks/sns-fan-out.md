# SNS Fan-Out Pattern

Publish one event to an SNS topic and have several independent Lambda functions — each with their own SNS trigger and their own handlers — process a copy of it.

## Problem Statement

You have one event (e.g. "order created") that multiple, independently-deployed services need to react to: one Lambda sends a notification email, another updates analytics, a third reconciles inventory. Rather than the publisher knowing about every consumer, you publish once to an SNS topic and let each Lambda subscribe independently. This cookbook covers:

- Publishing a message to SNS with `Benzene.Clients.Aws.Sns`'s `SnsBenzeneMessageClient`
- Wiring an SNS-triggered Lambda to Benzene's message handler pipeline with `Benzene.Aws.Lambda.Sns`
- Subscribing multiple Lambda functions to the same SNS topic (including how much of this Benzene's Terraform code generator actually automates, and how much is plain AWS configuration)

> **A handler failure result is silently dropped by default.** If a subscriber's handler returns
> a non-exception failure (e.g. `BenzeneResult.ServiceUnavailable(...)`) instead of throwing,
> `SnsOptions.RaiseOnFailureStatus` defaults to `false` and the Lambda invocation reports success —
> SNS never retries that notification, and there's no DLQ for it either. See
> ["Configuring exception and retry behavior with `SnsOptions`"](#configuring-exception-and-retry-behavior-with-snsoptions)
> below to opt into retry-on-failure, and note it requires an idempotent handler (SNS redelivery
> has no dedup of its own) — see [Capability Matrix](../capability-matrix.md) and
> [Idempotency](idempotency.md).

## Prerequisites

- An SNS topic (or the ARN of one you'll create alongside your Lambdas)
- One Benzene Lambda function per consumer, each independently deployable

## Installation

Publisher (whichever service raises the event):

```bash
dotnet add package Benzene.Clients.Aws.Sns
```

Each subscriber Lambda:

```bash
dotnet add package Benzene.Aws.Lambda.Sns
```

## Step-by-Step Implementation

### 1. Publish to SNS

Route the topic through `.UseSns(topicArn)` on an outbound route, then send via `IBenzeneMessageSender` — see [Clients — SNS](../clients.md#sns) for the full reference:

```csharp
services.UsingBenzene(x => x
    .AddOutboundRouting(routing => routing
        .Route("order:created", pipeline => pipeline.UseSns(configuration["ORDER_EVENTS_TOPIC_ARN"]))));
```

```csharp
public class OrderPublisher
{
    private readonly IBenzeneMessageSender _sender;

    public OrderPublisher(IBenzeneMessageSender sender)
    {
        _sender = sender;
    }

    public Task<IBenzeneResult<Void>> PublishOrderCreatedAsync(Order order)
    {
        return _sender.SendAsync<OrderCreatedMessage, Void>(
            "order:created",
            new OrderCreatedMessage { OrderId = order.Id, Total = order.Total });
    }
}
```

`OutboundSnsContextConverter` (used internally by `.UseSns(...)`) puts your topic onto the SNS message's `topic` message attribute — this is what each subscriber's `SnsMessageTopicGetter` reads to route to the right handler on the way in. **SNS has no request/response semantics beyond a send acknowledgement**, so `order:created` must be sent via `SendAsync<TRequest, Void>` as above — see [Clients — SNS](../clients.md#sns) for the `Void`-only constraint.

### 2. Give each consuming Lambda its own SNS-triggered pipeline

Each subscriber is a completely separate Lambda function/deployment. Wire `UseSns` into its middleware pipeline the same way `examples/Aws/Benzene.Examples.Aws/StartUp.cs` does:

**Lambda 1 — sends the customer a notification email:**

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Sns;
using Benzene.Core.MessageHandlers;
using Benzene.FluentValidation;
using Benzene.Microsoft.Dependencies;

public class NotificationStartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => /* ... */;

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers(typeof(SendOrderConfirmationEmailHandler).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseSns(snsApp => snsApp
                .UseMessageHandlers(router => router.UseFluentValidation())));
    }
}

public class NotificationFunction : AwsLambdaHost<NotificationStartUp> { }

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
public class AnalyticsStartUp : BenzeneStartUp
{
    // ...same shape, different assembly of handlers...

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseAwsLambda(eventPipeline => eventPipeline
            .UseSns(snsApp => snsApp
                .UseMessageHandlers()));
    }
}

public class AnalyticsFunction : AwsLambdaHost<AnalyticsStartUp> { }

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
- **It's fire-and-forget.** `SnsApplication.HandleAsync` has no return value — unlike SQS's `SqsApplication`, there is no `BatchItemFailures`-style partial-failure reporting for SNS (SNS-to-Lambda doesn't have an equivalent AWS mechanism). Whether a handler's exception or failure result surfaces to SNS at all is governed by `SnsOptions` — see below.
- **Routing is by the `topic` message attribute**, extracted by `SnsMessageTopicGetter` (`SnsUtils.GetFromAttributes(context, "topic")`) — set by whatever publishes to the topic (as shown in step 1).
- Each record in a batch runs through `TransportMiddlewarePipeline<SnsRecordContext>("sns", pipeline)`, so `ICurrentTransport` reports `"sns"` inside your handler if you need transport-specific behavior.

### Configuring exception and retry behavior with `SnsOptions`

By default, a handler exception cascades out of the Lambda invocation entirely (Lambda reports the invocation as failed, so SNS's own subscription retry policy applies), while a handler that returns a non-exception failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`) is silently accepted — the invocation reports success, and SNS never retries it. This is the AWS best-practice default: genuine exceptions usually mean something transient and worth retrying, while a deliberate failure status usually means a permanent/business-logic failure that retrying won't fix.

Both halves of that behavior are independently configurable via a second, optional argument to `UseSns`:

```csharp
using Benzene.Aws.Lambda.Sns;

app.UseAwsLambda(eventPipeline => eventPipeline
    .UseSns(snsApp => snsApp
            .UseMessageHandlers(),
        options =>
        {
            // Catch handler exceptions instead of letting them fail the invocation (no SNS retry).
            options.CatchExceptions = true;
            // Escalate a non-exception failure result into a thrown exception too, so SNS retries it
            // the same way it would an unhandled exception.
            options.RaiseOnFailureStatus = true;
        }));
```

Both default to `false`, reproducing today's implicit behavior exactly — this is purely opt-in. `RaiseOnFailureStatus` and `CatchExceptions` compose: if both are `true`, a failure result is escalated into an `SnsMessageProcessingException` and then immediately caught and logged (no SNS retry either) — useful if you want failure results logged consistently with exceptions but still don't want SNS retrying business failures.

### 3. Subscribe both Lambdas to the same SNS topic

This is where Benzene's Terraform code generation (`Benzene.CodeGen.Terraform`) actually helps, and where you should be clear about what it does and doesn't cover.

`TerraformLambdaBuilder.BuildCodeFiles(TerraformLambdaSettings)` (see `docs/terraform.md`) generates the Lambda function and IAM role. If you also pass a `TopicsMap` (`IDictionary<string, string[]>` — SNS topic key to the list of Benzene message topics that Lambda cares about), it additionally generates the SNS-side wiring via `TerraformLambdaEventBusPermissionsBuilder`:

```csharp
using Benzene.CodeGen.Terraform;

var terraformBuilder = new TerraformLambdaBuilder();

var codeFiles = terraformBuilder.BuildCodeFiles(new TerraformLambdaSettings
{
    Name = "order-notification-func",
    EntryPoint = "OrderNotification::OrderNotification.NotificationFunction::FunctionHandlerAsync",
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

Unlike SQS, `SnsApplication` has no partial-batch-failure reporting — check your Lambda's own logs/CloudWatch, and note that SNS itself has a separate, subscription-level retry policy (`RedrivePolicy` on the `aws_sns_topic_subscription`, distinct from an SQS queue's redrive policy) if you need SNS to retry delivery to a misbehaving Lambda endpoint. If you've set `SnsOptions.CatchExceptions = true`, remember that means the exception is caught and logged but never reaches SNS at all — check `ILogger<SnsApplication>`'s output rather than expecting a retry.

### A failure result isn't triggering an SNS retry

This is the default: `SnsOptions.RaiseOnFailureStatus` defaults to `false`, so a handler returning a non-exception failure result is silently accepted by Lambda and SNS never retries it. Set `RaiseOnFailureStatus = true` if you want failure results treated the same as exceptions for retry purposes.

### Subscription stuck in "pending confirmation"

`endpoint_auto_confirms = true` in the generated `aws_sns_topic_subscription` relies on AWS auto-confirming `protocol = "lambda"` subscriptions; this doesn't apply if you hand-write a subscription with a different protocol (e.g. `https`), which needs an explicit confirmation handshake that Benzene does not handle.

## Variations

### SNS-to-SQS fan-out for durability

If a subscriber needs message durability/backpressure (rather than raw SNS-to-Lambda, which has no queue in front of it), subscribe an SQS queue to the topic instead of the Lambda directly (`protocol = "sqs"`), then trigger that subscriber's Lambda from the queue with `Benzene.Aws.Lambda.Sqs` as covered in [Handling SQS Message Failures](handling-sqs-failures.md). This gets you SQS's partial-batch-failure reporting and DLQ support for that specific subscriber, at the cost of an extra hop.

### Filtering multiple message topics through one SNS subscription

`TopicsMap`'s value is a `string[]`, so a single Lambda/subscription pair can filter on several Benzene message topics from the same SNS topic ARN — pass all the topics that Lambda's handlers care about and let the generated `filter_policy` do the routing before the message ever reaches your Lambda.

### Swallow exceptions entirely for a best-effort subscriber

If a subscriber is genuinely optional (e.g. it only updates a non-critical dashboard) and you'd rather log-and-move-on than have SNS retry a misbehaving downstream repeatedly, set `SnsOptions.CatchExceptions = true` with `RaiseOnFailureStatus` left at its `false` default. Every failure - thrown or returned - is then logged via `ILogger<SnsApplication>` and never propagates, so SNS always sees the invocation as successful.

## Further Reading

- [Terraform Code Generation](../terraform.md) — `TerraformLambdaBuilder`/`TerraformLambdaSettings` in full
- [Handling SQS Message Failures](handling-sqs-failures.md) — partial batch failures and DLQs for SQS-based subscribers
- [Message Handlers](../message-handlers.md) — `[Message]` topic routing
- [Clients](../clients.md) — `IBenzeneMessageSender`, `AddOutboundRouting(...)`, and outbound middleware
- [AWS SNS message filtering](https://docs.aws.amazon.com/sns/latest/dg/sns-message-filtering.html)
