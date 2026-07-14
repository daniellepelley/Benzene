using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// The <c>dynamodb</c> object of a stream record. <see cref="Keys"/>, <see cref="NewImage"/>, and
/// <see cref="OldImage"/> are kept as raw <see cref="JsonElement"/>s in DynamoDB AttributeValue
/// format (<c>{"Id": {"N": "101"}}</c>) — <see cref="DynamoDbAttributeValueConverter"/> unmarshals
/// them into plain JSON for handlers. An absent image has
/// <see cref="JsonElement.ValueKind"/> == <see cref="JsonValueKind.Undefined"/>.
/// </summary>
public class DynamoDbStreamData
{
    /// <summary>The primary key attributes of the changed item, in AttributeValue format.</summary>
    [JsonPropertyName("Keys")]
    public JsonElement Keys { get; set; }

    /// <summary>
    /// The item after the change, in AttributeValue format. Present for <c>INSERT</c>/<c>MODIFY</c>
    /// when the stream view includes new images.
    /// </summary>
    [JsonPropertyName("NewImage")]
    public JsonElement NewImage { get; set; }

    /// <summary>
    /// The item before the change, in AttributeValue format. Present for <c>MODIFY</c>/<c>REMOVE</c>
    /// when the stream view includes old images.
    /// </summary>
    [JsonPropertyName("OldImage")]
    public JsonElement OldImage { get; set; }

    /// <summary>
    /// The record's sequence number within its shard — the identifier reported back to Lambda for
    /// partial batch failure checkpointing.
    /// </summary>
    [JsonPropertyName("SequenceNumber")]
    public string SequenceNumber { get; set; }

    /// <summary>The stream view type (e.g. <c>NEW_AND_OLD_IMAGES</c>, <c>KEYS_ONLY</c>).</summary>
    [JsonPropertyName("StreamViewType")]
    public string StreamViewType { get; set; }

    /// <summary>The size of the record in bytes.</summary>
    [JsonPropertyName("SizeBytes")]
    public long? SizeBytes { get; set; }

    /// <summary>The approximate time the change was captured, as a Unix epoch timestamp.</summary>
    [JsonPropertyName("ApproximateCreationDateTime")]
    public double? ApproximateCreationDateTime { get; set; }
}
