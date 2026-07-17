# Benzene.Saga — Design

**Status:** DRAFT for review — plan-first; no code yet, decisions needed (see §7)
**Last Updated:** 2026-07-17
**Purpose:** Design a general, sustainable saga/orchestration package for Benzene, using the
original commercial implementation as the design basis.
**Design basis:** `Legacy/Benzene.Framework/Saga/` in the `daniellepelley/BenzeneImport` repo — the
saga code from the first (customer-tailored) version of Benzene, still in production use.
**Backlog item:** A.1 in [`enterprise-adoption-gap-analysis.md`](enterprise-adoption-gap-analysis.md).

---

## 1. The problem being solved

An **in-code orchestrator** performs a **distributed transaction** across several services (mostly
Benzene-to-Benzene "lander-to-lander" calls, sometimes HTTP). The transaction must be
**all-or-nothing**: either every step completes, or every step that did complete is **rolled back**,
leaving the system exactly as it started — no orphaned records — after which the whole thing can be
safely retried.

Worked example (from production — a user-signup flow):

- **Stage 1** (parallel): create a tenant; create a company in Okta.
- **Stage 2**: using the tenant ID from stage 1, create a user in a microservice.
- **Stage 3**: using the user ID from stage 2, create an RBAC role + related identity items.
- If **any** call fails, run each stage's **compensation** (a function handed the created record's
  ID that deletes it), rolling back to the starting state; then optionally retry the whole flow.

This proved very reliable and orphan-free. The uniformity of Benzene calls (topic + payload) is what
makes wrapping a saga around them natural.

## 2. The legacy design (what exists today)

Files: `SagaStatus`, `ISagaPart`/`SagaPart`, `SagaPartFactory`, `ISagaStep`/`SagaStep`,
`SagaStepFactory`, `SagaStepResult`.

- **`SagaStatus`** — `Executing | Succeeded | Failed | RolledBack`.
- **`SagaPart<T>`** — the atomic unit: a forward action paired with a compensation.
  - Forward: `Task<(bool success, T result)>`; compensation: `Func<T, Task>`.
  - `ExecuteAsync`: await forward; on success capture `Result` + status `Succeeded`; on failure set
    `Failed`. `UndoAsync`: run the compensation **only if** the part `Succeeded`, then `RolledBack`.
- **`SagaPartFactory`** — builds a part from a **client call**: `up: Task<IClientResult<T>>`,
  `down: Func<T, Task<IClientResult>>`. Maps client-result status → success. **This is the
  Benzene integration point** — a part is "call a service, and here's the call that undoes it".
- **`SagaStep<T1..T4>`** — a **stage**: 1–4 parts run in **parallel** (`Task.WhenAll`). If all
  succeed → typed `SagaStepResult` with every part's result. If any fails → `Task.WhenAll` all
  parts' `UndoAsync` (only succeeded ones actually compensate) → failed result.
- **`SagaStepResult<T1..T4>`** — a typed carrier of a stage's parallel results.

### What the legacy framework does *not* provide

Confirmed by searching the import repo: **there is no cross-stage orchestrator in the framework.**
`SagaStep` handles all-or-nothing *within one stage*; sequencing stages, threading stage-N results
into stage-N+1, and rolling back **earlier** stages when a **later** stage fails was all hand-written
in the customer's application code. Providing that orchestrator is the single biggest improvement
opportunity.

## 3. What to keep vs. what to fix

**Keep (the good core):**
- Compensation is attached to the unit that created the effect (`up`/`down` pairing).
- Parallel execution within a stage; all-or-nothing per stage.
- A part maps directly onto a uniform Benzene service call + its compensating call.
- Full rollback → clean retry, no orphans.

**Fix (limitations for general use):**
1. **Arity cap.** `SagaStep<T1..T4>` hand-codes 1–4 parts; 5+ is impossible and each arity is
   duplicated. → Support **N parts** per stage.
