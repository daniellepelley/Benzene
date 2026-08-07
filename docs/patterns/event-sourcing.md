# Event Sourcing

**Status: DRAFT v0.1 — part of the [Patterns](index.md) "at enterprise scale" set.**

Regulated and audit-heavy domains — trading, payments, ledgers — often cannot store just the
*current* state of an aggregate; they must store **every change that led to it**, as an immutable,
ordered log. That is **event sourcing**: the aggregate's state is a fold over its event history, the
log is the source of truth, and "what did this account look like last Tuesday?" and "prove how we got
to this balance" are answerable by construction.

Benzene has **no first-class event-sourcing library** — no event store, no aggregate-rehydration
helper, no snapshot or replay framework. What it has is the set of primitives you **compose** one
from, and a change-data-capture transport that makes the projection half nearly free. This document
is explicit about that line, and works a **trade ledger** through it.

---

## What Benzene gives you, and what you build

| Piece | Status |
|---|---|
| **Command ingest** — validate and decide | **Built-in** — a message handler (`[Message("account:debit")] IMessageHandler<Debit, …>`) is the command handler. |
| **The event log** — append-only, ordered | **Composed** — an append-only store you own. On AWS, a DynamoDB table keyed `(aggregateId, sequence)` is the log; **writing the row *is* the append** (there is no Benzene "append" call — the write is your data-layer's). |
| **Projections** — turn events into read state | **Built-in transport** — point DynamoDB Streams at the log and consume it with `UseDynamoDb` (`[Message("ledger:INSERT")]`); Benzene delivers each appended event, in shard order, to a projector. This is [CQRS](cqrs-read-models.md) fed by the log. |
| **Idempotency / exactly-once effect** | **Built-in middleware** — `UseIdempotency()` (+ a durable store) so a replayed event projects once. |
| **Durable event evolution** — old events, new code | **Built-in** — `AddPayloadVersioning` upcasts historical event schemas to the current shape at the pipeline edge (below). |
| **Aggregate rehydration, snapshots, replay orchestration** | **App-level — you write these.** Benzene deliberately does not impose an aggregate base class or a store interface. |

The honest summary: **Benzene gives you the ingest, the stream-projection consumer, idempotency, and
event versioning; you own the log, the rehydration fold, snapshots, and the replay driver.** That is
a deliberate small surface — event-sourcing conventions vary enough that a framework abstraction
usually gets in the way.

---

## How you build it with Benzene

### 1. The command handler appends an event

A command handler validates, loads the aggregate's current state (a fold of its events, below),
decides, and **appends** the resulting event(s) to the log — an ordinary write to the append store:

*(informative, .NET)*

```csharp
[Message("account:debit")]
public class Debit : IMessageHandler<Debit, DebitAccepted>
{
    private readonly IEventLog _log;   // your append-only store (e.g. DynamoDB), app-owned
    public Debit(IEventLog log) => _log = log;

    public async Task<IBenzeneResult<DebitAccepted>> HandleAsync(Debit cmd)
    {
        var account = await Rehydrate(cmd.AccountId);           // fold events → current state (app code)
        if (account.Balance < cmd.Amount)
            return BenzeneResult.Invalid("insufficient-funds");  // a decision, returned as a result
        var evt = new AccountDebited(cmd.AccountId, cmd.Amount, account.NextSequence);
        await _log.AppendAsync(evt);        // the append IS the DynamoDB write; ordered by sequence
        return BenzeneResult.Ok(new DebitAccepted(evt.Sequence));
    }
}
```

Two Benzene-shaped details that matter for correctness:

- The decision (`insufficient-funds`) is a returned [result](../specification/core-concepts.md#5-result),
  not a thrown exception — failure stays in the type, and the transport maps the status.
- The append's **optimistic concurrency** (reject if the aggregate moved since you rehydrated) is
  your store's conditional write — a DynamoDB `attribute_not_exists`/version-check on
  `(aggregateId, sequence)`. Benzene doesn't do this for you; the log does.

### 2. The log emits its events — via change data capture

You do **not** publish the event from the handler (that would be the [dual-write
problem](transactional-outbox.md)). Instead, the append is captured off the log's stream and
projected — the [transactional outbox](transactional-outbox.md), applied to the event log itself:

```csharp
app.UseAwsLambda(events => events
    .UseDynamoDb(cdc => cdc
        .UseIdempotency()
        .UseMessageHandlers()));

[Message("ledger:INSERT")]                 // a newly-appended event, in shard order
public class ProjectBalance : IMessageHandler<AccountEvent>
{
    private readonly IReadStore _view;
    public ProjectBalance(IReadStore view) => _view = view;
    public Task<IBenzeneResult> HandleAsync(AccountEvent e)
        => _view.ApplyAsync(e);            // fold the event into the read model
}
```

Because DynamoDB Streams are **shard-ordered and processed sequentially, resuming from the first
failed record**, events project **in order, at least once** — exactly what a ledger needs. The read
models this feeds are ordinary [CQRS](cqrs-read-models.md) views: current balances, statements,
positions — each a projection of the same authoritative log.

### 3. Rehydration and snapshots (your code, Benzene-friendly shapes)

- **Rehydrate** = read an aggregate's events in sequence and fold them into state. This is app code;
  keep the fold **pure** (`(state, event) => state`) so it is testable and identical to the
  projection fold.
- **Snapshots** avoid re-reading a long history: periodically store a folded state at a sequence, and
  rehydrate from the latest snapshot forward. Also app-level — Benzene has no snapshot type — but the
  in-process pipeline makes it easy to replay events through the *same* projection handler you deploy.
- **Replay** = re-project the whole log into a fresh read model (fix a projection bug, add a view).
  Drive it by re-reading the log and invoking the projection handler **in process** (build the
  pipeline and call it directly), or by re-streaming. The projections' idempotent folds make replay a
  routine operation, not a migration.

### 4. Events are immutable — so version them, never rewrite them

An event, once written, is history and cannot be edited. When the event's shape must evolve, you
keep the old events exactly as written and **upcast** them to the current shape as they are read —
Benzene's payload-schema versioning does this at the pipeline edge:

```csharp
services.AddPayloadVersioning(v => v.ForContext<DynamoDbRecordContext>()
    .Topic("ledger:INSERT", t => t
        .Version<AccountDebitedV1>("v1")
        .Version<AccountDebitedV2>("v2")
        .Upcast<AccountDebitedV1, AccountDebitedV2>(f => f.RegisterInitValue(e => e.Currency, "USD"))));
```

One projection handler on the latest schema; a decade of historical `v1` events upcast on read; the
caster graph **validated at startup** (a missing conversion path throws at boot, not on a 2015 event
in production). Chain composition handles a long back-catalog. This is the mechanism that makes an
append-only log survivable across years of schema change.

---

## Worked example: a trade ledger

**The system:** every change to a trading account — trades booked, cash moved, fees applied — must be
an immutable, ordered, auditable record; current positions and balances must be queryable fast; and
"reconstruct the account as of any past instant" must be possible for audit and dispute.

- **Commands** (`trade:book`, `cash:move`, `fee:apply`) are Benzene handlers. Each rehydrates the
  account, validates against current state, and **appends** an event to the DynamoDB event log,
  ordered by `(accountId, sequence)` with a conditional write for optimistic concurrency.
- **The log is the truth.** Nothing updates state in place; state is a fold of events.
- **Projections** consume the log's stream (`UseDynamoDb`, `ledger:INSERT`) and maintain read models:
  current positions, cash balances, a statement view — each a [CQRS](cqrs-read-models.md) projection,
  in order, at least once, idempotent.
- **Audit / point-in-time** is answered by replaying an account's events up to a sequence or
  timestamp through the pure fold — no separate audit system, because the log *is* the audit trail.
- **Evolution:** as the trade event shape changes over years, historical events are upcast on read
  via `AddPayloadVersioning`; the log is never rewritten.
- **Reliability:** idempotent projections + at-least-once ordered CDC mean a redelivery or a full
  replay converges to the same balances — the property an auditor will ask you to demonstrate.

---

## When to use it

- **Use it** when the **history is a requirement**, not a nicety — audit, regulation, dispute
  resolution, temporal queries ("as of"), or when several read models must derive from the same
  authoritative change log.
- **Don't** event-source a CRUD aggregate whose history nobody needs — a [core service](core-services.md)
  storing current state is simpler and correct. Event sourcing adds real complexity (rehydration,
  snapshots, event versioning, eventual-consistency reads); spend it where the history pays for it,
  not fleet-wide by default.

---

## Checklist

Event sourcing is well-formed when:

- [ ] State changes are **immutable, ordered events** appended to a log; nothing updates in place.
- [ ] The append is the **write to the log** (no separate publish — CDC emits the event).
- [ ] Rehydration and projection use the **same pure fold**; projections are idempotent.
- [ ] Read models are **CQRS projections** of the log, in order, at least once.
- [ ] Historical events are **upcast on read** (`AddPayloadVersioning`), never rewritten.
- [ ] The choice is **deliberate per aggregate** — event-sourced where history is required, plain
      CRUD where it isn't.

See also: [transactional outbox](transactional-outbox.md) (the CDC mechanism that emits appended
events), [CQRS & read models](cqrs-read-models.md) (what the log projects into), and
[map-reduce](map-reduce.md) (for replaying/aggregating a large log in parallel).
