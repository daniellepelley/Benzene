# Configurable Batch/Retry Failure Handling — Design Note (2026-07-16)

**Status:** Implemented for `Benzene.Aws.Lambda.Sqs`, `Benzene.Aws.Lambda.Sns`,
`Benzene.Kafka.Core` (both the dispatcher's catch/continue toggle and outcome-gated offset commit),
`Benzene.Azure.Function.Kafka`, `Benzene.Azure.Function.ServiceBus` (the
catch/escalate toggle only, not true per-message `ServiceBusMessageActions` partial-ack), and
`Benzene.Aws.Sqs`'s polling `SqsConsumer`. `Benzene.Aws.Lambda.Kinesis` remains deliberately
deferred - see its entry below for why.

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

## Shipped, beyond the original SQS/SNS pair

| Transport | Shape | What shipped |
|---|---|---|
| `Kafka.Core` (`BoundedConcurrentDispatcher`, shared with `Benzene.SelfHost.Http`) | Single message, bounded-concurrent poll loop | `BenzeneKafkaConfig.CatchHandlerExceptions` (default `true`, unchanged behavior) - `false` rethrows after logging, which ends that lane; `BenzeneKafkaWorker` wires the dispatcher's new `onFault` callback to stop the whole worker (a dead lane's channel otherwise silently deadlocks `EnqueueAsync` for that key once it fills). Additionally, `BenzeneKafkaConfig.CommitOnlyOnSuccess` (default `false`, unchanged behavior - Confluent.Kafka's own auto-store-on-consume default) - `true` sets `ConsumerConfig.EnableAutoOffsetStore = false` and has `BenzeneKafkaWorker` call `IConsumer.StoreOffset` itself, only after the handler succeeds, for at-least-once redelivery on failure/crash. Requires `CatchHandlerExceptions = false` and `PreserveOrderPerPartition = true` (enforced at `StartAsync` via `InvalidOperationException`) because `StoreOffset` is a last-write-wins watermark with no gap tracking (confirmed against librdkafka's C source) - a swallowed exception or out-of-order handling could let a later, successful message advance the watermark past an earlier failed one before its offset was ever stored. |
| `Azure.Function.Kafka` (`KafkaApplication`/new `KafkaBatchApplication`) | Batch, concurrent fan-out | `KafkaOptions.CatchExceptions`/`RaiseOnFailureStatus` (both default `false`, unchanged behavior), same shape as `SnsOptions`. No platform partial-ack exists for Kafka triggers (confirmed: no companion action type in the isolated-worker SDK), so containment here means swallow-vs-cascade, not report-a-subset. `KafkaMessageMessageHandlerResultSetter` was upgraded from a true no-op to actually recording `MessageResult`, so `RaiseOnFailureStatus` has something to read. |
| `Azure.Function.ServiceBus` (`ServiceBusApplication`/new `ServiceBusBatchApplication`) | Batch, concurrent fan-out (`IsBatched` optional) | Same `CatchExceptions`/`RaiseOnFailureStatus` shape as Kafka above (`ServiceBusOptions`), same no-op-to-real upgrade for `ServiceBusMessageMessageHandlerResultSetter`. True per-message `ServiceBusMessageActions` completion (the SDK does support this, via `AutoCompleteMessages = false` plus a bound `ServiceBusMessageActions` parameter) is **not** part of this - it needs an additive change to the trigger function's own signature, which is a distinct, larger unit of work than this toggle. |
| `Aws.Sqs/Consumer/SqsConsumer` (poll-based, distinct from Lambda-triggered SQS) | Batch per poll, whole-batch ack | `SqsConsumerOptions.AckMode` (`WholeBatch` default, unchanged behavior, vs `PerMessage`). Required three things together: a bespoke per-message try/catch loop in `SqsConsumerApplication` (replacing the no-try/catch `MiddlewareMultiApplication` base), `SqsConsumerMessageContext` gaining `IHasMessageResult` (it had no result concept at all before), and `SqsConsumerMessageMessageHandlerResultSetter` upgraded from a no-op to real - `PerMessage` mode calls `DeleteMessageBatchAsync` with only the successful subset instead of `WholeBatch`'s all-or-nothing. |

## Deliberately deferred

| Transport | Why |
|---|---|
| `Aws.Lambda.Kinesis` (`KinesisStreamApplication`) | Confirmed **not** a small change on investigation: it fans the entire batch into one `StreamContext`/one pipeline run (fan-in, by design, for windowing/aggregation) - there is no per-record dispatch loop anywhere in Benzene's streaming code to hook a result into, and `StreamContext<TItem>` carries no result concept at all. AWS's Kinesis event source mapping does support `ReportBatchItemFailures`, but Benzene has zero scaffolding toward it (no `KinesisBatchResponse`, no `Amazon.Lambda.KinesisEvents` dependency - `KinesisEventRecord` is hand-rolled). Doing this properly means redesigning the streaming abstraction's per-item identity tracking first (`StreamContext`, `StreamOperators.Window`/`PartitionBy`, `KinesisStreamApplication`'s whole shape) - 5-7 files and a real design decision, not an add-on. |
| `Azure.Function.ServiceBus` true `ServiceBusMessageActions` partial-ack | See the shipped-toggle row above - reachable (the SDK supports it), additive (existing trigger signatures keep working), but requires new `HandleServiceBusMessages` overloads and threading `ServiceBusMessageActions` into `ServiceBusContext`/the result setter - a distinct unit of work from the catch/escalate toggle that shipped. |

`Aws.Lambda.DynamoDb`'s `DynamoDbApplication` (sequential, stop-at-first-failure,
checkpoint-and-redeliver-from-here) is a third, deliberately different shape from a prior design
decision (DS5) — not a gap, and not a candidate for this same two-knob toggle, since ordering is
the whole point there.

## Why this wasn't built as one shared abstraction

Six transports now implement some form of this (SQS Lambda, SNS Lambda, Kafka.Core's dispatcher,
Azure Kafka, Azure Service Bus, the SQS poll consumer). Per the project's own "no premature
abstraction" convention (`AGENTS.md`), each is still an independent, transport-local `XOptions`
class with a transport-local exception type (`SqsBatchProcessingException`,
`SnsMessageProcessingException`, `KafkaMessageProcessingException`,
`ServiceBusMessageProcessingException`) rather than a shared interface/base class - the concrete
shapes differ enough (an enum for SQS's single containment knob; two independent bools for the
transports with both knobs; a constructor bool + callback for the dispatcher, since it's shared
infrastructure rather than an `XOptions`-wired transport) that a forced common interface would add
ceremony without removing real duplication. If a seventh transport picks up the same shape, that's
the point to revisit factoring out a shared abstraction.
