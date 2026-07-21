# Settlement default alignment — proposal for review (1.0)

> **Ask.** Align how every inbound transport settles a message under its **default** config, so
> "a handler that fails does not silently lose the message" holds uniformly. This is a plan-first
> proposal — nothing here is implemented yet. Ordered by value; each item states the change, the
> breaking-ness, and the effort.

## Context — where we actually are (verified 2026-07-21 against source)

The `settlement-contract-1.0` push already did most of this: every adapter that has a
`RaiseOnFailureStatus` flag now **defaults it to `true`**, and every `CatchExceptions` flag defaults
to `false`. So under default config a **thrown exception** is retained/redelivered **everywhere**, and
a **non-throwing failure result** (`IsSuccessful == false`) is retained on the large majority of
adapters. What remains is a small set of genuine gaps plus one cross-transport policy inconsistency.

Two distinct handler outcomes drive settlement — keep them separate:
- **Failure result** — the handler ran and returned `IsSuccessful == false`.
- **Null / unestablished outcome** — no result was recorded (most commonly an **unrouted** message:
  no handler matched the topic).

### Current default behaviour (condensed)

| Transport | Throw | Failure result | Null / unrouted | Note |
|---|---|---|---|---|
| SQS (Lambda) | retain | retain | **retain** (`!= true`) | safest |
| SQS (worker) | retain | retain | **retain** (`!= true`) | `PerMessage` default |
| DynamoDB (Lambda) | retain | retain | **retain** (`!= true`) | stop-at-first-failure |
| Service Bus (worker) | abandon | abandon | **abandon** (`!= true`) | null-abandon is a deliberate fix |
| Service Bus (Functions) | retain | retain (escalate) | ack (`== false`) | escalates under `AutoComplete` |
| SNS / S3 / EventBridge (Lambda) | retain | retain (escalate) | ack (`== false`) | `RaiseOnFailureStatus=true` |
| Queue Storage / Event Grid (Functions) | retain | retain (escalate) | ack (`== false`) | runtime retry/poison/DLQ |
| Kafka (Lambda) | retain | retain (`== false`) | **ack** | null-ack deliberate (no per-record DLQ) |
| Kafka (Functions) | retain | retain (escalate) | ack (`== false`) | offset not committed on throw |
| RabbitMQ (worker) | nack | nack (`== false`) | **ack** | null-ack documented/tested |
| **Kafka (worker)** | see below | **ack — always committed** | ack | **GAP 1** |
| **Event Hub (Functions)** | retain (batch) | **ack — flag inert on default path** | ack | **GAP 2** |
| Event Hub (worker) | see note | retain (escalate, `== false`) | ack | flag=true; `CatchHandlerExceptions=true` caveat |
| Kinesis (Lambda) | retain (resume) | **ack — results never inspected** | ack | fan-in streaming |
| Cosmos (Functions) | retain (lease) | **ack — no per-doc result** | ack | fan-in streaming |

---

## Tier A — genuine silent-loss gaps to fix (safe-by-default already the stated intent)

### A1. Kafka self-hosted worker never escalates a non-throwing failure result
`Benzene.Kafka.Core/BenzeneKafkaWorker.cs`. There is **no `RaiseOnFailureStatus`** (unlike the Kafka
Lambda handler and the Azure Functions Kafka trigger, which both have it defaulting `true`).
- Default `CommitOnlyOnSuccess=false`: Confluent auto-stores the offset when `Consume` returns,
  before the handler runs — any non-success is committed (at-most-once).
- Even `CommitOnlyOnSuccess=true`: `StoreOffset(consumeResult)` (`BenzeneKafkaWorker.cs:208`) is called
  right after `HandleAsync` returns **without inspecting `IsSuccessful`** — so a *throw* is protected
  (it never reaches line 208), but a **non-throwing failure result is always committed** → silent loss.
- Dead-lettering (`KafkaDeadLetterOptions`) only catches *exceptions*, not failure results, so it
  doesn't cover this either.

**Change (recommended).** Add `BenzeneKafkaConfig.RaiseOnFailureStatus` (default `true`) that routes a
non-throwing failure result through the **same path as a fault**: under dead-lettering it retries/
dead-letters; under `CommitOnlyOnSuccess=true` it does **not** `StoreOffset` (redelivery); under the
default auto-store config it can only log a warning (the offset is already stored — same inherent
limitation the docs already state, so surface it). This makes the three Kafka adapters consistent.
**Breaking:** none (additive, safe default). **Effort:** small–medium (worker + config + a unit test;
respects the existing `CommitOnlyOnSuccess`/`CatchHandlerExceptions` startup-validation interactions).

