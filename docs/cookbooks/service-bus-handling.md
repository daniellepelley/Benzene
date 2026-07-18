# Service Bus Message Handling

Process Azure Service Bus queue and topic/subscription messages with Benzene, using the same
`[Message]`/topic-routing model as HTTP and Kafka.

## Problem Statement

You're consuming messages from an Azure Service Bus queue (or a topic/subscription) and want to
route them to Benzene message handlers by topic, the same way HTTP requests and Kafka records are
routed, rather than hand-rolling per-message dispatch. Doing this well means understanding a few
things the [Azure Functions getting-started guide](../azure-functions.md) only introduces briefly:

- Where the "topic" used for handler routing actually comes from, since a Service Bus queue or
  topic/subscription is a routing *destination*, not a per-message topic field.
- How headers reach your handler, and what values get filtered out.
- What Benzene does when a handler fails: by default, still nothing tied to message completion
  (the Functions host auto-completes on its own), but real per-message complete/abandon control is
  now available if you opt in - see step 5.
- How to process a single message vs. a batch (`IsBatched = true`).

This cookbook works through a realistic handler and answers each of those honestly, citing the
actual source in `src/Benzene.Azure.Function.ServiceBus/`.

## Prerequisites

- An Azure Functions isolated-worker project already wired up per
  [Azure Functions Setup](../azure-functions.md), steps 1–5 (project, `BenzeneStartUp`,
  `Program.cs`).
- A Service Bus namespace with a queue (or topic/subscription), and a connection string or managed
  identity configured for the Function App.
- Familiarity with `[Message]`/message handler registration — see
  [Message Handlers](../message-handlers.md).

## Installation

```bash
dotnet add package Benzene.Azure.Function.ServiceBus --prerelease
```

This also needs `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus` referenced directly in
your function app project, same as the other Microsoft worker packages — see
[Azure Functions Setup, step 2](../azure-functions.md#2-install-the-nuget-packages).

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus --version 5.22.0
```

## Step-by-Step Implementation

### 1. The trigger function and pipeline

This is the same shape as the [Service Bus section](../azure-functions.md#service-bus) of the
getting-started guide:

```csharp
using Benzene.Azure.Function.ServiceBus;

app.UseServiceBus(serviceBus => serviceBus.UseMessageHandlers());
```

```csharp
using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Microsoft.Azure.Functions.Worker;

public class OrderQueueFunction
{
    private readonly IAzureFunctionApp _app;

    public OrderQueueFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("order-queue")]
    public Task Run([ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        return _app.HandleServiceBusMessages(message);
    }
}
```

### 2. Where the topic comes from

A Service Bus queue, or a topic/subscription pair, is a routing *destination* configured on the
trigger itself — it isn't a per-message field the way a Kafka record's topic is. Reading
`src/Benzene.Azure.Function.ServiceBus/ServiceBusMessageTopicGetter.cs`:

```csharp
public class ServiceBusMessageTopicGetter : IMessageTopicGetter<ServiceBusContext>
{
    public ITopic GetTopic(ServiceBusContext context)
    {
        return new Topic(GetTopicProperty(context));
    }

    private static string GetTopicProperty(ServiceBusContext context)
    {
        return context.Message.ApplicationProperties.TryGetValue("topic", out var value) ? value as string : null;
    }
}
```

Benzene reads the topic from a custom `"topic"` [application
property](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.applicationproperties)
on the message — the same convention `Benzene.Aws.Sqs` uses for SQS message attributes. Your
**sender** needs to set it explicitly:

```csharp
var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(new CreateOrderRequest { OrderId = "o-123" }))
{
    ApplicationProperties = { ["topic"] = "order:create" }
};
await sender.SendMessageAsync(message);
```

If the property is missing, `GetTopic` returns a topic whose `Id` is `Constants.Missing`
(`"<missing>"`) — `MessageRouter` (`src/Benzene.Core.MessageHandlers/MessageRouter.cs`) then
returns a validation-error result rather than routing anywhere; see
[Troubleshooting](#message-never-reaches-a-handler) below for what that looks like from the
outside.

### 3. Headers, and what gets filtered out

Unlike `Benzene.Azure.Function.Kafka` (whose headers getter always returns an empty dictionary),
Service Bus headers are real. `ServiceBusMessageHeadersGetter` exposes every **string-typed**
application property as a header:

```csharp
public IDictionary<string, string> GetHeaders(ServiceBusContext context)
{
    return context.Message.ApplicationProperties
        .Where(x => x.Value is string)
        .ToDictionary(x => x.Key, x => (string)x.Value);
}
```

`ApplicationProperties` is `IDictionary<string, object>` — Service Bus allows numeric, boolean, and
other primitive property values, not just strings. Only the string-typed ones make it into
Benzene's header dictionary; a numeric property like a retry count or a boolean flag is silently
excluded (not converted to a string) — read those directly off `context.Message.ApplicationProperties`
in your own middleware if you need them.

### 4. A realistic handler

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

[Message("order:create")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly IOrderStore _store;

    public CreateOrderHandler(IOrderStore store)
    {
        _store = store;
    }

    public async Task<IBenzeneResult<CreateOrderResponse>> HandleAsync(CreateOrderRequest request)
    {
        await _store.SaveAsync(request.OrderId);
        return BenzeneResult.Ok(new CreateOrderResponse { Accepted = true });
    }
}

public class CreateOrderRequest
{
    public string OrderId { get; set; }
}

public class CreateOrderResponse
{
    public bool Accepted { get; set; }
}
```