2. **No framework orchestrator across stages.** → Provide a first-class **saga orchestrator**:
   ordered stages, result threading, and **LIFO rollback across all completed stages** on failure.
3. **Eager/hot tasks.** Parts are built from already-started `Task`s (`SagaPartFactory` awaits a
   passed-in task), so forward calls fire at construction, before the orchestrator decides to run
   them — races with ordering/rollback. → Use **deferred** actions (`Func<Task<...>>`), so the
   orchestrator controls when each runs.
4. **Coupled to legacy `IClientResult`.** → Integrate with the **current** outbound API
   (`IBenzeneMessageSender.SendAsync<TReq,TRes>(topic, req)` → `IBenzeneResult<T>`), using
   `IBenzeneResult.IsSuccess` for the success test. Keep it **transport-agnostic**: a part may wrap
   a message send, an HTTP call, or any `Func<Task>` — not only clients.
5. **In-memory only.** Matches the original (synchronous in-process orchestration). Durability
   (crash recovery for long-running sagas) is an **optional, pluggable** later enhancement — no DB
   opinion baked into the core (consistent with the gap-analysis "stay out of the database" rule).

## 4. Proposed model

```
Saga  ── ordered ──▶  Stage  ── parallel ──▶  Step (forward + compensation)
```

- **`ISagaStep<T>`** (renamed from legacy "Part" to avoid confusion with stages): a deferred
  forward action returning `IBenzeneResult<T>` + a compensation `Func<T, Task>` run only if the
  forward succeeded. Success = `IBenzeneResult.IsSuccess`.
- **Stage**: an N-sized set of steps run with `Task.WhenAll`; succeeds iff all steps succeed;
  on failure compensates its own succeeded steps.
- **`Saga`** (new — the missing orchestrator): runs stages in order, threads a typed **saga
  context** (results of earlier stages available to later stages), and on any stage failure
  compensates **every completed step across every completed stage in reverse order**, returning a
  `SagaResult` (succeeded, or rolled-back-with-cause). Optional whole-saga retry with backoff.

### 4.1 API sketch (the signup example)

```csharp
var saga = Saga.Define(sender)                       // sender: IBenzeneMessageSender
    .Stage(stage => stage                            // Stage 1 — parallel
        .Step(s => s
            .Do(ctx => sender.SendAsync<CreateTenant, TenantCreated>("tenant:create", new(...)))
            .Compensate((ctx, r) => sender.SendAsync<DeleteTenant, Unit>("tenant:delete", new(r.TenantId))))
        .Step(s => s
            .Do(ctx => sender.SendAsync<CreateOktaCo, OktaCoCreated>("okta:create-company", new(...)))
            .Compensate((ctx, r) => sender.SendAsync<DeleteOktaCo, Unit>("okta:delete-company", new(r.CompanyId)))))
    .Stage(stage => stage                            // Stage 2 — uses stage 1 output via ctx
        .Step(s => s
            .Do(ctx => sender.SendAsync<CreateUser, UserCreated>("user:create", new(ctx.Get<TenantCreated>().TenantId)))
            .Compensate((ctx, r) => sender.SendAsync<DeleteUser, Unit>("user:delete", new(r.UserId)))))
    .Stage(stage => stage                            // Stage 3 — uses stage 2 output
        .Step(s => s
            .Do(ctx => sender.SendAsync<CreateRbacRole, RoleCreated>("rbac:create-role", new(ctx.Get<UserCreated>().UserId)))
            .Compensate((ctx, r) => sender.SendAsync<DeleteRbacRole, Unit>("rbac:delete-role", new(r.RoleId)))))
    .Build();

SagaResult result = await saga.RunAsync();           // all-or-nothing; rolls back on any failure
```

- **Rollback**: if stage 3 fails, the orchestrator compensates stage 2's user, then stage 1's Okta
  company and tenant (reverse order), leaving the starting state. `RunAsync` reports the failure +
  that a full rollback occurred (and whether any compensation itself failed — see §7).
