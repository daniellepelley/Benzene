# RabbitMQ Setup

`Benzene.RabbitMq` is a self-hosted RabbitMQ consumer worker and outbound publish client, built on
the [`RabbitMQ.Client`](https://www.nuget.org/packages/RabbitMQ.Client) v7 async API. It's Benzene's
first vendor-neutral, self-hosted broker — every other broker integration is a cloud vendor's
(SQS/SNS/Service Bus/Event Hubs/PubSub) plus Kafka — so it's the option for teams running on-prem, in
Kubernetes, or across clouds who don't want to couple to a vendor's queue.

It shares the same `[Message]`/message-handler programming model as every other transport, and slots
into the same self-hosted worker host as `Benzene.Kafka.Core` and `Benzene.Azure.ServiceBus` (see
[Unified Hosting Model](hosting.md)) — Benzene owns the process, unlike the Lambda/Functions triggers.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A RabbitMQ broker to develop against. The quickest is the official image:

  ```bash
  docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 \
    -e RABBITMQ_DEFAULT_USER=benzene -e RABBITMQ_DEFAULT_PASS=benzene \
    rabbitmq:4.0-management
  ```

  Port `5672` is AMQP (what the app connects to); `15672` is the management UI
  (`http://localhost:15672`, log in as `benzene`/`benzene`) for inspecting queues and messages.

  > **Why not the default `guest` user?** RabbitMQ's built-in `guest` account is restricted to
  > loopback connections *inside the container*, so a host→mapped-port login as `guest` is refused.
  > Any non-`guest` user (here `benzene`) isn't loopback-restricted — hence the `RABBITMQ_DEFAULT_*`
  > overrides above.

## Install the NuGet packages

```bash
dotnet add package Benzene.RabbitMq --prerelease
dotnet add package Benzene.HostedService --prerelease
dotnet add package Benzene.SelfHost --prerelease
```

`RabbitMQ.Client` (v7) comes in transitively from `Benzene.RabbitMq`.

## The worker assumes the queue exists

`Benzene.RabbitMq` does **not** declare exchanges, queues, or bindings — the worker consumes a queue
you've already declared, exactly as the Kafka worker assumes its topics exist. Declare your topology
out-of-band (the management UI, a definitions file, an `IaC` step, or a one-off
`channel.QueueDeclareAsync(...)` at startup). Dead-letter topology is likewise yours to set up (see
[Ack policy](#ack-policy-what-happens-on-failure) below).

## 1. Define a message handler

The consumer routes each delivery to a handler by matching the message's **topic** against the
handler's `[Message("...")]` value. The topic comes from the `topic` header, falling back to the AMQP
routing key when that header isn't present (so a message published by a Benzene client routes by
header, and a message from a non-Benzene producer routes by its natural routing key):

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;

[Message("orderCreated")]
public class OrderCreatedMessageHandler : IMessageHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message)
    {
        Console.WriteLine($"Order {message.OrderId} created");
        return Task.CompletedTask;
    }
}

public class OrderCreated
{
    public string OrderId { get; set; }
}
```

This is an `IMessageHandler<TRequest>` (no response type) — the right shape for a fire-and-forget
queue delivery. See [Message Handlers](message-handlers.md) for the request/response shape.

## 2. Define your StartUp

RabbitMQ consumption uses the same platform-neutral `BenzeneStartUp` as every other transport. The
`Benzene.RabbitMq.Extensions.UseRabbitMq` extension targets `IBenzeneWorkerStartup` — the
worker-specific builder that `UseWorker(...)` hands you — so you wire it up inside
`app.UseWorker(worker => worker.UseRabbitMq(...))`:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Benzene.RabbitMq;
using Benzene.SelfHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(OrderCreatedMessageHandler).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        var config = new RabbitMqConfig
        {
            QueueName = "orders",
            PrefetchCount = 10,
            ConcurrentRequests = 5,
        };

        var connectionFactory = new RabbitMqConnectionFactory(new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "benzene",
            Password = "benzene",
        });

        app.UseWorker(worker =>
            worker.UseRabbitMq(config, connectionFactory, rabbit => rabbit.UseMessageHandlers()));
    }
}
```

- `UseRabbitMq(...)` registers the topic/version/headers/body getters that adapt a RabbitMQ delivery
  to the message-handler pipeline (`AddRabbitMq()` is called for you) and adds the worker.
