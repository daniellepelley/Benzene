# Kinesis Per-Record Failure Containment — Design Proposal (2026-07-17)

**Status:** Design proposal only — no code changes accompany this document.

## 1. Correcting the prior scope estimate

`work/batch-failure-handling.md`'s "Deliberately deferred" table (written 2026-07-16) said Kinesis
containment needs "redesigning the streaming abstraction's per-item identity tracking first
(`StreamContext`, `StreamOperators.Window`/`PartitionBy`, `KinesisStreamApplication`'s whole shape)
- 5-7 files and a real design decision." Having now read `Benzene.Core.Middleware/Streaming/*` and
`docs/plans/streaming-plan.md` directly: **that estimate was made without accounting for work
already done.** The streaming engine already ships `IStreamCheckpointer<TItem>`/
`NullStreamCheckpointer<TItem>` — a generic, transport-agnostic per-item checkpoint hook — and
`Benzene.Aws.Lambda.Kinesis/CLAUDE.md` already flags exactly this gap: *"Checkpointing uses the
streaming engine's `NullStreamCheckpointer` default... A real `IStreamCheckpointer<KinesisEventRecord>`
keyed on `SequenceNumber` is future (streaming Phase 2, see `docs/plans/streaming-plan.md`)."* This
is not a redesign — it's implementing a checkpointer against an abstraction that was built with
this exact use case in mind, plus one new response type. Real scope: **4 files touched, 1 new core
type (optional), no redesign of `StreamContext`/the stream operators.**

## 2. Why Kinesis's failure model fits the checkpoint abstraction better than a fan-out model

This is the key insight the original "needs per-record dispatch" framing missed: **Kinesis's own
`ReportBatchItemFailures` contract for the Lambda event source mapping is not "skip the bad
records," it's "resume from a checkpoint."** Unlike SQS (where `BatchItemFailures` names the
specific messages to redrive, all others treated as done), a Kinesis (and DynamoDB Streams) event
source mapping reads only the *first* `itemIdentifier` in the returned list and retries **every
record from that sequence number to the end of the batch** — because Kinesis records are ordered
within a shard and there is no independent per-record redelivery. This is precisely
"resume-from-last-checkpoint" semantics, which is exactly what `IStreamCheckpointer<TItem>` already
models. A fan-out-per-record design would have been fighting Kinesis's actual retry contract, not
serving it.

## 3. Concrete design

### 3.1 `KinesisBatchResponse` (new, `Benzene.Aws.Lambda.Kinesis`)

Mirrors `Benzene.Aws.Lambda.Sqs.SQSBatchResponse`'s shape (same package convention: Benzene hand-rolls
its own dependency-free event/response types here rather than taking `Amazon.Lambda.KinesisEvents`,
per this package's own CLAUDE.md):

```csharp
public class KinesisBatchResponse
{
    public KinesisBatchResponse(string? failedSequenceNumber = null)
    {
        BatchItemFailures = failedSequenceNumber == null
            ? new List<BatchItemFailure>()
            : new List<BatchItemFailure> { new() { ItemIdentifier = failedSequenceNumber } };
    }

    public List<BatchItemFailure> BatchItemFailures { get; }

    public class BatchItemFailure
    {
        public string ItemIdentifier { get; set; }
    }
}
```

Deliberately a single-optional-failure constructor, not a list-builder like SQS's — documented
clearly (with a doc-comment pointing at this design note) that only the first entry is meaningful to
AWS for Kinesis/DynamoDB Streams, unlike SQS where every entry matters.

### 3.2 `KinesisStreamCheckpointer : IStreamCheckpointer<KinesisEventRecord>` (new)

```csharp
internal class KinesisStreamCheckpointer : IStreamCheckpointer<KinesisEventRecord>
{
    private readonly List<KinesisEventRecord> _records;
    private int _lastCheckpointedIndex = -1;

    public KinesisStreamCheckpointer(List<KinesisEventRecord> records) => _records = records;

    public Task CheckpointAsync(KinesisEventRecord lastProcessed)
    {
        _lastCheckpointedIndex = _records.IndexOf(lastProcessed);
        return Task.CompletedTask;
    }

    // The record to resume from, if the stream didn't finish - null if every record was
    // checkpointed (or the batch was empty).
    public string? FirstUncheckpointedSequenceNumber =>
        _lastCheckpointedIndex + 1 < _records.Count
            ? _records[_lastCheckpointedIndex + 1].Kinesis.SequenceNumber
            : null;
}
```

