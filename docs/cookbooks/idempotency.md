# Idempotency (de-duplicating redelivered messages)


> **Boundary:** Benzene ships an in-memory, single-process idempotency store; cross-instance de-duplication is deliberately not solved in-box (see why, and the external-store pattern, in the [Capability Matrix](../capability-matrix.md)).

At-least-once transports — SQS, Azure Service Bus, Event Hubs, Kafka — will occasionally deliver the
same message more than once: a visibility timeout lapses mid-processing, a consumer restarts before
committing, a producer retries after a network blip. If your handler has a side effect (charges a
card, sends an email, inserts a row), processing the duplicate does the side effect twice.

`Benzene.Idempotency` adds a pipeline middleware that makes the handler run **at most once** per
message, backed by a pluggable store so the dedupe record lives wherever suits your deployment.

## Problem statement

The same message is delivered twice. You want the second delivery to be recognised as a duplicate and
short-circuited — without re-running the handler — while a genuine *first* delivery of a different
message still runs normally, and a *failed* attempt is still retried on redelivery.

## Prerequisites

- A Benzene worker/consumer using `UseMessageHandlers()` (which registers the message accessors the
  default key strategy needs).
- A reference to `Benzene.Idempotency`.

## Step 1 — register a store

The middleware needs an `IIdempotencyStore`. For a single worker instance (or tests) use the
in-memory store:

```csharp
services.AddInMemoryIdempotencyStore(TimeSpan.FromHours(24)); // retain keys for 24h
```

> **Multi-instance deployments need a shared store.** The in-memory store keeps its map in one
> process, so a duplicate redelivered to a *different* instance won't be recognised. Register a
> shared store instead (see [Step 5](#step-5--a-shared-store-for-multi-instance-deployments)).

## Step 2 — add the middleware

Add `UseIdempotency` to the pipeline before `UseMessageHandlers`, after any logging/tracing so
duplicates remain observable:

```csharp
app
    .UseProcessResponse()
    .UseIdempotency<ServiceBusMessageContext>()   // <-- de-duplicate here
    .UseMessageHandlers(x => x.AddHandlers());
```

That's it. The first delivery of each message is processed; redeliveries of the same key
short-circuit.

## Step 3 — how the key is derived

By default (`HeaderOrBodyHashIdempotencyKeyStrategy`):

1. If the message carries an `idempotency-key` header, its value is the key. This is the strongest
   option — the *producer* stamps a stable key (e.g. the business event ID), so even a re-published
   message with a byte-different body is still recognised as the same logical event.
2. Otherwise, the key is a deterministic SHA-256 hash of the message topic + body. Identical
   redeliveries hash to the same key; genuinely different messages don't collide.

Tune it via options:

```csharp
app.UseIdempotency<ServiceBusMessageContext>(o =>
{
    o.HeaderName = "idempotency-key";   // header to read a caller-supplied key from
    o.HashBodyWhenNoHeader = true;      // false = only de-dupe messages that carry an explicit key
    o.KeyPrefix = "orders:";            // namespace keys in a shared store
});
```

To key on a business identifier instead, register your own `IIdempotencyKeyStrategy<TContext>` before
`UseIdempotency` — it will be used automatically:

```csharp
services.AddScoped<IIdempotencyKeyStrategy<ServiceBusMessageContext>, OrderIdKeyStrategy>();
```

## Step 4 — failure handling (retries still work)

Idempotency must not turn a transient failure into a permanently swallowed message. The middleware
only records a key as *completed* on success:

| First attempt | What happens |
|---|---|
| Handler succeeds | Key recorded **completed**; future duplicates short-circuit. |
| Handler **throws** | Claim **released**, exception rethrown; the transport redelivers and reprocesses. |
| Handler reports an unsuccessful result (`IHasMessageResult`) | Claim **released**; the transport redelivers and reprocesses. |

A duplicate that arrives while the first copy is *still in progress* is, by default, dropped
(`InProgressBehavior.Skip`) so the handler is never run twice concurrently. If losing a duplicate
whose sibling later fails is unacceptable, choose `Throw` instead — the duplicate is not acknowledged
and the transport redelivers it once the sibling has finished:

```csharp
app.UseIdempotency<ServiceBusMessageContext>(o =>
    o.InProgressBehavior = InProgressBehavior.Throw);
```

## Step 5 — a shared store for multi-instance deployments

`IIdempotencyStore` is three methods. Back it with anything that offers an **atomic** set-if-absent
(the whole guarantee rests on `TryClaimAsync` being atomic). A Redis implementation using
`SET key value NX` — `Benzene.Cache.Redis` already brings in `StackExchange.Redis`:

```csharp
public class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IDatabase _db;
    private readonly TimeSpan _ttl;

    public RedisIdempotencyStore(IDatabase db, TimeSpan ttl) { _db = db; _ttl = ttl; }

    public async Task<ClaimResult> TryClaimAsync(string key, CancellationToken ct = default)
    {
        // Atomic claim: only succeeds if the key does not already exist. Forward `ct` all the way
        // to the client call so a caller-side cancellation actually aborts the network round trip
        // instead of an accepted-but-ignored token (StackExchange.Redis honors it via the
        // command's async pipeline).
        var won = await _db.StringSetAsync(key, "in-progress", _ttl, When.NotExists).WaitAsync(ct);
        if (won)
            return ClaimResult.Won();

        var value = await _db.StringGetAsync(key).WaitAsync(ct);
        var status = value == "completed" ? IdempotencyStatus.Completed : IdempotencyStatus.InProgress;
        return ClaimResult.AlreadyExists(new IdempotencyRecord(key, status, wasSuccessful: status == IdempotencyStatus.Completed));
    }

    public Task CompleteAsync(string key, bool wasSuccessful, CancellationToken ct = default)
        => _db.StringSetAsync(key, wasSuccessful ? "completed" : "failed", _ttl).WaitAsync(ct);

    public Task ReleaseAsync(string key, CancellationToken ct = default)
        => _db.KeyDeleteAsync(key).WaitAsync(ct);
}
```

Register it as the `IIdempotencyStore` instead of `AddInMemoryIdempotencyStore`.

## Testing

`InMemoryIdempotencyStore` accepts an injected clock, so you can test TTL expiry without waiting, and
the middleware is a plain `IMiddleware<TContext>` you can drive directly. See
`test/Benzene.Core.Test/Idempotency/` for worked examples: first-delivery-processes,
duplicate-short-circuits, throw/failed-result-releases, and the key-derivation cases.

## Troubleshooting

- **Duplicates still slip through across instances** — you're on the in-memory store; move to a shared
  one (Step 5).
- **Nothing is being de-duplicated** — check a key is actually derived: with `HashBodyWhenNoHeader =
  false` and no header, the message is intentionally untracked. Confirm `UseIdempotency` sits before
  `UseMessageHandlers`.
- **A legitimately-new message is treated as a duplicate** — you're hashing the body but the producer
  reuses an identical payload for distinct events; stamp a unique `idempotency-key` header instead.

## Further reading

- `Benzene.Idempotency/CLAUDE.md` — package internals and the outcome-decision rules.
- [Redis Caching](redis-caching.md) — the `Benzene.Cache.Redis` connection setup you can reuse for a
  Redis-backed store.
- `work/enterprise-adoption-gap-analysis.md` A.3 — the design context.