This is invoked through the same generic `.UseMessageHandlers()` topic-routing pipeline as HTTP and
Kafka — no envelope deserialization step, unlike `Benzene.Azure.Function.EventHub`.

### 5. Message completion: the default, and real per-message control

This is the part worth being precise about, since Service Bus (unlike Event Hubs) has native
dead-lettering, and it would be easy to assume Benzene always wires into it.
`ServiceBusMessageHandlerResultSetter` **does** record the outcome:

```csharp
public class ServiceBusMessageHandlerResultSetter : MessageHandlerResultSetterBase<ServiceBusContext>;
```

(it used to be a genuine no-op; it isn't anymore) - but recording the outcome onto
`ServiceBusContext.MessageResult` is not automatically the same as *acting* on it.

**By default** (`ServiceBusOptions.AckMode = ServiceBusAckMode.AutoComplete`, unchanged from before
this option existed), whatever your handler returns has no effect on the Service Bus message
itself — the Azure Functions Service Bus trigger completes the message automatically on its own
default settings once your trigger function returns without throwing.

**Set `AckMode = ServiceBusAckMode.Explicit`** for real per-message
`CompleteMessageAsync`/`AbandonMessageAsync` control tied to the handler's outcome. This needs two
things together:

1. Your `[ServiceBusTrigger]` attribute must set `AutoCompleteMessages = false` — a Functions-runtime
   setting Benzene can't set for you.
2. Your trigger function must bind `ServiceBusMessageActions` and call the overload that accepts it:

```csharp
using Microsoft.Azure.Functions.Worker;

app.UseServiceBus(serviceBus => serviceBus.UseMessageHandlers(),
    configure: options => options.AckMode = ServiceBusAckMode.Explicit);
```

```csharp
[Function("order-queue")]
public Task Run(
    [ServiceBusTrigger("orders", Connection = "ServiceBusConnection", AutoCompleteMessages = false)] ServiceBusReceivedMessage message,
    ServiceBusMessageActions messageActions)
{
    return _app.HandleServiceBusMessages(messageActions, message);
}
```

With this wired up: a handler that returns `Ok` (or any successful result) completes the message;
a handler that returns a non-exception failure result, or throws, abandons it (returned to the
queue, respecting the queue's own max-delivery-count before Service Bus's native auto-dead-letter
kicks in). The plain `HandleServiceBusMessages(IAzureFunctionApp, params
ServiceBusReceivedMessage[])` overload (no `ServiceBusMessageActions`) has nothing to act on, so
`AckMode = Explicit` has no effect through it even if set — you have to use the
`ServiceBusMessageActions`-accepting overload for explicit ack to actually happen.

`ServiceBusOptions.CatchExceptions`/`RaiseOnFailureStatus` still control something different -
whether a handler's exception/escalated failure cascades to fail the *whole trigger invocation*
(reported to the Functions host), independent of whether that one message gets completed or
abandoned. All four combinations of `AckMode` × `CatchExceptions` are independently valid; see
`ServiceBusFailureHandlingTest.cs` for the exact behavior of each.

Session handling (`ServiceBusSessionMessageActions`, ordered per-session processing) is still not
implemented — if you need it, you have to bridge that gap yourself, the same pattern used for
[Event Hub's poison-event handling](event-hub-processing.md#6-checkpointing-on-failure-and-why-benzene-doesnt-help-with-poison-events).

## Testing

Use `Benzene.Testing` and the Service Bus test helpers, exactly as
`test/Benzene.Core.Test/Azure/ServiceBusPipelineTest.cs` does:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus.TestHelpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

var mockOrderStore = new Mock<IOrderStore>();

var app = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockOrderStore.Object))
    .BuildAzureFunctionApp();

