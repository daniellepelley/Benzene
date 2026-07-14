using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Runs a Kinesis batch through the streaming pipeline as a single <see cref="StreamContext{TItem}"/>
/// (fan-in): the whole batch is exposed as one ordered <see cref="IAsyncEnumerable{T}"/> of records,
/// processed by one pipeline run in one DI scope — the same shape as the Azure Event Hubs streaming
/// transport. This preserves the batch as a stream, so handlers can window and re-order it (e.g.
/// <c>PartitionBy(r =&gt; r.Kinesis.PartitionKey)</c>), rather than fanning out per record and losing
/// shard ordering.
/// </summary>
public class KinesisStreamApplication : StreamMiddlewareApplication<KinesisEvent, KinesisEventRecord>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisStreamApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built stream pipeline to run the batch through.</param>
    public KinesisStreamApplication(IMiddlewarePipeline<StreamContext<KinesisEventRecord>> pipeline)
        : base(
            new TransportMiddlewarePipeline<StreamContext<KinesisEventRecord>>("kinesis", pipeline),
            @event => new StreamContext<KinesisEventRecord>(ToAsyncEnumerable(@event.Records)))
    { }

    private static async IAsyncEnumerable<KinesisEventRecord> ToAsyncEnumerable(List<KinesisEventRecord> records)
    {
        if (records != null)
        {
            foreach (var record in records)
            {
                yield return record;
            }
        }

        await Task.CompletedTask;
    }
}
