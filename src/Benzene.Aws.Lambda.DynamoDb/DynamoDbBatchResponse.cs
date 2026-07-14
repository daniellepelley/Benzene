using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// The partial batch failure response returned to Lambda when the event source mapping has
/// <c>ReportBatchItemFailures</c> enabled. Lambda checkpoints the shard at the lowest reported
/// sequence number and redelivers from there.
/// </summary>
public class DynamoDbBatchResponse
{
    /// <summary>The records that failed, identified by sequence number.</summary>
    [JsonPropertyName("batchItemFailures")]
    public List<BatchItemFailure> BatchItemFailures { get; set; } = new();

    /// <summary>
    /// Identifies a single failed record within the batch.
    /// </summary>
    public class BatchItemFailure
    {
        /// <summary>The failed record's <c>SequenceNumber</c>.</summary>
        [JsonPropertyName("itemIdentifier")]
        public string ItemIdentifier { get; set; }
    }
}
