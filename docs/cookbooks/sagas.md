# Sagas (distributed transactions that roll back cleanly)

Some operations span several services and must be all-or-nothing: a signup that creates a tenant,
then a user, then an RBAC role, across three systems. There's no database transaction that covers
all three — if the third step fails, you're left with an orphaned tenant and user. A **saga** solves
this: run the steps, and if any one fails, run each completed step's *compensation* in reverse to
undo everything, leaving the system back where it started so the whole thing can be retried.

`Benzene.Saga` is an in-code saga orchestrator. There's no workflow engine to stand up, no attributes,
no DSL, no database — you describe the steps with a fluent builder and call `RunAsync()`.

## Is this hard to use?

No. If you can write an `async` method, you can write a saga. The whole API is three nested builders
(`Saga → Stage → Step`) and each step is two plain functions: the thing to do, and the thing that
undoes it. The orchestrator handles ordering, concurrency, rollback order, and failure reporting. The
one part that needs real thought is writing correct **compensations** — that's inherent to the
pattern, not the library, and this guide calls out how to get it right.

## The mental model

```
Saga  ── ordered ──▶  Stage  ── concurrent ──▶  Step (do + undo)
```

- A **Step** is one unit of work plus its compensation: a forward action (`Do`) and, optionally, the
  action that undoes it (`Compensate`).
- A **Stage** is a group of steps that run **concurrently** and succeed or fail together.
- A **Saga** is an ordered list of stages that run **one after another**. A later stage can use an
  earlier stage's results.

If any step fails, the saga compensates every effect created so far — the failed stage's
concurrently-succeeded steps first, then each earlier stage newest-first (last-in, first-out) — and
reports a rolled-back result.

## The simplest possible saga

One stage, one step, with its compensation:

```csharp
using Benzene.Saga;
using Benzene.Results;

var saga = new SagaBuilder()
    .Stage(stage => stage
        .Step<Order>(step => step
            .Do(_ => CreateOrderAsync())                       // returns Task<IBenzeneResult<Order>>
            .Compensate((_, order) => CancelOrderAsync(order.Id))))
    .Build();

SagaResult result = await saga.RunAsync();

if (result.IsSuccess)
{
    // every step succeeded
}
```

> **Entry point is `new SagaBuilder()`**, not `Saga.Define()`. A bare type named `Saga` is shadowed
> by the `Benzene.Saga` namespace from any `Benzene.*` caller, so the builder is the front door.

