using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// The response an Amazon MSK / self-managed Kafka event source mapping reads back when
/// <c>ReportBatchItemFailures</c> is configured on the trigger's <c>FunctionResponseTypes</c>.
/// </summary>
/// <remarks>
/// <para>
/// Benzene hand-rolls this rather than taking a dependency on an AWS event package, matching the rest
/// of this package's dependency-light convention.
/// </para>
/// <para>
/// The Kafka wire contract differs from Kinesis/DynamoDB/SQS: the <c>itemIdentifier</c> is a JSON
/// <em>object</em> naming the topic-partition and the offset to resume from, not a bare string —
/// <c>{ "batchItemFailures": [ { "itemIdentifier": { "partition": "my-topic-0", "offset": 100 } } ] }</c>.
/// AWS resumes each reported topic-partition from the named offset; any <c>partition</c>/<c>offset</c>
/// that wasn't in the invoked event is treated as an error and retries the whole batch. Verified
/// against the AWS "Configuring error handling controls for Kafka event sources" documentation.
/// </para>
/// </remarks>
public class KafkaBatchResponse
{
    /// <summary>
    /// Initializes a new empty <see cref="KafkaBatchResponse"/> (no failures — the whole batch is
    /// considered successfully processed).
    /// </summary>
    public KafkaBatchResponse()
    {
    }

    /// <summary>
    /// Initializes a new <see cref="KafkaBatchResponse"/> carrying the given per-partition failures.
    /// </summary>
    /// <param name="failures">The topic-partition/offset resume points that failed.</param>
    public KafkaBatchResponse(IEnumerable<BatchItemFailure> failures)
    {
        BatchItemFailures.AddRange(failures);
    }

    /// <summary>Gets the batch item failures reported back to the Kafka event source mapping.</summary>
    [JsonPropertyName("batchItemFailures")]
    public List<BatchItemFailure> BatchItemFailures { get; } = new();

    /// <summary>A single reported failure, identifying the topic-partition and offset to resume from.</summary>
    public class BatchItemFailure
    {
        /// <summary>Initializes a new empty <see cref="BatchItemFailure"/>.</summary>
        public BatchItemFailure()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="BatchItemFailure"/> for the given topic-partition and offset.
        /// </summary>
        /// <param name="partition">The <c>topic-partition_number</c> key (e.g. <c>"my-topic-0"</c>).</param>
        /// <param name="offset">The offset of the first record to resume from within that partition.</param>
        public BatchItemFailure(string partition, long offset)
        {
            ItemIdentifier = new ItemIdentifierValue { Partition = partition, Offset = offset };
        }

        /// <summary>Gets or sets the topic-partition/offset resume point for this failure.</summary>
        [JsonPropertyName("itemIdentifier")]
        public ItemIdentifierValue ItemIdentifier { get; set; }
    }

    /// <summary>The Kafka-shaped item identifier: a topic-partition plus the offset to resume from.</summary>
    public class ItemIdentifierValue
    {
        /// <summary>Gets or sets the <c>topic-partition_number</c> key (e.g. <c>"my-topic-0"</c>).</summary>
        [JsonPropertyName("partition")]
        public string Partition { get; set; }

        /// <summary>Gets or sets the offset of the first record to resume from within the partition.</summary>
        [JsonPropertyName("offset")]
        public long Offset { get; set; }
    }
}
