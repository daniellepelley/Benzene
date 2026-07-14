using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Benzene's own model of the event a DynamoDB Streams event source mapping delivers to a Lambda
/// function (plan decision DS1). Deliberately not a dependency on <c>Amazon.Lambda.DynamoDBEvents</c>:
/// the envelope is stable, and this keeps the package dependency-free beyond
/// <c>Benzene.Aws.Lambda.Core</c>.
/// </summary>
public class DynamoDbEvent
{
    /// <summary>The stream records in this batch, in shard order.</summary>
    [JsonPropertyName("Records")]
    public List<DynamoDbStreamRecord> Records { get; set; }
}
