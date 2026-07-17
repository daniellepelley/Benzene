# Benzene.Saga

## What this package does
An in-code **saga orchestrator** for distributed transactions across services: run a sequence of
stages that either **completes in full or rolls back in full**, leaving no orphaned records, so the
whole operation can be safely retried. It's the generalized, sustainable successor to the original
commercial Benzene saga code (`Legacy/Benzene.Framework/Saga` in the `BenzeneImport` repo) — see
`work/saga-design.md` for the design and the decisions taken.

## The model
```
Saga  ── ordered ──▶  Stage  ── concurrent ──▶  Step (forward + compensation)
```
- **Step** (`SagaStep<T>`) — a forward action (`Func<SagaContext, Task<IBenzeneResult<T>>>`) paired
  with an optional compensation (`Func<SagaContext, T, Task<IBenzeneResult>>`). Success is
  `IBenzeneResult.IsSuccessful`. A forward that throws is caught and treated as a failed step.
  Compensation runs during rollback **only if the step succeeded**; a succeeded step with no
  compensation is treated as "nothing to undo".
- **Stage** — an N-sized group of steps run concurrently (`Task.WhenAll`); succeeds only if every
  step succeeds. On its own failure it compensates its concurrently-succeeded steps.
- **Saga** — runs stages in order, threading each stage's results into a shared `SagaContext`
  (typed bag, `ctx.Get<T>()`) so a later stage can use an earlier stage's output. On the first
  stage failure it compensates every completed effect in **reverse (LIFO) order** — the failed
  stage's succeeded steps first, then each completed stage newest-first — then returns a
  `SagaResult`.

## Key types
- `new SagaBuilder()` → `StageBuilder` → `StepBuilder<T>` → `.Build()` returns a `Saga` — the fluent
  API. (Entry is `new SagaBuilder()`, not `Saga.Define()`: a bare type named `Saga` inside namespace
  `Benzene.Saga` is shadowed by the namespace from any `Benzene.*` caller — same reason
  `Benzene.Results` names its type `BenzeneResult`, not `Results`.)
- `SagaContext` — typed result bag; steps publish their result after their stage succeeds.
- `SagaResult` — `Outcome` (`Succeeded` / `RolledBack` / `PartiallyRolledBack`), `IsSuccess`,
  `FailedStageIndex`, `Failure` (the failing step's result), `FailureException`, and
  `CompensationFailures` (steps whose compensation itself failed — orphaned effects to attend to).
- `SagaStepState` — `Pending` / `Succeeded` / `Failed` / `RolledBack` / `CompensationFailed`.
- `Saga.RunAsync(SagaRunOptions)` — the opt-in overload for the §7 fast-follows (`RunAsync()` stays
  the zero-overhead default: one attempt, no store, no id). `SagaRunOptions` carries an optional
  `SagaId`/`Name`, an `ISagaStateStore`, and a `SagaRetryPolicy`.
- `SagaRetryPolicy` (§7.5) — whole-saga retry: `MaxAttempts` (total attempts, 1 = no retry),
  `InitialDelay`, `BackoffFactor`, and an internal `Delay` seam for tests. Retry fires **only** on
  `RolledBack` (a clean rollback is safe to re-run from scratch); `Succeeded` needs none and
  `PartiallyRolledBack` must not be re-run (would double-apply orphaned effects).
- `ISagaStateStore` (§7.4) — pluggable progress/outcome sink: `RecordStartedAsync` →
  `RecordStageCompletedAsync` (per completed stage) → `RecordFinishedAsync`, once per attempt.
  Records progress for durable **observability/operational recovery**; it does **not** resume a saga
  (in-memory step closures can't be serialized/rehydrated). `InMemorySagaStateStore` (event list +
  `EventsFor(sagaId)`) is the built-in test double; a durable adapter is a 3-method copy-paste (see
  the cookbook). `SagaRunInfo`/`SagaStateEvent`/`SagaStateEventKind` are the data model.

## Design decisions (from `work/saga-design.md` §7)
- **Await-all within a stage** (not fail-fast) — deterministic; every step's outcome is known
  before deciding to compensate.
- **Best-effort rollback** — every compensation is attempted even if one fails; failures surface as
  `PartiallyRolledBack` + `CompensationFailures` rather than stranding the rest.
- **Typed `SagaContext`** for threading results between stages.
- **In-process execution** — the engine runs a saga to completion/rollback in one call. Progress can
  be recorded to a pluggable `ISagaStateStore` (shipped fast-follow) for durable observability, but
  the engine does **not** resume in-memory step closures after a crash (they can't be serialized) —
  no DB dependency is baked into core. Optional whole-saga retry via `SagaRetryPolicy` (shipped).
- **Client-agnostic engine** — depends only on `Benzene.Abstractions` + `Benzene.Results`, **not**
  `Benzene.Clients`. Because `IBenzeneMessageSender.SendAsync(topic, req)` already returns
  `Task<IBenzeneResult<T>>`, a step's `Do(...)` calls it directly — no adapter package needed. An
  HTTP call or any async action returning an `IBenzeneResult<T>` works identically.

## When to use
- Multi-step operations spanning several services where partial completion must not leave orphaned
  records (e.g. a signup that creates a tenant, then a user, then RBAC roles across services).

## Dependencies on other Benzene packages
- **Benzene.Abstractions** — `IBenzeneResult` / `IBenzeneResult<T>`.
- **Benzene.Results** — `BenzeneResult` factory (used to synthesize a failure result when a forward throws).

## Conventions / notes
- Concurrency safety: steps in a stage run concurrently but only **read** earlier stages' context
  values during that phase; writes happen single-threaded after each stage barrier (`Stage.Publish`),
  so `SagaContext` needs no locking.
- Test coverage lives in `test/Benzene.Core.Test/Saga/` — `SagaTest.cs` (happy path, mid-stage
  failure + rollback, cross-stage LIFO rollback, compensation-failure → `PartiallyRolledBack`,
  forward-throws) and `SagaRetryAndStateStoreTest.cs` (retry recovers a flaky step, exhausts to
  `RolledBack`, refuses to retry `PartiallyRolledBack`; state store records start/stage/finish, only
  completed stages on failure, one `Started` per retry attempt, and generates an id when none given).
- Vocabulary note: the legacy code called the forward+compensation unit a "Part" and the parallel
  group a "Step"; this package renames them **Step** and **Stage** respectively.
