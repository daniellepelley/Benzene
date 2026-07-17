# Configurable Batch/Retry Failure Handling — Design Note (2026-07-16)

**Status:** Implemented for `Benzene.Aws.Lambda.Sqs`, `Benzene.Aws.Lambda.Sns`,
`Benzene.Kafka.Core` (both the dispatcher's catch/continue toggle and outcome-gated offset commit),
`Benzene.Azure.Function.Kafka`, `Benzene.Azure.Function.ServiceBus` (**both** the catch/escalate
toggle **and** true per-message `ServiceBusMessageActions` ack, as of 2026-07-17),
`Benzene.Aws.Sqs`'s polling `SqsConsumer`, and (as of 2026-07-17) `Benzene.Aws.Lambda.Kinesis` -
implementing the design at `work/kinesis-batch-failure-handling-design.md`.

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
| `Azure.Function.ServiceBus` (`ServiceBusApplication`/new `ServiceBusBatchApplication`) | Batch, concurrent fan-out (`IsBatched` optional) | Same `CatchExceptions`/`RaiseOnFailureStatus` shape as Kafka above (`ServiceBusOptions`), same no-op-to-real upgrade for `ServiceBusMessageMessageHandlerResultSetter`. **2026-07-17: true per-message `ServiceBusMessageActions` completion shipped too** - `ServiceBusOptions.AckMode` (`AutoComplete` default vs `Explicit`), a new `HandleServiceBusMessages(IAzureFunctionApp, ServiceBusMessageActions, params ServiceBusReceivedMessage[])` overload, and a new `ServiceBusTriggerBatch` request type carrying the messages + actions together (named to avoid colliding with the real `Azure.Messaging.ServiceBus.ServiceBusMessageBatch` SDK type). `ServiceBusBatchApplication` now implements both `IMiddlewareApplication<ServiceBusReceivedMessage[]>` and `IMiddlewareApplication<ServiceBusTriggerBatch>`, sharing one instance registered as two entry points. Complete on success, abandon on failure result or exception - exactly once per message regardless of `CatchExceptions`/`RaiseOnFailureStatus`, which independently control invocation-level cascade. |
| `Aws.Sqs/Consumer/SqsConsumer` (poll-based, distinct from Lambda-triggered SQS) | Batch per poll, whole-batch ack | `SqsConsumerOptions.AckMode` (`WholeBatch` default, unchanged behavior, vs `PerMessage`). Required three things together: a bespoke per-message try/catch loop in `SqsConsumerApplication` (replacing the no-try/catch `MiddlewareMultiApplication` base), `SqsConsumerMessageContext` gaining `IHasMessageResult` (it had no result concept at all before), and `SqsConsumerMessageMessageHandlerResultSetter` upgraded from a no-op to real - `PerMessage` mode calls `DeleteMessageBatchAsync` with only the successful subset instead of `WholeBatch`'s all-or-nothing. |
| `Aws.Lambda.Kinesis` (`KinesisStreamApplication`) | Fan-in (whole batch = one `StreamContext<KinesisEventRecord>`), so containment here means "resume from checkpoint," not "report a subset" - matches Kinesis's own shard-ordered `ReportBatchItemFailures` contract, which only reads the *first* reported failure. Implements `work/kinesis-batch-failure-handling-design.md` in full: a new `KinesisBatchResponse` (hand-rolled, mirrors `Amazon.Lambda.SQSEvents.SQSBatchResponse`'s wire shape), a new internal `KinesisStreamCheckpointer : IStreamCheckpointer<KinesisEventRecord>` wired into the batch's `StreamContext`, and a new `StreamMiddlewareApplication<TEvent,TItem,TResult>` core sibling (`Benzene.Core.Middleware.Streaming`) that `KinesisStreamApplication` now extends instead of the non-result 2-generic version. A pipeline exception is caught inside `KinesisStreamApplication` (logged, not rethrown) so the response still carries whatever was checkpointed before the failure - the checkpointer's resume point is itself the correct failure signal here, unlike the fan-out transports' opt-in `CatchExceptions`. `KinesisLambdaHandler` now writes the response back instead of discarding it (Kinesis event source mapping invocations are synchronous from Lambda's own perspective once `ReportBatchItemFailures` is configured on the trigger). One real behavior change, flagged in the package's own `CLAUDE.md`: a handler that never calls `Checkpointer.CheckpointAsync(...)` now gets a response naming the *first* record on any exception (whole-batch retry) instead of the previous silent no-op via `NullStreamCheckpointer`. |

## Deliberately deferred

None currently - every batch/event transport surveyed this session now has some form of
configurable containment/checkpointing.

`Aws.Lambda.DynamoDb`'s `DynamoDbApplication` (sequential, stop-at-first-failure,
checkpoint-and-redeliver-from-here) is a deliberately different shape from a prior design decision
(DS5) — not a gap, and not a candidate for this same two-knob toggle, since ordering is the whole
point there.

## Why this wasn't built as one shared abstraction

Seven transports now implement some form of this (SQS Lambda, SNS Lambda, Kafka.Core's dispatcher,
Azure Kafka, Azure Service Bus, the SQS poll consumer, and now Kinesis). Per the project's own "no
premature abstraction" convention (`AGENTS.md`), each is still an independent, transport-local
shape rather than a shared interface/base class - the concrete shapes differ enough (an enum for
SQS's single containment knob; two independent bools for the transports with both knobs; a
constructor bool + callback for the dispatcher, since it's shared infrastructure rather than an
`XOptions`-wired transport; a checkpointer + response type for Kinesis's resume-from-checkpoint
model, genuinely different from every other transport's skip-or-cascade shape) that a forced common
interface would add ceremony without removing real duplication. `StreamMiddlewareApplication<TEvent,TItem,TResult>`
(`Benzene.Core.Middleware.Streaming`, added for Kinesis) is the one piece that *is* shared - it's
the natural `TResult`-producing sibling of an existing pattern
(`MiddlewareApplication<TEvent,TContext,TResult>`), not new ceremony for this feature specifically,
and directly reusable for SQS streaming's still-open half of `docs/plans/streaming-plan.md` Phase 2.
