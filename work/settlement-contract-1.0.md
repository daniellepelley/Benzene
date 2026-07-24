# Settlement contract: safe-by-default (1.0) — breaking change

**Date:** 2026-07-21. **Decision:** maintainer-approved (Tier-2 #1, "flip all to safe").

## What changed

Across every async/event transport, a message handler that **returns** a failure `IBenzeneResult`
(e.g. `BenzeneResult.ServiceUnavailable(...)`) — rather than throwing — is now **not settled by
default**: it is escalated so the platform redelivers it (at-least-once), instead of being silently
acked/completed/checkpointed-past (at-most-once). This aligns every transport with the SQS consumer,
which already defaulted to safe (`PerMessage`).

Mechanically, `RaiseOnFailureStatus` flipped from `false` → **`true`** on:

| Package | Options type |
|---|---|
| `Benzene.Aws.Lambda.Sns` | `SnsOptions` |
| `Benzene.Aws.Lambda.S3` | `S3Options` |
| `Benzene.Aws.Lambda.EventBridge` | `EventBridgeOptions` |
| `Benzene.Azure.Function.Kafka` | `KafkaOptions` |
| `Benzene.Azure.Function.QueueStorage` | `QueueStorageOptions` |
| `Benzene.Azure.Function.EventGrid` | `EventGridOptions` |
| `Benzene.Azure.Function.EventHub` | `EventHubOptions` |
| `Benzene.Azure.EventHub` (self-host worker) | `BenzeneEventHubConfig` |
| `Benzene.GoogleCloud.Functions.PubSub` | `PubSubOptions` |
| `Benzene.Azure.Function.ServiceBus` | `ServiceBusOptions` |

A returned failure now throws that transport's `*MessageProcessingException`, which (because
`CatchExceptions` still defaults to `false`) propagates and fails the invocation → the platform's
own retry/redelivery kicks in.

## Caveat: the self-hosted *stream* workers remain at-most-once by default

The flip above makes every **queue-shaped** transport safe-by-default, and it is effective on the
Function/Lambda triggers (including the Event Hub and Kafka *Function* triggers) because their
`CatchExceptions` defaults `false`, so the escalated throw propagates and the platform redelivers.

It is **not** effective, on its own, for the two self-hosted **stream** workers, which keep their
pre-existing "keep the stream flowing" default:

- **`Benzene.Azure.EventHub`** — although `RaiseOnFailureStatus` flipped to `true` (table above),
  `CatchHandlerExceptions` **also** defaults `true`, so the escalated `EventHubMessageProcessingException`
  is caught and logged, the worker continues, and the next successful event checkpoints past the failed
  one — effectively **at-most-once**. At-least-once requires **`CatchHandlerExceptions = false`** as well
  (the worker then stops at the failure without checkpointing, so a restart redelivers).
- **`Benzene.Kafka.Core`** — not in the flip table (it settles via offset commit, not `RaiseOnFailureStatus`).
  `CommitOnlyOnSuccess` defaults `false`, so offsets auto-commit regardless of outcome — also **at-most-once**.
  At-least-once requires **`CommitOnlyOnSuccess = true`** (which in turn requires `CatchHandlerExceptions = false`).

This is intrinsic to streams: a partition has no per-message ack/abandon, so "don't lose a failed record"
means "stop the worker and don't advance the offset/checkpoint", which is too aggressive to be a default
for a long-running consumer (one poison record halts all processing). The queue transports and the
Lambda/Functions stream *triggers* don't have this tension — the platform redelivers the un-advanced
work without any single process halting. See the per-transport table and the streaming callout in
[Capability Matrix](../docs/capability-matrix.md).

## Already safe before this change (no flip needed)

- **`Benzene.Aws.Sqs` consumer** — `AckMode` already defaulted to `PerMessage` (only successful
  messages deleted).
- **`Benzene.Azure.ServiceBus` self-host worker** — `AckMode` already defaulted to `Explicit`.
- **`Benzene.RabbitMq`** — `AckMode` already defaulted to `Explicit`.
- **Kinesis / DynamoDB Streams** — settle via `ReportBatchItemFailures` (the batch response), not
  `RaiseOnFailureStatus`; see the separate Kinesis `AutoCheckpointOnSuccess` change (Tier-2 #2).

## Azure Service Bus — no trigger reconfiguration required

Service Bus keeps `AckMode = AutoComplete` as its default. Under AutoComplete the Functions host
completes on a normal return and **abandons on a thrown exception**, so the now-default
`RaiseOnFailureStatus = true` escalation (a thrown `ServiceBusMessageProcessingException`) causes the
host to abandon the message → redelivery. **You do not need `AutoCompleteMessages = false`** for
safe-by-default. `AckMode = Explicit` remains an opt-in for real per-message complete/abandon control
(that path still requires `AutoCompleteMessages = false` and binding `ServiceBusMessageActions`).

One refinement shipped with this flip: the `RaiseOnFailureStatus` escalation is now skipped under
`AckMode = Explicit` (`ServiceBusApplication`: `if (!explicitAck && ...)`). Under Explicit the message
is already abandoned on failure by Benzene itself, so throwing again would be redundant and would
needlessly fail the whole (possibly batched) invocation even though each message was settled
individually. The two mechanisms are now non-overlapping: AutoComplete relies on throw→host-abandon,
Explicit relies on its own abandon.

## Migration — how to restore the old behavior

If you deliberately relied on at-most-once ("a returned failure result is accepted, the message is
not retried"), set the flag back to `false` at the call site:

```csharp
// AWS Lambda event sources (SNS shown; S3/EventBridge identical)
app.UseAwsLambda(events => events
    .UseSns(sns => sns.UseMessageHandlers(),
        options => options.RaiseOnFailureStatus = false));

// Azure Functions triggers (Service Bus shown; Kafka/QueueStorage/EventGrid/EventHub identical)
app.UseServiceBus(sb => sb.UseMessageHandlers(),
    options => options.RaiseOnFailureStatus = false);

// Self-hosted Event Hub worker
config.RaiseOnFailureStatus = false;
```

## Why

Theme 1 of the cloud/transport review: "silent success on a non-throwing failure" was the dominant
correctness footgun — the same handler shape lost data or not depending on the transport, and the
unsafe mode was the default. Making the safe mode the default (with a one-line opt-out) is the right
default for a 1.0 and removes the cross-transport inconsistency. The tradeoff is that at-least-once
requires idempotent handlers and a DLQ/redrive policy for poison messages — long-documented in
[Idempotency](../docs/cookbooks/idempotency.md) and the
[Capability Matrix](../docs/capability-matrix.md).