var request = MessageBuilder
    .Create("order:create", new CreateOrderRequest { OrderId = "o-123" })
    .AsAzureServiceBusMessage();

await app.HandleServiceBusMessages(request);

mockOrderStore.Verify(x => x.SaveAsync("o-123"));
```

`AsAzureServiceBusMessage()` (`src/Benzene.Azure.Function.ServiceBus.TestHelpers/MessageBuilderExtensions.cs`)
builds a real `ServiceBusReceivedMessage` via the SDK's `ServiceBusModelFactory` (which has no
public constructor for test code to call directly), setting the `"topic"` application property from
`MessageBuilder.Create`'s topic argument and copying any `.WithHeader(...)` calls onto
`ApplicationProperties` too, so header-dependent handlers are testable the same way.

`HandleServiceBusMessages` takes `params ServiceBusReceivedMessage[]`, so a batched trigger
(`IsBatched = true`) is testable by passing multiple messages:

```csharp
var batch = Enumerable.Range(0, 10)
    .Select(i => MessageBuilder.Create("order:create", new CreateOrderRequest { OrderId = $"o-{i}" }).AsAzureServiceBusMessage())
    .ToArray();

await app.HandleServiceBusMessages(batch);

mockOrderStore.Verify(x => x.SaveAsync(It.IsAny<string>()), Times.Exactly(10));
```

For pipeline-only tests without a full `StartUp`, `InlineAzureFunctionStartUp` works the same way as
in `azure-functions.md`'s testing section:

```csharp
var app = new InlineAzureFunctionStartUp()
    .ConfigureServices(services => services
        .UsingBenzene(x => x.AddMessageHandlers(typeof(CreateOrderHandler).Assembly))
        .AddSingleton(mockOrderStore.Object))
    .Configure(app => app
        .UseServiceBus(serviceBus => serviceBus
            .UseMessageHandlers()))
    .Build();
```

Testing `AckMode = ServiceBusAckMode.Explicit`'s complete/abandon behavior needs a
`ServiceBusMessageActions` test double - it's mockable directly with Moq (non-sealed, virtual
methods, a protected constructor Moq's proxy can call):

```csharp
using Microsoft.Azure.Functions.Worker;

var mockActions = new Mock<ServiceBusMessageActions>();
var message = MessageBuilder.Create("order:create", new CreateOrderRequest { OrderId = "o-123" }).AsAzureServiceBusMessage();

await app.HandleServiceBusMessages(mockActions.Object, message);

