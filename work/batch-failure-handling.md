# Configurable Batch/Retry Failure Handling — Design Note (2026-07-16)

**Status:** Implemented for `Benzene.Aws.Lambda.Sqs` and `Benzene.Aws.Lambda.Sns`. Documented here
as the convention for the other transports listed below, not yet implemented for them.

## Context

The maintainer's ask: SQS Lambda batch processing already reports partial batch failures (only
the messages that failed get redelivered) — the right default — but a user should be able to opt
into failing the whole batch instead. SNS Lambda processing should let a user decide whether
handler exceptions are caught or cascade to fail the invocation, and whether a non-exception
failure result should be escalated into a thrown exception to trigger an SNS retry. Both should be
configurable, with the AWS best-practice behavior as the default — "the design philosophy of
Benzene." A wider survey across every other Benzene batch/event transport was done to find a
reusable shape rather than solving this as a one-off for SQS/SNS. See the commit(s) around
`SqsOptions`/`SnsOptions` for the shipped implementation; this note captures the vocabulary and the
survey findings for whoever tackles the rest of the list.

## The vocabulary

Two independent, orthogonal knobs recur across every transport that delivers more than one
message per invocation, or that can fail without throwing:

1. **Containment** — does one item's failure (exception or unsuccessful result) get contained and
   reported per-item, leaving the rest of the batch/invocation unaffected, or does it cascade to
   fail the *whole* unit of work (batch/invocation), triggering the transport's native whole-batch
   retry/redelivery mechanics?
2. **Escalation** — for transports whose only failure signal to the platform is "did the
   invocation throw" (most of them — SNS, EventBridge, Kinesis, Kafka), does a handler returning a
   non-exception failure result get promoted into a thrown exception (so the platform's retry
   mechanism notices it), or is it accepted as if it were a success?

Both knobs default to whatever is the platform's best practice for that transport — which, in
every case examined, happens to already be today's *implicit*, hardcoded, undocumented behavior.
This is why the SQS/SNS change is purely additive: making the choice explicit and overridable
doesn't change a single default.

## Shipped: `Benzene.Aws.Lambda.Sqs` / `Benzene.Aws.Lambda.Sns`

- **SQS** (containment only — SQS's own `SQSBatchResponse.BatchItemFailures` already gives
  per-item containment natively): `SqsOptions.BatchFailureMode`, an enum defaulting to
  `PartialBatchFailure` (today's behavior). `FailWholeBatch` throws a new
  `SqsBatchProcessingException` when any record failed, instead of returning the partial
  response — the whole invocation fails, so SQS retries/redrives every message in the batch.
- **SNS** (both knobs — SNS-to-Lambda has no partial-failure mechanism at all, so containment
  here means "swallow vs. cascade" rather than "report a subset"): `SnsOptions.CatchExceptions`
  (containment, default `false`/cascade) and `SnsOptions.RaiseOnFailureStatus` (escalation,
  default `false`/don't escalate). A new `SnsMessageProcessingException` is what an escalated
  failure result becomes; `CatchExceptions` governs both real exceptions and escalated ones
  uniformly, since the escalation throw happens inside the same try block.

Both are plain mutable `XOptions` POCOs wired via `Action<TOptions> configure = null` on the
existing `UseSqs`/`UseSns` extension methods — matching the dominant convention already used
elsewhere in the codebase (`AvroOptions`/`AddAvro`, `BenzeneGrpcOptions`/`AddBenzeneGrpc`), not the
scoped-DI-holder pattern (`PresetTopicHolder`) which solves a different problem shape (a
per-message value handed from one middleware to a later one in the *same* pipeline run, not a
policy decided once at wiring time and consumed by the `Application` class that sits above the
pipeline).

## Surveyed, not yet implemented

None of these have a runtime toggle today. Listed with what would need to change, using the same
two-knob vocabulary:

| Transport | Shape | Containment today | Escalation today | What a fix would touch |
|---|---|---|---|---|
| `Aws.Lambda.Kinesis` (`KinesisStreamApplication`) | Batch, but fans **in** to one `StreamContext`/one pipeline run, not per-record | Whole-invocation throw on any exception; no `KinesisBatchResponse`-equivalent exists in the AWS SDK surface Benzene targets | Not observable — `StreamContext<TItem>` has no result concept at all | Would need a genuine per-record result-tracking design first (bigger lift than SQS/SNS - there's no existing capture point to escalate from) |
| `Aws.Lambda.EventBridge` (`EventBridgeApplication`) | Single event, not a batch | Whole-invocation throw (only outcome possible for a single event) | Captured on `EventBridgeContext.MessageResult` but never read — the void `MiddlewareApplication.HandleAsync` doesn't check it | Add the same escalation-only knob SNS got (no containment knob needed - there's only one item) |
| `Kafka.Core` (`BoundedConcurrentDispatcher`) | Single message, bounded-concurrent poll loop | Hardcoded catch-log-continue per message at the dispatcher; offset auto-commits on a timer regardless of outcome | Captured on `KafkaRecordContext.MessageResult` but never read | Both knobs apply; escalation is complicated by decoupled auto-commit — making offset advancement actually depend on the outcome is a bigger, separate change from just adding a toggle |
| `Azure.Function.Kafka` / `Azure.Function.ServiceBus` (`MiddlewareMultiApplication`-based) | Batch, concurrent fan-out (Kafka always; Service Bus optional via `IsBatched`) | Exception in one record cascades to fail the whole trigger invocation via `Task.WhenAll`; no Azure-Functions-equivalent of `batchItemFailures` wired up | Result setters are explicit, documented no-ops | Both knobs apply; Azure's own per-message ack idioms (`ServiceBusMessageActions.CompleteMessageAsync`/`AbandonMessageAsync`) would need wiring up for true containment, not just a rethrow/no-rethrow toggle |
| `Aws.Sqs/Consumer/SqsConsumer` (poll-based, distinct from Lambda-triggered SQS) | Batch per poll, whole-batch ack | Whole batch left un-deleted (not partially) if `HandleAsync` throws; deletes the **entire** batch unconditionally on non-throw, ignoring failure-status entirely | N/A — deletion happens regardless of result | Both knobs apply; would need per-message `DeleteMessageAsync` instead of `DeleteMessageBatchAsync` for real containment, which is a bigger structural change than SQS Lambda's already-per-record model |

`Aws.Lambda.DynamoDb`'s `DynamoDbApplication` (sequential, stop-at-first-failure,
checkpoint-and-redeliver-from-here) is a third, deliberately different shape from a prior design
decision (DS5) — not a gap, and not a candidate for this same two-knob toggle, since ordering is
the whole point there.

## Why this wasn't built as one shared abstraction

Only two transports need this today. Per the project's own "no premature abstraction" convention
(`AGENTS.md`), the two implementations are independent, transport-local `XOptions` classes with
transport-local exception types (`SqsBatchProcessingException`, `SnsMessageProcessingException`)
rather than a shared interface/base class. This note is the placeholder for that shared shape,
written down once there are enough real implementations to justify factoring it out — likely once
2-3 of the transports above pick it up.
