using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// One change record within a DynamoDB Streams batch. The envelope keys are camelCase on the wire
/// (<c>eventID</c>, <c>eventName</c>, ...) while the nested <c>dynamodb</c> object uses PascalCase —
/// the explicit <see cref="JsonPropertyNameAttribute"/>s pin the exact wire names so deserialization
/// is independent of any serializer naming policy.
/// </summary>
public class DynamoDbStreamRecord
{
    /// <summary>The unique identifier of this stream record.</summary>
    [JsonPropertyName("eventID")]
    public string EventId { get; set; }

    /// <summary>The type of change: <c>INSERT</c>, <c>MODIFY</c>, or <c>REMOVE</c>.</summary>
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    /// <summary>The stream record format version.</summary>
    [JsonPropertyName("eventVersion")]
    public string EventVersion { get; set; }

    /// <summary>The event source; always <c>aws:dynamodb</c> for stream records.</summary>
    [JsonPropertyName("eventSource")]
    public string EventSource { get; set; }

    /// <summary>
    /// The stream ARN this record came from
    /// (<c>arn:aws:dynamodb:region:account:table/Name/stream/timestamp</c>). The table name is
    /// parsed out of this via <see cref="DynamoDbUtils.GetTableName"/>.
    /// </summary>
    [JsonPropertyName("eventSourceARN")]
    public string EventSourceArn { get; set; }

    /// <summary>The AWS region the change happened in.</summary>
    [JsonPropertyName("awsRegion")]
    public string AwsRegion { get; set; }

    /// <summary>The change data itself: keys, images, and stream metadata.</summary>
    [JsonPropertyName("dynamodb")]
    public DynamoDbStreamData Dynamodb { get; set; }
}