- `RabbitMqConnectionFactory` wraps a `ConnectionFactory` — you own how the connection is built (host,
  credentials, virtual host, TLS, automatic recovery, which is on by the SDK's default); the worker
  owns its channel and disposes both on stop. For a custom connection strategy, implement
  `IRabbitMqConnectionFactory` yourself.
- `PrefetchCount` (default 5) is the consumer QoS — the max unacknowledged deliveries the broker
  hands this consumer at once. Set it at or above `ConcurrentRequests` so every dispatcher lane can
  stay fed.
- `ConcurrentRequests` (default 5) bounds how many deliveries this worker handles concurrently.

## 3. Wire up `Program.cs`

`Benzene.HostedService`'s `UseBenzene<StartUp>()` registers the RabbitMQ worker as an
`IHostedService`, so it starts and stops with the host (a graceful stop cancels the consumer, drains
in-flight handlers, then closes the channel and connection):

```csharp
using Benzene.HostedService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseBenzene<StartUp>()
    .Build();

await host.RunAsync();
```

## Ack policy: what happens on failure

`RabbitMqConfig.AckMode` defaults to `RabbitMqAckMode.Explicit` — **safe by default**, unlike several
other transports (see the [Capability Matrix](capability-matrix.md#retry-on-handler-failure-result--the-per-transport-breakdown)).
RabbitMQ's first-class per-message ack makes this natural:

- **Handler success** → `BasicAck` (the message is removed from the queue).
- **Handler returns a failure `IBenzeneResult`, or throws** → `BasicNack`. Whether the message is
  requeued or dead-lettered depends on `RequeueOnFailure`:
  - `RequeueOnFailure = true` (default) → requeue, **bounded to one retry**: a first-attempt failure
    is requeued, but an already-redelivered failure is nacked *without* requeue (routed to the queue's
    dead-letter exchange if one is configured, else dropped) so a poison message can't hot-loop.
  - `RequeueOnFailure = false` → always nack without requeue → straight to the DLX (the production
    setting when you want a precise redelivery limit via a broker queue policy).

RabbitMQ's redelivered flag is a boolean, not a count, so the built-in bound is one retry; for a
higher, precise limit configure a dead-letter exchange with a queue policy on the broker.
`RabbitMqAckMode.AutoAck` is available for at-most-once, loss-tolerant workloads (the broker acks on
dispatch, before the handler runs). Because redelivery can reprocess a message, **handlers should be
idempotent** — see [Idempotency](cookbooks/idempotency.md).

## Producing messages

To publish from another Benzene service (rather than a raw `RabbitMQ.Client` channel), use
`RabbitMqBenzeneMessageClient` — an `IBenzeneMessageClient`, so your business logic depends only on
the transport-agnostic client/`IBenzeneMessageSender`:

```csharp
using Benzene.Clients;
using Benzene.RabbitMq.RabbitMqSendMessage;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

await using var connection = await connectionFactory.CreateConnectionAsync(CancellationToken.None);
await using var channel = await connection.CreateChannelAsync();

var client = new RabbitMqBenzeneMessageClient(channel,
    NullLogger<RabbitMqBenzeneMessageClient>.Instance, serviceResolver, exchange: "");

await client.SendMessageAsync<OrderCreated, Benzene.Abstractions.Results.Void>(
    "orderCreated", new OrderCreated { OrderId = "123" });
```

The request `Topic` becomes the AMQP **routing key** and is also carried as a `topic` **header**, so a
Benzene consumer routes by header (portable) with the routing key as the idiomatic fallback. The
Benzene header dictionary is forwarded onto `BasicProperties.Headers`, so correlation-ID / W3C
trace-context / payload-version headers reach the wire. With the default exchange (`exchange: ""`) the
routing key must equal the target queue name, so publishing topic `"orders"` lands in queue `"orders"`;
for topic/direct/fanout exchanges, pass the exchange name and bind your queues to it out-of-band.

Publishing is fire-and-forget by default — a completed publish maps to `Accepted`, a thrown publish to
`ServiceUnavailable`. (Publisher confirms for at-least-once publish are a planned opt-in.)

For the `OutboundRoutingBuilder` path (so call sites use `IBenzeneMessageSender.SendAsync`), the
`.UseRabbitMq<T>(exchange, ...)` / `.UseRabbitMqClient(channel)` pipeline extensions are the conversion
entry points, mirroring Kafka's `.UseKafka<T>(...)` — see [Clients](clients.md).

## Testing

`Benzene.RabbitMq`'s own tests show both levels:

- **Unit (no broker)** — the worker's ack/nack decisions are driven through a real
  `AsyncEventingBasicConsumer` against a mocked `IChannel`
  (`test/Benzene.Core.Test/RabbitMq/RabbitMqWorkerTest.cs`): success→ack, failure→nack-requeue,
  redelivered-failure→nack-no-requeue, exception→nack, AutoAck. The getters, application dispatch,
  outbound status mapping/header forwarding, and real-DI registration completeness are unit-tested
  alongside.
- **Live (needs Docker)** — `test/Benzene.Integration.Test/RabbitMq/RabbitMqWorkerLiveTest.cs`
  round-trips a real message through a real broker: the `RabbitMqBenzeneMessageClient` publishes and
  the `RabbitMqWorker` consumes and dispatches through the pipeline, all against a `rabbitmq:4.0`
  container (CI-only, via the `DockerEmulatorCollection` fixture).

## Troubleshooting

- **`ACCESS_REFUSED` / login fails from the host** — you're connecting as `guest`. Use a non-`guest`
  user (see the Prerequisites note); `guest` only works over the container's loopback.
- **Handler never fires** — the `[Message("...")]` value must equal the message topic: the `topic`
  header if the producer set one, otherwise the AMQP routing key. Check the management UI
  (`http://localhost:15672`) that messages are actually landing in the queue the worker consumes
  (`RabbitMqConfig.QueueName`), and that the queue exists (the worker doesn't declare it).
- **Messages keep redelivering forever** — a handler that always fails with `RequeueOnFailure = true`
  is requeued once, then dead-lettered on the redelivery; if you see an endless loop, confirm you
  haven't set up a DLX that routes back to the same queue. For a hard cap, set
  `RequeueOnFailure = false` and configure a dead-letter exchange + queue policy on the broker.
- **A returned failure result isn't being redelivered** — you're in `AutoAck` mode (the broker acked
  on dispatch). Use the default `Explicit` mode for at-least-once processing.

## See Also

- [Unified Hosting Model](hosting.md)
- [Self-Hosted Worker Setup](getting-started-worker.md)
- [Kafka Setup](getting-started-kafka.md) — the closest sibling self-hosted worker
- [Message Handlers](message-handlers.md)
- [Message Result](message-result.md)
- [Idempotency](cookbooks/idempotency.md)
- [Capability Matrix](capability-matrix.md)