### A2. Azure Functions Event Hub `RaiseOnFailureStatus` is inert on the default path
`Benzene.Azure.Function.EventHub/EventHubApplication.cs:84`. The escalation guard reads
`EventHubContext.MessageResult`, but on the documented default `UseBenzeneMessage` envelope path the
handler runs on the **inner** `BenzeneMessageContext` with its response suppressed, so
`EventHubContext.MessageResult` is never populated and the guard never fires — a non-throwing failure
is checkpointed past (silent loss), despite the flag defaulting `true`.

**Change.** Propagate the inner `BenzeneMessageContext`'s result to the outer `EventHubContext` (the
same "surface the inner result" wiring the other envelope hosts use) so the escalation guard sees a
real outcome. **Breaking:** none (makes the already-documented default actually work). **Effort:**
small–medium (one wiring fix + a pipeline test that a failure result on the envelope path escalates).

---

## Tier B — cross-transport policy: null / unrouted outcome (`!= true` vs `== false`)

An **unrouted** message (no handler matched the topic → null result) is silently **acked/lost** on
every `== false` adapter, but **retained** (→ DLQ / dequeue-count / abandon) on the `!= true` ones
(SQS, Service Bus, DynamoDB). The Service Bus worker's own code comment documents this as a real
prior bug it fixed (`== false` "completed a null result, dropping a message whose outcome was never
established"). The same silent-drop is still live on SNS/S3/EventBridge/Queue Storage/Event Grid/
Event Hub/RabbitMQ/Kafka.

**Recommended policy.** A null/unestablished outcome is **not** success — treat it as failure
(retain/escalate) **wherever a redelivery backstop exists**, and ack it **only** where retaining it
would be an unbreakable poison loop:
- **Switch to `!= true` (retain/escalate on null):** SNS, S3, EventBridge, Queue Storage, Event Grid
  — all have a runtime DLQ / dequeue-count / retry backstop, so an unrouted message goes there after N
  attempts instead of vanishing. RabbitMQ too (it has a DLX + bounded single requeue).
- **Keep `== false` (ack on null) — deliberate carve-out:** Kafka (all three) and the fan-in streams
  (Kinesis, Cosmos, Event Hub) — no per-record DLQ, so retaining an unrouted record replays the
  partition/shard forever. Document this as the one intentional exception.

**Breaking:** behavioural — an unrouted message that used to be silently dropped now surfaces
(exception → runtime retry/poison). That is the *point*, but it's a behaviour change, so it needs
your sign-off and a release note. **Effort:** small per adapter (flip `== false` → `!= true` in each
escalation guard + a test), medium in aggregate.

---

## Tier C — inherent to fan-in streaming (document, don't "fix")

Kinesis (Lambda) and Cosmos (Functions) hand the **whole batch** to one handler as a stream and never
inspect a per-record `IMessageResult`; "fail this record" is expressed by **throwing** (or, for
Kinesis, not checkpointing past it). A per-record non-throwing failure result has nowhere to go in a
fan-in model. This is a documented contract, not a gap — the only action is a one-line clarification
in each package's CLAUDE.md that a fan-in handler signals failure by throwing / withholding the
checkpoint, not by returning a failure result.

---

## Suggested cut

- **Do now (bugs, non-breaking, safe default):** **A1** (Kafka worker `RaiseOnFailureStatus`) and
  **A2** (Functions Event Hub inert flag). These are the only two genuine silent-loss defaults left.
- **Decide, then do (behavioural, needs sign-off):** **B** — unify null/unrouted handling to
  `!= true` on the DLQ-backed adapters, keeping the documented Kafka/streaming carve-out.
- **Docs only:** **C** — clarify the fan-in "throw to fail" contract for Kinesis/Cosmos.

Tell me which tiers to action. A1 + A2 I can implement immediately on the usual test-first,
incremental-push track. B I'd want your explicit yes on the policy (retain-unrouted-where-DLQ-exists)
before flipping the guards, since it changes what happens to an unrouted message in production.
