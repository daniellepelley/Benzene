# Transactional Outbox

Publish a handler's event **atomically with its database write**, so a crash between "commit the
order" and "publish `order:created`" can never lose or duplicate-without-record the event.

## Problem Statement

[Response as Event](response-as-event.md) republishes a handler's response as an event by sending
it through `IBenzeneMessageSender` the moment the handler returns. That's a **dual write**: the
handler commits to its database, then a separate call publishes to SNS/SQS/EventBridge. If the
process dies between the two, you've committed the order but never announced it (or announced it
but rolled back the order).

The **outbox pattern** removes the dual write: the handler writes the event into an *outbox table
in the same database transaction as the business data*, and a separate **relay** later reads that
table and publishes. One transaction, so the event and the data commit or roll back together;
the relay gives you at-least-once delivery.

Benzene doesn't ship an outbox — the half that matters (writing the outbox row inside *your* DB
transaction) is application territory. But it's built to let you drop one in: the publish step
behind `UseResponseEvents` is the swappable **`IResponseEventPublisher`** port, resolved from the
same DI scope as your handler, so your implementation shares the handler's `DbContext`. This
cookbook wires an outbox behind it.

## Prerequisites

- `Benzene.ResponseEvents` and the [Response as Event](response-as-event.md) setup
- A scoped `DbContext` (see [Entity Framework Core Integration](entity-framework-integration.md))
- An outbound route per event topic (`AddOutboundRouting`), for the relay to publish through

## Step-by-Step Implementation

### 1. An outbox table

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Topic { get; set; }
    public string PayloadType { get; set; }   // assembly-qualified type name
    public string Payload { get; set; }        // serialized event
    public string Headers { get; set; }        // serialized header dictionary
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? PublishedOnUtc { get; set; }
}

// In your DbContext:
public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
```

### 2. An `IResponseEventPublisher` that writes to the outbox

Instead of sending, it **adds a row to the same `DbContext`** the handler is using — no
`SaveChanges` here, so the row joins the handler's pending transaction:

```csharp
public class OutboxResponseEventPublisher : IResponseEventPublisher
{
    private readonly OrdersDbContext _db;

    public OutboxResponseEventPublisher(OrdersDbContext db) => _db = db;

    public Task<IBenzeneResult> PublishAsync(string eventTopic, object payload,
        IDictionary<string, string> headers = null)
    {
        _db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = eventTopic,
            PayloadType = payload.GetType().AssemblyQualifiedName,
            Payload = JsonSerializer.Serialize(payload, payload.GetType()),
            Headers = JsonSerializer.Serialize(headers ?? new Dictionary<string, string>()),
            OccurredOnUtc = DateTime.UtcNow,
        });

        // Not sent yet — the relay does that. Report success so the message is acknowledged.
        return Task.FromResult(BenzeneResult.Accepted());
    }
}
```

Register it — a plain `AddScoped` overrides the default `IBenzeneMessageSender`-backed publisher
(`UseResponseEvents` registers that one with `TryAddScoped`, so yours wins):

```csharp
services.AddScoped<IResponseEventPublisher, OutboxResponseEventPublisher>();
```

`OutboxResponseEventPublisher` and your handler resolve the *same* scoped `DbContext` — Benzene
creates one DI scope per message and `DbContext` is scoped — so the outbox row and the business
data are pending on one context.

### 3. Commit both in one transaction

The outbox row is written by `ResponseEventsMiddleware`, which runs **after** your handler but
**inside** `UseMessageHandlers`. So commit *once, at the end*, from a transport-pipeline step that
wraps `UseMessageHandlers` — by then both the handler's entities and the outbox row are on the
`DbContext`:

```csharp
public class UnitOfWorkMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly OrdersDbContext _db;
    public UnitOfWorkMiddleware(OrdersDbContext db) => _db = db;
    public string Name => "UnitOfWork";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        await next();                    // handler writes business data + outbox row (unsaved)
        await _db.SaveChangesAsync();    // one transaction — data and event commit together
    }
}
```

Wire it before `UseMessageHandlers`, and require your handlers to *add* entities without calling
`SaveChanges` themselves (standard unit-of-work discipline):

```csharp
aws.UseSqs(sqs => sqs
    .Use(resolver => new UnitOfWorkMiddleware<SqsMessageContext>(resolver.GetService<OrdersDbContext>()))
    .UseMessageHandlers(router => router
        .UseResponseEvents(events => events.Map("order:create", "order:created"))));
