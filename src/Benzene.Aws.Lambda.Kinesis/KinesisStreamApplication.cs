using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Runs a Kinesis batch through the streaming pipeline as a single <see cref="StreamContext{TItem}"/>
/// (fan-in): the whole batch is exposed as one ordered <see cref="IAsyncEnumerable{T}"/> of records,
/// processed by one pipeline run in one DI scope — the same shape as the Azure Event Hubs streaming
/// transport. This preserves the batch as a stream, so handlers can window and re-order it (e.g.
/// <c>PartitionBy(r =&gt; r.Kinesis.PartitionKey)</c>), rather than fanning out per record and losing
/// shard ordering.
/// </summary>
/// <remarks>
/// Response-producing: wires a <see cref="KinesisStreamCheckpointer"/> into the batch's
/// <see cref="StreamContext{TItem}"/> and returns a <see cref="KinesisBatchResponse"/> naming the
/// sequence number to resume from, for triggers with <c>ReportBatchItemFailures</c> configured. If
/// the pipeline throws, the exception is caught (logged, not rethrown) so the response still carries
/// whatever the handler had checkpointed before failing — the checkpointer's resume point is itself
/// the correct failure signal for Kinesis's shard-ordered retry contract, so there's nothing to gain
/// by cascading the exception instead. See <c>work/kinesis-batch-failure-handling-design.md</c>.
/// </remarks>
public class KinesisStreamApplication : StreamMiddlewareApplication<KinesisEvent, KinesisEventRecord, KinesisBatchResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisStreamApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built stream pipeline to run the batch through.</param>
    /// <param name="options">
    /// Checkpointing options. Defaults to a new <see cref="KinesisStreamOptions"/>
    /// (<see cref="KinesisStreamOptions.AutoCheckpointOnSuccess"/> on) if omitted.
    /// </param>
    public KinesisStreamApplication(IMiddlewarePipeline<StreamContext<KinesisEventRecord>> pipeline, KinesisStreamOptions options = null)
        : base(
            new CatchAndCheckpointPipeline(
                new TransportMiddlewarePipeline<StreamContext<KinesisEventRecord>>(TransportNames.Kinesis, pipeline),
                (options ?? new KinesisStreamOptions()).AutoCheckpointOnSuccess),
            @event => BuildContext(@event.Records),
            context => BuildResponse((KinesisStreamCheckpointer)context.Checkpointer))
    { }

    private static StreamContext<KinesisEventRecord> BuildContext(List<KinesisEventRecord> records)
    {
        records ??= new List<KinesisEventRecord>();
        return new StreamContext<KinesisEventRecord>(ToAsyncEnumerable(records), checkpointer: new KinesisStreamCheckpointer(records));
    }

    private static KinesisBatchResponse BuildResponse(KinesisStreamCheckpointer checkpointer)
        => new(checkpointer.FirstUncheckpointedSequenceNumber);

    private static async IAsyncEnumerable<KinesisEventRecord> ToAsyncEnumerable(List<KinesisEventRecord> records)
    {
        foreach (var record in records)
        {
            yield return record;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Catches an exception from the inner pipeline instead of letting it cascade, so the outer
    /// <see cref="MiddlewareApplication{TEvent,TContext,TResult}"/> can still run its
    /// <c>resultMapper</c> against the context's checkpointer and return a real
    /// <see cref="KinesisBatchResponse"/> resume point.
    /// </summary>
    private class CatchAndCheckpointPipeline : IMiddlewarePipeline<StreamContext<KinesisEventRecord>>
    {
        private readonly IMiddlewarePipeline<StreamContext<KinesisEventRecord>> _pipeline;
        private readonly bool _autoCheckpointOnSuccess;

        public CatchAndCheckpointPipeline(IMiddlewarePipeline<StreamContext<KinesisEventRecord>> pipeline, bool autoCheckpointOnSuccess)
        {
            _pipeline = pipeline;
            _autoCheckpointOnSuccess = autoCheckpointOnSuccess;
        }

        public async Task HandleAsync(StreamContext<KinesisEventRecord> context, IServiceResolver serviceResolver)
        {
            try
            {
                await _pipeline.HandleAsync(context, serviceResolver);

                // Success (no throw): if the handler never checkpointed anything itself, advance the
                // checkpoint to the end so a fully-processed batch isn't redelivered forever - the
                // UseStream callback overload never checkpoints on its own. A handler that manages its
                // own checkpoints is left untouched. Never runs on the throwing path below, where the
                // resume point must stay at the handler's last explicit checkpoint.
                if (_autoCheckpointOnSuccess && context.Checkpointer is KinesisStreamCheckpointer checkpointer && !checkpointer.HasCheckpointed)
                {
                    checkpointer.CheckpointAll();
                }
            }
            catch (Exception ex)
            {
                serviceResolver.GetService<ILogger<KinesisStreamApplication>>()
                    .LogError(ex, "Kinesis stream processing failed; resuming from the last checkpoint");
            }
        }
    }
}