mockActions.Verify(x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()));
```

See `test/Benzene.Core.Test/Azure/ServiceBusFailureHandlingTest.cs` for the full set of
`AckMode`/`CatchExceptions`/`RaiseOnFailureStatus` combinations tested this way.

## Troubleshooting

### Message never reaches a handler

If the `"topic"` application property is missing or isn't a string, `ServiceBusMessageTopicGetter`
returns a topic with `Id == "<missing>"`. `MessageRouter` then returns a validation-error result
instead of dispatching to any handler — unlike Event Hub's silent-drop behavior for a malformed
envelope, this is at least a visible result. With the default `AckMode = AutoComplete` this result
has no effect on the underlying Service Bus message: the trigger still completes it on its own
default settings. With `AckMode = Explicit` (see [step 5](#5-message-completion-the-default-and-real-per-message-control)),
a validation-error result is a non-exception failure result, so the message is abandoned instead —
either way, confirm your sender is actually setting the `"topic"` property (step 2), since a
missing property is easy to miss when nothing about the send call itself fails.

### Handler runs but the message keeps redelivering, or never does

With the default `AckMode = AutoComplete`, this isn't a Benzene concern — completion/abandon/
redelivery/dead-lettering is entirely governed by the Service Bus trigger's own configuration
(`maxAutoLockRenewalDuration`, `maxConcurrentCalls`/`maxConcurrentSessions` in `host.json`) and is
completely disconnected from whatever your handler returns. If you need redelivery to depend on
handler success/failure, set `AckMode = ServiceBusAckMode.Explicit` (step 5) instead of bridging
`ServiceBusMessageActions` yourself. If you've already set `AckMode = Explicit` and redelivery still
looks wrong, confirm `AutoCompleteMessages = false` is actually set on the `[ServiceBusTrigger]`
attribute — without it, the Functions host completes the message before Benzene's explicit
completion ever runs, silently defeating `AckMode = Explicit`.

### NuGet can't find the Benzene package

Benzene packages are prerelease-only until 1.0 — `dotnet add package` needs `--prerelease` (or pin
an explicit `-alpha` version), same as every other Benzene package.

## Variations

### Batched triggers

Configure `[ServiceBusTrigger(..., IsBatched = true)]` and bind `ServiceBusReceivedMessage[]`
instead of a single message — `HandleServiceBusMessages` accepts both via its `params` array, no
code change needed beyond the trigger signature.

### Topics and subscriptions instead of a queue

`[ServiceBusTrigger("my-topic", "my-subscription", Connection = "ServiceBusConnection")]` works
identically from Benzene's perspective — a topic/subscription pair and a queue both hand
`ServiceBusApplication` a `ServiceBusReceivedMessage`/`ServiceBusReceivedMessage[]`; nothing in this
package distinguishes between the two.

### Consuming without Azure Functions (self-hosted worker)

Everything above assumes an Azure Functions Service Bus *trigger* — the runtime receives the
message and hands it to your function. If you'd rather consume Service Bus from a long-running
process you own (a console app, a container, an AKS pod, an App Service WebJob) with no Functions
runtime at all, use `Benzene.Azure.ServiceBus` instead of `Benzene.Azure.Function.ServiceBus`.

It's a self-hosted [worker](../getting-started-worker.md#azure-service-bus-benzeneazureservicebus):
`worker.UseServiceBus(config, clientFactory, sb => sb.UseMessageHandlers())` runs the SDK's
`ServiceBusProcessor` and dispatches each message through the *same* `[Message]`/topic-routing model
(the `"topic"` application property, exactly as in step 2). The differences from the trigger:

- **You own the process and the concurrency.** `MaxConcurrentCalls` on `BenzeneServiceBusConfig` is
  the processor's own cap; there's no `host.json` and no Functions scale controller.
- **Settlement is a first-class option, not a workaround.** `ServiceBusConsumerAckMode.AutoComplete`
  (default) mirrors the trigger's auto-complete; `Explicit` makes Benzene complete/abandon each
  message itself from the handler's outcome (including a non-exception failure result) — the
  self-hosted equivalent of the per-message control described in step 5, but without the
  `AutoCompleteMessages = false` trigger wiring.
- **You build the `ServiceBusClient`**, so authentication (connection string, managed identity, or
  the local emulator) is entirely yours.

See [Worker Service Setup, Part B](../getting-started-worker.md#part-b-built-in-workers-kafka-http-service-bus-event-hub-cosmos-db)
for the full host wiring.

## Further Reading

- [Azure Functions Setup](../azure-functions.md) — project setup, HTTP routing, and the
  Event Hub/Kafka/Service Bus trigger basics this cookbook builds on
- [Event Hub Stream Processing](event-hub-processing.md) — the analogous cookbook for Event Hubs,
  including a worked example of bridging handler failure to the runtime's retry/dead-letter behavior
- [Worker Service Setup](../getting-started-worker.md) — consuming Service Bus in-process, without
  Azure Functions, via `Benzene.Azure.ServiceBus` (see "Consuming without Azure Functions" above)
- [Message Handlers](../message-handlers.md) — `[Message]` and handler discovery
- [Monitoring & Diagnostics](../monitoring.md) — `AddDiagnostics()`, tracing every middleware in the
  Service Bus pipeline
- [Testing Benzene](../testing-benzene.md) — `BenzeneTestHost`, `InlineAzureFunctionStartUp`
- [Microsoft.Azure.Functions.Worker.Extensions.ServiceBus reference](https://learn.microsoft.com/azure/azure-functions/functions-bindings-service-bus) — the runtime-side trigger/binding configuration this cookbook references
