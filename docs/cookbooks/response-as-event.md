# Response as Event

Turn a request/response handler's response payload into a follow-up event — e.g. an SQS-triggered
`order:create` handler returns an `OrderCreated` payload, and the pipeline broadcasts it on
`order:created`.

## Problem Statement

On a queue transport there is no reply channel: whatever payload your handler returns is dropped
after the message is acknowledged. But that payload is often exactly the event the rest of your
system wants — "the order *was* created, here it is." Publishing it by hand from inside the
handler works, but couples the handler to messaging concerns and repeats the same boilerplate in
every handler.

`UseResponseEvents` (in `Benzene.ResponseEvents`) does this declaratively: the handler stays a pure
request/response handler (reusable on HTTP, where the same payload *is* the response body), and
the pipeline decides that on this transport, the response becomes an event.

## Prerequisites

- A queue-triggered Benzene pipeline (SQS, Service Bus, Kafka, RabbitMQ, ...)
- An outbound route for each event topic (SNS, EventBridge, SQS, Kafka, ...)

## Installation

```bash
dotnet add package Benzene.ResponseEvents
```

## Step-by-Step Implementation

### 1. Keep the handler a plain request/response handler

```csharp
[Message("order:create")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, OrderCreated>
{
    private readonly IOrderRepository _orders;

    public CreateOrderHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IBenzeneResult<OrderCreated>> HandleAsync(CreateOrderRequest request)
    {
        var order = await _orders.CreateAsync(request);
        return BenzeneResult.Created(new OrderCreated(order.Id, order.Total));
    }
}
```

### 2. Route the event topic outbound

The published event goes through the normal [outbound routing](../clients.md), so it gets startup
validation, retry, and correlation/trace header stamping like any other send:

```csharp
services.UsingBenzene(x => x
    .AddOutboundRouting(routing => routing
        .Route("order:created", pipeline => pipeline
            .UseCorrelationId()
            .UseSns(configuration["ORDER_EVENTS_TOPIC_ARN"]))));
```

### 3. Map the response to the event, per pipeline

```csharp
app.UseSqs(sqs => sqs
    .UseMessageHandlers(router => router
        .UseResponseEvents(events => events
            .Map("order:create", "order:created"))));
```

That's it. After `CreateOrderHandler` returns a successful result with a payload, the payload is
published on `order:created`. On any other pipeline hosting the same handler (say, HTTP) nothing
changes — the mapping belongs to the SQS pipeline only.

## Configuration Options

### Conditional publishing

By default a mapping fires on any successful result that carries a payload. Pass `when:` to
narrow it:

```csharp
.UseResponseEvents(events => events
    .Map("order:create", "order:created",
         when: result => result.Status == BenzeneResultStatus.Created))
```

### Declaring the payload type (specs) and reshaping the payload

`Map<TPayload>` declares the event's payload type — which also surfaces the event in generated
AsyncAPI / event-service specs — and can project the response into a different event shape:

```csharp
.UseResponseEvents(events => events
    .Map<OrderCreated>("order:create", "order:created",
         project: order => new OrderCreatedNotification(order.Id)))
```

Returning `null` from `project` skips the publish for that message.

### CRUD naming convention

`MapCrudConvention()` adds one rule covering every CRUD topic on the pipeline:
`X:create`/`X:update`/`X:delete` handled with status `created`/`updated`/`deleted` publishes the
payload on `X:created`/`X:updated`/`X:deleted`:

```csharp
.UseResponseEvents(events => events.MapCrudConvention())
```

### Custom rules

Anything the built-ins can't express is an `IResponseEventMapping` implementation added via
`events.Add(new MyMapping())` — a mapping receives the topic and the handler's full result and
returns the publication (topic + payload) or `null`.

### Publish failure behavior

By default (`PublishFailureMode.FailMessage`) a failed publish replaces the handler's response
with an `unexpected-error`, so the transport reports the message as failed and the queue redelivers
it. **This is at-least-once delivery: the handler re-runs on redelivery, so the handler and the
event's consumers must be idempotent** (see [Idempotency](idempotency.md)). If the event is
best-effort, opt out:

```csharp
.UseResponseEvents(events => events
    .Map("order:create", "order:created")
    .OnPublishFailure(PublishFailureMode.LogAndContinue))
```

## Introspection

Every mapping is plain data. Resolve `IResponseEventCatalog` to see exactly what the service
republishes, across all pipelines:

```csharp
var catalog = resolver.GetService<IResponseEventCatalog>();
foreach (var mapping in catalog.Mappings)
{
    Console.WriteLine(mapping.Description);   // e.g. "order:create -> order:created (OrderCreated)"
}
```

Mappings declared with `Map<TPayload>` also appear in generated specs: the catalog is registered
as an `IMessageDefinitionFinder<IMessageDefinition>`, the seam spec builders read published-event
declarations from.

## Catching a forgotten mapping

A request/response handler that returns a payload but runs on a fire-and-forget transport with no
mapping silently drops that payload — the classic "I wrote `IMessageHandler<CreateOrder,
OrderCreated>`, deployed it on SQS, and my event vanished" mistake. An opt-in startup diagnostic
surfaces it. Call it once after wiring, e.g. against the resolved container:

```csharp
var gaps = serviceResolver.LogUnmappedResponseHandlers();   // logs a warning per gap, returns them
// or, to decide yourself:
foreach (var gap in serviceResolver.FindUnmappedResponseHandlers())
    Console.WriteLine(gap.Description);
```

Each `ResponseEventGap` names the handler, topic, and response type of a response-returning
handler no mapping covers. It's **advisory, never throws** — because a Benzene handler is
transport-agnostic, the same handler legitimately returns its response as the reply over HTTP and
(maybe) drops it over SQS, and registration alone can't tell which you meant. Treat the list as
"did I forget a mapping?": map the ones whose response should become an event, ignore any topic
served only over HTTP/gRPC. To gate CI, fail when the (filtered) list is non-empty.

## Swapping the publisher

The middleware publishes through the `IResponseEventPublisher` port; the default implementation
sends via `IBenzeneMessageSender`. Replace the scoped registration to publish differently — a test
fake, a custom fan-out, or an outbox relay:

```csharp
services.AddScoped<IResponseEventPublisher, MyOutboxPublisher>();
```

## Gotchas

- **Every event topic needs an outbound route.** An unrouted topic throws
  `UnroutedTopicException` at publish time, which surfaces per your `PublishFailureMode`.
  `ValidateOutboundRouting` catches missing routes at startup.
- **Event routes should be fire-and-forget transports** (SQS, SNS, EventBridge, Kafka...). A
  route that produces a typed response makes the default publisher's send throw a response-type
  mismatch; register a custom `IResponseEventPublisher` for such routes.
- **No-response handlers (`IMessageHandler<TRequest>`) never publish** — they produce an
  `accepted` result with no payload, and a mapping never fires without a payload.
- **This is not an outbox.** The publish happens after the handler's work, in the same
  invocation; a crash between the two can drop the event (or, with `FailMessage`, re-run the
  handler). If you need the event to commit atomically with the DB write, put an outbox behind
  `IResponseEventPublisher` — see [Transactional Outbox](transactional-outbox.md).

## Related

- [Transactional Outbox](transactional-outbox.md) — publish the event atomically with the DB write.
- [SNS Fan-Out Pattern](sns-fan-out.md) — publishing the same event to many consumers.
- [Idempotency](idempotency.md) — required for `FailMessage` redelivery semantics.
