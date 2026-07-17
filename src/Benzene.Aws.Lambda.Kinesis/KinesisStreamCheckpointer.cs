using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// An <see cref="IStreamCheckpointer{TItem}"/> for a Kinesis batch: tracks the last record a stream
/// handler has checkpointed and computes the sequence number AWS should resume from if the batch
/// didn't finish - see <c>work/kinesis-batch-failure-handling-design.md</c> §3.2.
/// </summary>
internal class KinesisStreamCheckpointer : IStreamCheckpointer<KinesisEventRecord>
{
    private readonly List<KinesisEventRecord> _records;
    private int _lastCheckpointedIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisStreamCheckpointer"/> class.
    /// </summary>
    /// <param name="records">The batch's records, in their original order.</param>
    public KinesisStreamCheckpointer(List<KinesisEventRecord> records)
    {
        _records = records;
    }

    /// <inheritdoc />
    public Task CheckpointAsync(KinesisEventRecord lastProcessed)
    {
        _lastCheckpointedIndex = _records.IndexOf(lastProcessed);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the sequence number of the first record after the last checkpointed one - the record AWS
    /// should resume the batch from - or <c>null</c> if every record has been checkpointed (or the
    /// batch is empty).
    /// </summary>
    public string FirstUncheckpointedSequenceNumber =>
        _lastCheckpointedIndex + 1 < _records.Count
            ? _records[_lastCheckpointedIndex + 1].Kinesis.SequenceNumber
            : null;
}