```

> The `.Use(resolver => ...)` factory runs per message with that message's scoped resolver, so it
> binds the correct per-message `DbContext`. Guard the commit on success if a failed handler might
> leave partial entities (e.g. only `SaveChanges` when the result is successful, or don't mutate on
> the failure path).

### 4. The relay

A background worker polls the outbox and publishes unsent rows through `IBenzeneMessageSender`
(so they ride the normal outbound routes — retry, correlation/trace stamping, transport choice):

```csharp
public class OutboxRelay : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;

    public OutboxRelay(IServiceScopeFactory scopes) => _scopes = scopes;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var sender = scope.ServiceProvider.GetRequiredService<IBenzeneMessageSender>();

            var pending = await db.Outbox
                .Where(x => x.PublishedOnUtc == null)
                .OrderBy(x => x.OccurredOnUtc)
                .Take(100)
                .ToListAsync(stoppingToken);

            foreach (var message in pending)
            {
                var payload = JsonSerializer.Deserialize(message.Payload, Type.GetType(message.PayloadType));
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(message.Headers);

                var result = await sender.SendAsync<object, Void>(message.Topic, payload, headers);
                if (result.IsSuccessful)
                {
                    message.PublishedOnUtc = DateTime.UtcNow;   // mark sent
                }
            }

            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
```

```csharp
services.AddHostedService<OutboxRelay>();
```

## Testing

- **Publisher**: call `PublishAsync` on an `OutboxResponseEventPublisher` backed by an in-memory /
  SQLite `DbContext`; assert a row is added and *not yet* saved.
- **Atomicity**: run the handler pipeline end-to-end (`BenzeneTestHost`) against a real DB in
  Docker, make the handler throw after writing, and assert neither the business row nor the outbox
  row committed.
- **Relay**: seed an unsent row, run one relay pass with a fake `IBenzeneMessageSender`, assert it
  sent and stamped `PublishedOnUtc`.

## Variations & Gotchas

- **At-least-once, not exactly-once.** The relay can publish then crash before stamping the row →
  redelivery. Consumers must be idempotent — see [Idempotency](idempotency.md).
- **Ordering.** `OrderBy(OccurredOnUtc)` gives best-effort order; for strict per-entity order,
  partition by an aggregate/partition key and publish those in sequence.
- **Throughput.** Poll in batches (above) or switch to a push trigger (e.g. Postgres
  `LISTEN/NOTIFY`, SQL Server change tracking) to cut latency.
- **Cleanup.** Delete or archive rows past a retention window so the table stays small.
- **Relay hosting.** Run the relay as its own process/deployment for isolation, or as a hosted
  service co-located with the app — it only needs the `DbContext` and `AddOutboundRouting`.
- **Not using ResponseEvents?** The same `OutboxMessage` + relay works if your handler writes the
  outbox row directly; the `IResponseEventPublisher` seam just lets the *response-as-event* mapping
  feed the outbox instead of publishing inline, with no handler code change.

## Related

- [Response as Event](response-as-event.md) — the inline (non-durable) version this hardens.
- [Entity Framework Core Integration](entity-framework-integration.md) — the scoped `DbContext`.
- [Idempotency](idempotency.md) — required for at-least-once consumers.
- [SNS Fan-Out Pattern](sns-fan-out.md) — a common relay destination.