- **Retry**: `saga.RunAsync()` is idempotent-by-construction (a clean rollback restores the start
  state), so the caller — or an optional built-in retry policy — can re-run the whole saga.

## 5. Transport-agnosticism

The primary integration is `IBenzeneMessageSender` (lander-to-lander), but a step's `Do`/`Compensate`
are just `Func`s returning `IBenzeneResult<T>` (or a thin adapter for a plain `Task`), so an HTTP
call (`Benzene.Client.Http`) or any custom async action works identically — matching the production
system, which used mostly lander-to-lander but sometimes HTTP.

## 6. Proposed package shape

`Benzene.Saga` (depends on `Benzene.Abstractions.Results`, `Benzene.Clients`):

- `Saga`, `SagaBuilder`, `StageBuilder`, `StepBuilder`
- `ISagaStep`, `SagaStep<T>`, `Stage`, `SagaContext` (typed result bag)
- `SagaResult`, `SagaStatus`, `SagaStepStatus`
- (optional, later) `ISagaStateStore` for durable/resumable sagas
- Test helpers + a worked example under `examples/` mirroring the signup flow.

Fits the vision: orchestration logic is generic and transport-agnostic; the forward/compensation
business logic stays in the app; it leverages the uniform topic+payload call model.

## 7. Open design questions (need a decision before building)

1. **Parallel failure semantics.** Legacy awaits *all* parts in a stage even if one has already
   failed (`Task.WhenAll`), then compensates. Keep that, or **fail-fast** (cancel in-flight siblings
   as soon as one fails)? Fail-fast is faster but leaves in-flight siblings whose outcome is unknown
   — they must still be compensated if they ended up succeeding. Recommend: keep await-all
   (simpler, deterministic) unless you want cancellation.
2. **Compensation failure.** What if an `Undo` itself fails mid-rollback? Options: (a) best-effort —
   log, continue compensating the rest, report a `PartiallyRolledBack` status flagging manual
   intervention; (b) abort rollback and surface immediately. Recommend (a) — you never want a
   compensation failure to strand the *other* compensations.
3. **Result threading.** Typed `SagaContext` bag (`ctx.Get<T>()`) as sketched, or explicit
   result-passing between stages? Bag is ergonomic; explicit is more type-safe. Recommend the bag
   with typed keys.
4. **Durability.** In-process only (matches production), or ship an optional pluggable
   `ISagaStateStore` for crash-recovery/long-running sagas from day one? Recommend in-process first,
   store as a fast-follow, no DB dependency in core.
5. **Retry.** Built-in whole-saga retry policy (count + backoff), or leave entirely to the caller?
   Recommend a thin optional policy; default off.
6. **Naming.** Legacy "Part" vs proposed "Step", and "Step"(parallel group) vs proposed "Stage".
   The proposal renames to Stage → Step (forward+compensation) for clarity. Confirm the vocabulary.
7. **Package boundary.** `Benzene.Saga` depending on `Benzene.Clients` — acceptable, or keep the
   core saga engine client-agnostic (`Func`-only) with a separate `Benzene.Saga.Clients` glue so the
   engine has zero outbound dependency? Recommend the split: engine is `Func`-based; a thin adapter
   package wires `IBenzeneMessageSender`.

## 8. Proposed sequence

1. Agree §7 decisions.
2. `Benzene.Saga` engine (Func-based, N-ary, orchestrator + LIFO rollback + typed context) with
   thorough unit tests (happy path, mid-stage failure, cross-stage rollback, compensation-failure).
3. `IBenzeneMessageSender` adapter (part-from-send helpers, `IsSuccess` mapping).
4. Worked example (`examples/`) mirroring the signup flow; docs + cookbook.
5. (Fast-follow) optional retry policy and pluggable state store.
