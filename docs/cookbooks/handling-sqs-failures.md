# Handling SQS Message Failures

Report per-message failures back to SQS so only the messages that actually failed are retried, retry transient errors in-process before that, and route permanently-failing messages to a dead-letter queue.

## Problem Statement

You're processing SQS messages through a Benzene Lambda function and need to:
- Retry a message a few times in-process when a downstream dependency is flaky, without retrying the whole batch
- Make sure only the messages that genuinely failed are redelivered by SQS — not every message in the batch
- Let messages that fail permanently fall through to a dead-letter queue (DLQ) instead of retrying forever

This cookbook covers what Benzene actually does for you (partial-batch-failure reporting, in-process retry middleware) and where Benzene's responsibility ends (DLQ configuration is AWS queue/infrastructure configuration, not something Benzene generates or wires up).

## Prerequisites

- A Benzene AWS Lambda function using `Benzene.Aws.Lambda.Sqs` to process an SQS-triggered event
- The SQS event source mapping configured with `ReportBatchItemFailures` (see [Troubleshooting](#troubleshooting) — without this, everything in this cookbook is silently ignored by AWS)
- An SQS queue with a redrive policy pointing at a dead-letter queue, if you want permanently-failed messages to end up in a DLQ

## Installation

```bash
dotnet add package Benzene.Aws.Lambda.Sqs
dotnet add package Benzene.Resilience --prerelease
```

## How Benzene Reports Partial Batch Failures

This is the mechanism the rest of this cookbook builds on, so it's worth understanding exactly what `Benzene.Aws.Lambda.Sqs` does before writing any handler code.

`SqsLambdaHandler` routes an incoming `SQSEvent` to `SqsApplication`, which is the class that actually implements partial batch failure reporting. For every record in the batch it runs your middleware pipeline in its own service scope, and it decides whether that record failed in exactly two ways:

```csharp
// src/Benzene.Aws.Lambda.Sqs/SqsApplication.cs (excerpt)
try
{
    using (var scope = serviceResolverFactory.CreateScope())
    {
        var setCurrentTransport = scope.GetService<ISetCurrentTransport>();
        setCurrentTransport.SetTransport("sqs");
        await _pipeline.HandleAsync(context, scope);
    }

    if (context.IsSuccessful.HasValue && !context.IsSuccessful.Value)
    {
        batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId });
    }
}
catch (Exception ex)
{
    // ...logs the exception...
    batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId });
}
```

Two ways a message ends up in `BatchItemFailures`:

1. **Your handler returns a failure result and no exception is thrown.** `SqsMessageMessageHandlerResultSetter` copies your message handler's `IBenzeneResult.IsSuccessful` onto `SqsMessageContext.IsSuccessful` after the pipeline runs. Any `BenzeneResult` factory method built from an error array (`ServiceUnavailable`, `BadRequest`, `ValidationError`, `NotFound`, etc.) sets `IsSuccessful` to `false`; `Ok`, `Accepted`, `Created`, etc. set it to `true`.
2. **The pipeline throws.** `SqsApplication` catches the exception, logs it via `ILogger<SqsApplication>` (message: `"Processing SQS message {messageId} failed"`), and reports the same message ID as failed.

Either way, only the specific `MessageId`s that failed go into `SQSBatchResponse.BatchItemFailures` — the records that succeeded are never reported, so **AWS will not redeliver them**. This is what makes it a *partial* batch failure: a 10-message batch where 1 message fails only causes that 1 message to become visible again on the queue.

This is native AWS Lambda SQS event source mapping behavior (`ReportBatchItemFailures` / `functionResponseTypes`); Benzene's contribution is populating the response correctly per-record so you get it without writing the batch-reconciliation logic yourself.

## Step-by-Step Implementation

### 1. Write a handler that can fail

Message handlers processing SQS records look like any other Benzene message handler — `[Message("topic")]` plus `IMessageHandler<TRequest, TResponse>`. Whether the handler throws or returns a failure result, both are picked up by `SqsApplication` as shown above.

```csharp
using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace MyApp.Handlers;

public class OrderPaymentMessage
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}

[Message("order:payment-capture")]
public class CapturePaymentHandler : IMessageHandler<OrderPaymentMessage, Void>
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<CapturePaymentHandler> _logger;

    public CapturePaymentHandler(IPaymentGateway paymentGateway, ILogger<CapturePaymentHandler> logger)
    {
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<IBenzeneResult<Void>> HandleAsync(OrderPaymentMessage request)
    {
        try
        {
            await _paymentGateway.CaptureAsync(request.OrderId, request.Amount);
            return BenzeneResult.Accepted<Void>();
        }
        catch (PaymentGatewayUnavailableException ex)
        {
            // A transient dependency failure: report it as a failed result rather than
            // letting the exception propagate, so it doesn't get logged as an unhandled
            // pipeline exception on every attempt.
            _logger.LogWarning(ex, "Payment gateway unavailable for order {OrderId}", request.OrderId);
            return BenzeneResult.ServiceUnavailable<Void>("Payment gateway unavailable");
        }
        // Any other exception type is left to propagate — SqsApplication's catch block
        // will log it and report the batch item failure for you.
    }
}
```

Both paths — the caught `ServiceUnavailable` result and an uncaught exception — leave `SqsMessageContext.IsSuccessful` as `false` (or unset, in the case of an exception, since `SqsApplication`'s `catch` block reports the failure directly without needing `IsSuccessful` at all). Either way the message ID ends up in `BatchItemFailures`.

### 2. Add in-process retry with `UseRetry`

`Benzene.Resilience` provides `RetryMiddleware<TContext>`, wired into any pipeline via the generic `UseRetry<TContext>()` extension. Read its actual signature directly — it's a single middleware with exponential backoff, nothing more:

```csharp
// src/Benzene.Resilience/Extensions.cs
public static IMiddlewarePipelineBuilder<TContext> UseRetry<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app,
    int numberOfRetries = 3,
    TimeSpan? initialDelay = null,       // defaults to 200ms
    double backoffFactor = 2.0,
    Func<Exception, bool>? shouldRetry = null,          // defaults to "retry everything except OperationCanceledException"
    Func<TContext, bool>? shouldRetryContext = null,     // defaults to "never retry a non-throwing result"
    Func<TimeSpan, Task>? delay = null)                  // defaults to Task.Delay; override in tests
```

Important: by default, `RetryMiddleware` only retries when the pipeline **throws**. It does *not* retry a handler that returns a failure result (like `ServiceUnavailable`) without throwing — `shouldRetryContext` defaults to always returning `false`. If you want the `CapturePaymentHandler` example above to be retried in-process (rather than only relying on SQS's own redelivery), pass a `shouldRetryContext` that inspects `SqsMessageContext.IsSuccessful`:

```csharp
using Benzene.Resilience;

app.UseSqs(sqs => sqs
    .UseRetry<SqsMessageContext>(
        numberOfRetries: 3,
        initialDelay: TimeSpan.FromMilliseconds(200),
        backoffFactor: 2.0,
        shouldRetryContext: context => context.IsSuccessful == false)
    .UseMessageHandlers(router => router.UseFluentValidation())
);
```

`UseRetry` must wrap `UseMessageHandlers` (placed before it in the pipeline builder chain) so that it retries the handler invocation, not just downstream middleware.

With this wiring:
- A thrown exception from `CapturePaymentHandler` is retried up to 3 times (200ms, then 400ms, then 800ms delay) before being allowed to propagate out of the pipeline, where `SqsApplication` catches it, logs it, and reports the batch item failure.
- A `ServiceUnavailable` result is retried up to 3 times in-process (via `shouldRetryContext`) and, if still unsuccessful after the last attempt, `RetryMiddleware` returns normally (it does not throw for context-based failures) — `SqsApplication` then sees `context.IsSuccessful == false` and reports the batch item failure exactly as if there were no retry middleware at all.

There is no circuit breaker, timeout, or bulkhead middleware in `Benzene.Resilience` today — only this single retry middleware. If you need those patterns, you'll need to bring your own (e.g. wrap `IPaymentGateway` with Polly directly) since Benzene does not currently ship them, despite what `src/Benzene.Resilience/CLAUDE.md` claims.

### 3. Let genuinely-failed messages flow to SQS, then to a DLQ

Once `UseRetry` gives up (or a non-retryable exception is thrown), `SqsApplication` reports that message's ID in `BatchItemFailures`. From here, everything is native SQS behavior, not Benzene code:

- AWS Lambda's SQS event source mapping reads `BatchItemFailures` and makes **only those messages** visible again on the queue (instead of the whole batch), once their visibility timeout expires.
- If the source queue has a redrive policy, SQS tracks each message's approximate receive count. Once a message has been received (and failed) `maxReceiveCount` times, SQS moves it off the source queue and onto the configured dead-letter queue automatically.

**Benzene does not configure any of this.** `Benzene.CodeGen.Terraform` (`src/Benzene.CodeGen.Terraform/`) only generates `aws_lambda_function`, `aws_iam_role`, and — when an SNS topic map is supplied — `aws_lambda_permission`/`aws_sns_topic_subscription` resources (see `TerraformLambdaBuilder` and `TerraformLambdaEventBusPermissionsBuilder`). It has no SQS queue, redrive policy, or DLQ resource generation of any kind. DLQ setup is entirely your responsibility as AWS infrastructure. A hand-written Terraform example:

```hcl
resource "aws_sqs_queue" "orders_dlq" {
  name                      = "orders-dlq"
  message_retention_seconds = 1209600 # 14 days
}

resource "aws_sqs_queue" "orders" {
  name = "orders"

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.orders_dlq.arn
    maxReceiveCount      = 5
  })
}

resource "aws_lambda_event_source_mapping" "orders" {
  event_source_arn = aws_sqs_queue.orders.arn
  function_name     = aws_lambda_function.orders_processor.arn
  batch_size        = 10

  # Required for BatchItemFailures to have any effect. Without this, AWS ignores the
  # response entirely and either the whole batch succeeds or the whole batch is retried.
  function_response_types = ["ReportBatchItemFailures"]
}
```

`maxReceiveCount: 5` combined with `UseRetry(numberOfRetries: 3)` means a message can be attempted up to 6 times total in-process retries per Lambda invocation, times up to 5 separate SQS deliveries — tune both numbers together rather than independently.

## Testing

`Benzene.Aws.Lambda.Sqs`'s own test suite (`test/Benzene.Core.Test/Aws/Sqs/`) is the best reference for exercising this without a real queue. The pattern:

```csharp
[Fact]
public async Task HandleAsync_PipelineThrows_LogsExceptionAndReportsBatchFailure()
{
    var mockPipeline = new Mock<IMiddlewarePipeline<SqsMessageContext>>();
    mockPipeline
        .Setup(x => x.HandleAsync(It.IsAny<SqsMessageContext>(), It.IsAny<IServiceResolver>()))
        .ThrowsAsync(new InvalidOperationException("boom"));

    var application = new SqsApplication(mockPipeline.Object);

    var sqsEvent = new SQSEvent
    {
        Records = [new SQSEvent.SQSMessage { MessageId = "some-message-id" }]
    };

    var response = await application.HandleAsync(sqsEvent, mockResolverFactory.Object);

    Assert.Single(response.BatchItemFailures);
    Assert.Equal("some-message-id", response.BatchItemFailures[0].ItemIdentifier);
}
```

Adapt this for your own handler: build an `SQSEvent` with `MessageBuilder.Create(topic, payload).AsSqs()` (see `test/Benzene.Core.Test/Aws/Sqs/SqsMessagePipelineTest.cs`), run it through `SqsApplication.HandleAsync`, and assert on `SQSBatchResponse.BatchItemFailures`. To test `UseRetry` deterministically, pass a `delay: _ => Task.CompletedTask` override (see `test/Benzene.Core.Test/Resilience/RetryMiddlewareTest.cs`) so tests don't actually wait out the exponential backoff.

## Troubleshooting

### Messages are retried even though only one failed in the batch

Your event source mapping is missing `function_response_types = ["ReportBatchItemFailures"]` (Terraform) or the equivalent `FunctionResponseTypes: ["ReportBatchItemFailures"]` in CloudFormation/console config. Without it, AWS Lambda ignores `SQSBatchResponse.BatchItemFailures` entirely and treats the invocation as fully succeeded (if it returned without throwing) or fully failed (if the Lambda invocation itself threw) — regardless of what Benzene reports per-message.

### A handler that returns `ServiceUnavailable` isn't being retried

By default `RetryMiddleware`'s `shouldRetryContext` always returns `false` — it only retries thrown exceptions unless you explicitly pass `shouldRetryContext: context => context.IsSuccessful == false` (or your own predicate). This is intentional: Benzene has no way to know whether a non-throwing "failure" result should be retried without you telling it.

### Messages never reach the DLQ

Check the source queue's `redrive_policy` / `RedrivePolicy` — `maxReceiveCount` controls how many *deliveries* (not Benzene retries) a message survives before SQS moves it to the DLQ. If `UseRetry` masks every failure by eventually succeeding, the message never gets reported as failed, so SQS never redelivers it, and it never reaches `maxReceiveCount`.

## Variations

### Skip in-process retry, rely purely on SQS redelivery

If a failure is expensive to retry immediately (e.g. a downstream system that needs a full visibility-timeout's worth of backoff), don't add `UseRetry` at all — just let the handler fail fast, report the batch item failure, and let SQS's own visibility timeout and redrive policy handle the retry cadence. This is cheaper (no extra Lambda duration spent retrying) and lets you tune backoff purely at the queue level.

### Only retry specific exception types

Pass a narrower `shouldRetry` predicate so unrelated bugs (e.g. a `NullReferenceException` from bad code) fail immediately instead of being retried three times first:

```csharp
.UseRetry<SqsMessageContext>(
    numberOfRetries: 3,
    shouldRetry: ex => ex is HttpRequestException or TimeoutException)
```

## Further Reading

- [Resilience](../resilience.md) — full `UseRetry`/`RetryMiddleware<TContext>` configuration reference, including why it is not Polly-backed and has no circuit breaker/timeout/bulkhead support
- [Message Handlers](../message-handlers.md) — `[Message]` attribute and `IMessageHandler<TRequest, TResponse>`
- [Middleware](../middleware.md) — middleware pipeline ordering
- [SNS Fan-Out Pattern](sns-fan-out.md) — broadcasting one event to multiple SQS/Lambda consumers
- [AWS Lambda event source mapping for SQS](https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html) — `ReportBatchItemFailures` and partial batch responses
- [AWS SQS dead-letter queues](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-dead-letter-queues.html)
