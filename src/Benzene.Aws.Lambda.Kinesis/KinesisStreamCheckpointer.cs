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
        // Only ever advance the watermark, never rewind it. IndexOf returns -1 for a record that isn't
        // in the batch by reference equality (e.g. a projected/transformed copy the handler passes) -
        // the old code then set the watermark to -1, silently rewinding the resume point to before the
        // first record and reprocessing the whole batch. Guarding with `>` ignores that (and any
        // out-of-order checkpoint that would move the resume point backward).
        var index = _records.IndexOf(lastProcessed);
        if (index > _lastCheckpointedIndex)
        {
            _lastCheckpointedIndex = index;
        }

        return Task.CompletedTask;
    }

    /// <summary>Whether the handler has checkpointed at least one record.</summary>
    public bool HasCheckpointed => _lastCheckpointedIndex >= 0;

    /// <summary>
    /// Advances the checkpoint to the last record in the batch, marking the whole batch processed.
    /// Used by <see cref="KinesisStreamOptions.AutoCheckpointOnSuccess"/> when a batch completes
    /// without the handler checkpointing anything itself.
    /// </summary>
    public void CheckpointAll() => _lastCheckpointedIndex = _records.Count - 1;

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
