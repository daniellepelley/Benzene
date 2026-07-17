using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// The response a Kinesis event source mapping reads back when <c>ReportBatchItemFailures</c> is
/// configured on the trigger, mirroring <c>Amazon.Lambda.SQSEvents.SQSBatchResponse</c>'s shape
/// (Benzene hand-rolls this rather than taking a dependency on <c>Amazon.Lambda.KinesisEvents</c>,
/// matching <see cref="KinesisEvent"/>'s own convention - see this package's <c>CLAUDE.md</c>).
/// </summary>
/// <remarks>
/// Unlike SQS - where every entry in <c>BatchItemFailures</c> names a specific message to redrive -
/// AWS only ever reads the <em>first</em> entry for a Kinesis/DynamoDB Streams event source mapping:
/// records are ordered within a shard, so a failure resumes the whole batch from that sequence
/// number onward rather than skipping individually-named records. This type's constructor reflects
/// that contract directly - a single optional failed sequence number, not a list-builder - see
/// <c>work/kinesis-batch-failure-handling-design.md</c> §3.1.
/// </remarks>
public class KinesisBatchResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisBatchResponse"/> class.
    /// </summary>
    /// <param name="failedSequenceNumber">
    /// The sequence number of the first record to resume from, or <c>null</c> if the whole batch
    /// completed successfully.
    /// </param>
    public KinesisBatchResponse(string failedSequenceNumber = null)
    {
        BatchItemFailures = failedSequenceNumber == null
            ? new List<BatchItemFailure>()
            : new List<BatchItemFailure> { new() { ItemIdentifier = failedSequenceNumber } };
    }

    /// <summary>Gets the batch item failures reported back to the Kinesis event source mapping.</summary>
    [JsonPropertyName("batchItemFailures")]
    public List<BatchItemFailure> BatchItemFailures { get; }

    /// <summary>A single reported failure, identifying the sequence number to resume from.</summary>
    public class BatchItemFailure
    {
        /// <summary>Gets or sets the sequence number of the record to resume from.</summary>
        [JsonPropertyName("itemIdentifier")]
        public string ItemIdentifier { get; set; }
    }
}