Takes the original ordered record list (not just the latest checkpoint) so it can compute "the
next record after the last one we know succeeded" - the exact value Kinesis's retry contract needs.

### 3.3 `KinesisStreamApplication` (modify)

Needs to become response-producing. Two options, in order of preference:

**Option A (preferred): add `StreamMiddlewareApplication<TEvent, TItem, TResult>` to
`Benzene.Core.Middleware.Streaming`** - the natural sibling of the existing two-generic-parameter
version, mirroring how `MiddlewareApplication<TEvent, TContext>` already has a
`MiddlewareApplication<TEvent, TContext, TResult>` sibling for exactly this reason:

```csharp
public class StreamMiddlewareApplication<TEvent, TItem, TResult>(
    IMiddlewarePipeline<StreamContext<TItem>> pipeline,
    Func<TEvent, StreamContext<TItem>> mapper,
    Func<StreamContext<TItem>, TResult> resultMapper)
    : MiddlewareApplication<TEvent, StreamContext<TItem>, TResult>(pipeline, mapper, resultMapper);
```

This isn't Kinesis-specific scaffolding - `docs/plans/streaming-plan.md`'s own Phase 2 already
names a second future consumer of the identical shape ("SQS mapping windowed failures back to
`SQSBatchResponse`" for a hypothetical `UseSqsStream`), so this is completing an existing pattern
pair, not inventing a one-off abstraction for a single caller.

`KinesisStreamApplication` then becomes:

```csharp
public class KinesisStreamApplication : StreamMiddlewareApplication<KinesisEvent, KinesisEventRecord, KinesisBatchResponse>
{
    public KinesisStreamApplication(IMiddlewarePipeline<StreamContext<KinesisEventRecord>> pipeline)
        : base(
            new TransportMiddlewarePipeline<StreamContext<KinesisEventRecord>>("kinesis", pipeline),
            @event => BuildContext(@event.Records),
            context => BuildResponse((KinesisStreamCheckpointer)context.Checkpointer))
    { }

    private static StreamContext<KinesisEventRecord> BuildContext(List<KinesisEventRecord> records)
        => new(ToAsyncEnumerable(records), checkpointer: new KinesisStreamCheckpointer(records ?? new()));

    private static KinesisBatchResponse BuildResponse(KinesisStreamCheckpointer checkpointer)
        => new(checkpointer.FirstUncheckpointedSequenceNumber);

    // ToAsyncEnumerable unchanged from today
}
```

**Option B (fallback, no core change): a bespoke, Kinesis-local application** implementing
`IMiddlewareApplication<KinesisEvent, KinesisBatchResponse>` directly (not extending
`StreamMiddlewareApplication<TEvent,TItem>` at all), duplicating the handful of lines
`MiddlewareApplication<TEvent,TContext,TResult>` already provides. Lower blast radius (touches only
`Benzene.Aws.Lambda.Kinesis`, no `Benzene.Core.Middleware` change), at the cost of not being reusable
if/when SQS streaming needs the identical shape later. Worth using instead of Option A only if a
reviewer prefers not to touch `Benzene.Core.Middleware` for a single current consumer.

**A behavioral question either option must answer**: if the pipeline throws partway through (a
stream step's exception propagates out of `HandleAsync`), does `KinesisLambdaHandler` catch it and
still return the partial `KinesisBatchResponse` (containing the resume point), or let the exception
cascade and fail the whole invocation (losing the partial-resume information, but matching
`CatchHandlerExceptions`-style transports' "cascade is the safe default" precedent from
`work/batch-failure-handling.md`)? **Recommendation: catch it in `KinesisStreamApplication` and
always return the computed `KinesisBatchResponse`** - unlike the fan-out transports' `CatchExceptions`
toggle (which decides whether ONE failure should be allowed to affect other independent items),
here the checkpointer's resume-point IS the correct signal, and swallowing the exception without
returning it loses no information (a `KinesisMessageProcessingException`-style escalation option, if
wanted later, is additive on top of this baseline).

### 3.4 `KinesisLambdaHandler` (modify)

Same shape change `SqsLambdaHandler` already went through: hold
`IMiddlewareApplication<KinesisEvent, KinesisBatchResponse>` instead of
`IMiddlewareApplication<KinesisEvent>`, and in `HandleFunction`, call `MapResponse(context, response)`
with the result instead of discarding it (Kinesis event source mapping invocations are synchronous
from Lambda's own perspective once `ReportBatchItemFailures` is configured on the trigger, even
though today's fire-and-forget `KinesisLambdaHandler` never writes one).

### 3.5 `Extensions.UseKinesisStream` — no signature change

The public `UseKinesisStream(Action<IMiddlewarePipelineBuilder<StreamContext<KinesisEventRecord>>> action)`
extension is unaffected - the checkpointer wiring is entirely internal to `KinesisStreamApplication`.
**One real behavior change worth flagging to users in the CHANGELOG entry**: today, a handler that
never calls `context.Checkpointer.CheckpointAsync(...)` gets a silent no-op
(`NullStreamCheckpointer`). After this change, the same handler gets a real checkpointer that (if
never called) reports the *entire* batch as needing retry from the first record on any exception -
purely additive/more-correct behavior, but worth a documented callout since it changes what AWS
does on retry even though no code needs to change to adopt it.

## 4. What this does not solve

- **Handlers must opt into calling `Checkpointer.CheckpointAsync(record)`** at the right point in
  their own stream-processing logic (after a window/aggregate/individual record succeeds) for the
  resume point to be meaningfully ahead of "start of batch." This document doesn't add an automatic
  `UseCheckpointAfterEach()` operator - that's explicitly already-planned scope in
  `docs/plans/streaming-plan.md` Phase 2, not reinvented here. Recommend implementing that operator
  alongside this Kinesis work, since it's what makes the checkpointer useful without every handler
  hand-writing checkpoint calls - see §5.
- **No escalation toggle** (`RaiseOnFailureStatus`-equivalent) for a non-exception failure result -
  out of scope for this pass per §3.3's recommendation; additive later if wanted.
- **DynamoDB Streams** (`Benzene.Aws.Lambda.DynamoDb`) has the identical `ReportBatchItemFailures`
  resume-point contract as Kinesis (both are the "shard-ordered stream" event source mapping
  family) - per `work/batch-failure-handling.md`, `DynamoDbApplication` is a deliberately different,
  already-correct shape (sequential, stop-at-first-failure, checkpoint-and-redeliver-from-here, per
  design decision DS5) - this document doesn't touch it, just notes the family resemblance for
  whoever next reviews DynamoDB Streams behavior.

## 5. Recommended implementation order

1. `KinesisBatchResponse` (no dependencies, testable in isolation).
2. `KinesisStreamCheckpointer` (no dependencies beyond `KinesisEventRecord`, testable in isolation -
   unit test: checkpointing record N, then asking for `FirstUncheckpointedSequenceNumber`, returns
   record N+1's sequence number; checkpointing the last record returns `null`; checkpointing nothing
   returns the first record's sequence number).
3. `StreamMiddlewareApplication<TEvent,TItem,TResult>` (Option A) or the bespoke Kinesis-local
   application (Option B) - whichever the reviewer prefers per §3.3.
4. `KinesisStreamApplication`/`KinesisLambdaHandler` wiring, with an integration test proving: all
   records succeed → empty `BatchItemFailures`; a stream step throws after checkpointing record 2 of
   5 → `BatchItemFailures` names record 3's sequence number.
5. Update `Benzene.Aws.Lambda.Kinesis/CLAUDE.md`'s "Checkpointing uses... NullStreamCheckpointer"
   line, `work/batch-failure-handling.md` (move Kinesis from "Deliberately deferred" to a new
   shipped-table row), and `docs/plans/streaming-plan.md`'s Phase 2 (mark the Kinesis half of
   real-checkpoint-wiring done, leaving SQS streaming as the still-open half of that phase).
6. Consider implementing `UseCheckpointAfterEach()` (`docs/plans/streaming-plan.md` Phase 2) in the
   same pass, since it's what makes step 4's checkpointer useful to a handler that just wants "the
   default sensible behavior" without hand-writing checkpoint calls - not required for this design
   to be correct, but likely to be asked for immediately after this ships standalone.