A step's `Do` returns `Task<IBenzeneResult<T>>` — success is `IBenzeneResult.IsSuccessful`. That's the
same result type `IBenzeneMessageSender.SendAsync(...)` and every Benzene handler already returns, so
a step usually *is* just a service call (see [Calling real services](#calling-real-services)).

## A realistic saga: parallel work + passing data forward

The signup flow — create a tenant and an Okta company **in parallel**, then a user (needs the tenant
id), then a role (needs the user id):

```csharp
var saga = new SagaBuilder()
    // Stage 1: two steps run concurrently.
    .Stage(stage => stage
        .Step<TenantCreated>(step => step
            .Do(_ => api.CreateTenantAsync(companyName))
            .Compensate((_, tenant) => api.DeleteTenantAsync(tenant.TenantId)))
        .Step<OktaCompanyCreated>(step => step
            .Do(_ => api.CreateOktaCompanyAsync(companyName))
            .Compensate((_, company) => api.DeleteOktaCompanyAsync(company.CompanyId))))
    // Stage 2: runs after stage 1; reads the tenant id stage 1 produced.
    .Stage(stage => stage
        .Step<UserCreated>(step => step
            .Do(ctx => api.CreateUserAsync(ctx.Get<TenantCreated>().TenantId))
            .Compensate((_, user) => api.DeleteUserAsync(user.UserId))))
    // Stage 3: reads the user id from stage 2.
    .Stage(stage => stage
        .Step<RoleCreated>(step => step
            .Do(ctx => api.CreateRoleAsync(ctx.Get<UserCreated>().UserId))
            .Compensate((_, role) => api.DeleteRoleAsync(role.RoleId))))
    .Build();

var result = await saga.RunAsync();
```

If, say, the role step in stage 3 fails, the orchestrator undoes the user (stage 2), then the Okta
company and the tenant (stage 1) — in reverse — and returns `RolledBack`. No orphaned records.

This is the full worked example — runnable, with a fake API that shows the store ending up empty
after a rollback — in [`examples/Saga`](../../examples/Saga/Benzene.Example.Saga).

## Passing data between stages: `SagaContext`

Each succeeded step publishes its result into a typed bag after its stage completes; a later stage
reads it:

```csharp
.Do(ctx => api.CreateUserAsync(ctx.Get<TenantCreated>().TenantId))
```

- `ctx.Get<T>()` — the result of the step that produced a `T`, keyed by type.
- If a **single stage** produces two values of the *same* type, disambiguate with `.Key("...")` on
  the step and `ctx.Get<T>("...")` on the reader.
- `ctx.TryGet<T>(out var v)` / `ctx.Has<T>()` for optional reads.

Steps within a stage run concurrently but only *read* earlier stages' values, so there's no locking
to worry about — writes happen at each stage boundary, single-threaded.

## Reading the result

`SagaResult` tells you exactly what happened:

| Member | Meaning |
|---|---|
| `IsSuccess` / `Outcome == Succeeded` | Every stage completed. |
| `Outcome == RolledBack` | A step failed and **every** compensation succeeded — system is back to its starting state, safe to retry. |
| `Outcome == PartiallyRolledBack` | A step failed **and at least one compensation also failed** — some effects may still exist. **Needs attention** (see `CompensationFailures`). |
| `FailedStageIndex` | Zero-based index of the stage that failed. |
| `Failure` | The failing step's `IBenzeneResult` (why it failed). |
| `FailureException` | The exception the forward action threw, if it threw rather than returning a failed result. |
| `CompensationFailures` | The steps whose compensation itself failed — the orphaned effects to reconcile manually. |

```csharp
var result = await saga.RunAsync();
switch (result.Outcome)
{
    case SagaOutcome.Succeeded:          /* done */                       break;
    case SagaOutcome.RolledBack:         /* clean failure — retry maybe */ break;
    case SagaOutcome.PartiallyRolledBack: /* alert: manual cleanup */       break;
}
```

## Calling real services

A step's forward action is any `async` function returning `Task<IBenzeneResult<T>>`. The two common
sources both already return exactly that, so there's no glue:

```csharp
// A Benzene message send (to another service over any transport):
.Do(_ => messageSender.SendAsync<CreateTenant, TenantCreated>("tenant:create", new CreateTenant(name)))

// An HTTP/SDK call you wrap to return an IBenzeneResult<T>:
.Do(_ => httpClient.CreateTenantAsync(name))
```

A forward action that **throws** is caught and treated as a failed step (the exception shows up as
`SagaResult.FailureException`), so you don't have to wrap calls in try/catch to be safe.

## Writing good compensations

This is the part that deserves care — the library runs your compensation, but *correctness* is on
you:

- **Key the undo by an id from the forward result.** `Compensate((_, tenant) => DeleteTenantAsync(tenant.TenantId))` — the second argument is the forward step's own result, so you always have the id of the thing to remove.
- **Make it idempotent.** A compensation may run against an effect that's already gone (e.g. after a
  retry). "Delete tenant X" should succeed, or no-op, if X no longer exists — not throw.
- **A step with no side effect needs no `Compensate`.** A read-only or pure step is simply "nothing
  to undo"; omit compensation and rollback skips it.
- **Compensation runs only for steps that succeeded.** A step that failed created no effect, so it's
  never compensated; the orchestrator only unwinds what actually happened.

## What it does *not* do (by design)

- **It's in-process.** The saga runs to completion (or rollback) within one `RunAsync()` call. It
  does **not** persist progress or resume after a process crash — the steps are in-memory functions,
  which can't be serialized and rehydrated. If the host dies mid-saga, you re-run the operation; your
  idempotent forwards/compensations make that safe.
- **It doesn't retry for you (yet).** On a clean `RolledBack`, *you* decide whether to call
  `RunAsync()` again. A built-in retry policy is a planned fast-follow.
- **No two-phase commit, no distributed locks.** That's the whole point of the saga pattern — it
  trades atomic isolation for compensations, which is what lets it span services that share no
  transaction.

## Testing

A saga is plain objects and functions, so tests need no host or mocks framework — pass in fakes that
return `BenzeneResult.Ok(...)` or a failure, and assert on `SagaResult`:

```csharp
var log = new List<string>();
var saga = new SagaBuilder()
    .Stage(s => s.Step<string>(step => step
        .Do(_ => { log.Add("create"); return Task.FromResult(BenzeneResult.Ok("id-1")); })
        .Compensate((_, id) => { log.Add($"undo:{id}"); return Task.FromResult(BenzeneResult.Ok()); })))
    .Stage(s => s.Step<string>(step => step
        .Do(_ => Task.FromResult(BenzeneResult.ServiceUnavailable<string>()))))   // forces failure
    .Build();

var result = await saga.RunAsync();

Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
Assert.Equal(new[] { "create", "undo:id-1" }, log);   // stage-1 effect was undone
```

The engine's own suite (`test/Benzene.Core.Test/Saga/`) covers the happy path, mid-stage failure +
rollback, cross-stage LIFO rollback, compensation-failure → `PartiallyRolledBack`, and a
forward-throws case — worth a read as executable documentation.

## Troubleshooting

- **`Saga.Define()` doesn't compile / `Saga` is ambiguous** — use `new SagaBuilder()`; the type name
  is shadowed by the namespace (see the note above).
- **`A saga requires at least one stage` / `... at least one step`** — every saga needs a stage and
  every stage needs a step; `.Build()` enforces it up front.
- **`ctx.Get<T>()` throws `KeyNotFoundException`** — you're reading a value in the same stage that
  produces it (values publish only *after* a stage completes), or two steps produce the same type
  without a `.Key(...)`. Move the read to a later stage, or key the producers.
- **Got `PartiallyRolledBack`** — a compensation failed; the system may hold orphaned effects.
  Inspect `CompensationFailures`, reconcile them, and make those compensations idempotent so a
  re-run finishes the cleanup.

## Further reading

- [`examples/Saga`](../../examples/Saga/Benzene.Example.Saga) — the runnable signup saga.
- `src/Benzene.Saga/CLAUDE.md` — the type-by-type reference and design decisions.
- `work/saga-design.md` — the full design, including the legacy code it generalizes and the open
  questions (retry policy, pluggable state store) resolved along the way.
