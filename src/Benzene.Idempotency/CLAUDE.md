# Benzene.Idempotency

## What this package does
Message de-duplication for at-least-once transports. On SQS, Service Bus, Event Hub, Kafka (and any
other transport that can redeliver a message), the same message can be delivered more than once. This
package adds a pipeline middleware that ensures the handler's side effect runs **at most once** per
idempotency key: the first delivery is processed and its key recorded; redeliveries of that key
short-circuit without re-invoking the handler.

Persistence is pluggable via `IIdempotencyStore` so the dedupe record can live wherever suits the
deployment — no database opinion is baked in. The package ships an in-memory store for single-instance
workers/tests; a multi-instance deployment supplies a shared store (e.g. Redis).

## Key types
- `IdempotencyMiddleware<TContext>` — the middleware. Derives a key, atomically claims it, invokes the
  rest of the pipeline only on the first sighting, and records/releases the outcome afterwards.
- `IIdempotencyStore` — the pluggable persistence contract with **atomic** claim semantics
  (`TryClaimAsync` / `CompleteAsync` / `ReleaseAsync`). Implementations MUST make claim+insert atomic
  (Redis `SET NX`, a unique-key insert) so concurrent redeliveries can't both win the claim.
- `InMemoryIdempotencyStore` — default in-process store (dictionary + lock + TTL). Single-instance only.
- `IIdempotencyKeyStrategy<TContext>` — derives the key from a message. Swap it to key on a business
  identifier instead of the default.
- `HeaderOrBodyHashIdempotencyKeyStrategy<TContext>` — default strategy: prefers a caller-supplied
  `idempotency-key` header, else a deterministic SHA-256 hash of topic + body.
- `IdempotencyOptions` — header name, whether to hash the body when no header is present, a key
  prefix, and `InProgressBehavior`.
- `ClaimResult` / `IdempotencyRecord` / `IdempotencyStatus` — the store's data model.
- `IdempotencyConflictException` — thrown for an in-progress duplicate when
  `InProgressBehavior.Throw` is configured.
- `Extensions` — `UseIdempotency<TContext>(configure?)` (pipeline) and
  `AddInMemoryIdempotencyStore(ttl?)` (DI).

## How the outcome is decided
After `next()` runs, the middleware records completion only on success, so a transient failure never
permanently suppresses a message:
- handler **throws** → claim released → redelivery reprocesses (exception rethrown).
- context is `IHasMessageResult` and the result is **unsuccessful** → claim released → redelivery
  reprocesses.
- otherwise (no throw, and either no result signal or a successful one) → recorded **completed**.

On a duplicate of a completed key, the middleware sets a successful `IMessageResult` (when the context
is `IHasMessageResult`) so the transport acknowledges/completes the duplicate rather than looping.

## `InProgressBehavior`
A duplicate that arrives while the first copy is still `InProgress`:
- `Skip` (default) — drop it without invoking the handler; never double-processes, but a duplicate is
  lost if its sibling later fails before releasing.
- `Throw` — throw `IdempotencyConflictException` so the transport redelivers later (by which time the
  sibling has usually finished).

## Usage
```csharp
// DI: register a store once.
services.AddInMemoryIdempotencyStore(TimeSpan.FromHours(24));   // or your own IIdempotencyStore

// Pipeline: add the middleware before UseMessageHandlers, after logging/tracing.
app.UseIdempotency<MyContext>(o => o.HeaderName = "idempotency-key");
```
`UseIdempotency` uses a registered `IIdempotencyKeyStrategy<TContext>` if present, otherwise the
default header/body-hash strategy (resolving the transport's `IMessageHeadersGetter`,
`IMessageBodyGetter`, `IMessageTopicGetter`, which every `UseMessageHandlers` transport registers).

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** — `IMiddleware`/pipeline builder.
- **Benzene.Core.MessageHandlers** — transport-agnostic message accessors (`IMessageHeadersGetter`,
  `IMessageBodyGetter`, `IMessageTopicGetter`), `IHasMessageResult`/`MessageResult`.

## Conventions
- Engine is transport-agnostic; persistence is pluggable (mirrors how `Benzene.Saga` keeps state
  storage out of the engine). Do not bake in a specific database.
- A custom store's `TryClaimAsync` MUST be atomic — the whole guarantee rests on it.

## Tests
- `test/Benzene.Core.Test/Idempotency/InMemoryIdempotencyStoreTest.cs` — store semantics (claim wins
  once, refused while in-progress, completed outcome recorded, release/expiry allow re-claim, keys
  independent).
- `test/Benzene.Core.Test/Idempotency/IdempotencyMiddlewareTest.cs` — first-time processes+records;
  duplicate short-circuits; completed-duplicate replays a successful result; throw/failed-result
  release the claim; no-key passes through; in-progress duplicate skip vs. throw.
- `test/Benzene.Core.Test/Idempotency/HeaderOrBodyHashIdempotencyKeyStrategyTest.cs` — header key
  preferred, prefix applied, deterministic body hash, disabling body-hash returns null.
