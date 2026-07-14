using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// The AWS Lambda Kinesis Data Streams event: a batch of records delivered to a Lambda target per
/// invocation.
/// </summary>
/// <remarks>
/// Modeled as Benzene's own type — the Lambda Kinesis event shape is stable and documented, and this
/// keeps the package free of an extra AWS event-package dependency (mirroring
/// <c>Benzene.Aws.Lambda.EventBridge</c>). If you'd rather use the official
/// <c>Amazon.Lambda.KinesisEvents</c> POCOs, swap this type and <see cref="KinesisEventRecord"/> for
/// them — the record shape matches.
/// </remarks>
public class KinesisEvent
{
    /// <summary>Gets or sets the records in this batch.</summary>
    [JsonPropertyName("Records")]
    public List<KinesisEventRecord> Records { get; set; }
}

/// <summary>
/// A single record within a <see cref="KinesisEvent"/> batch.
/// </summary>
public class KinesisEventRecord
{
    /// <summary>Gets or sets the event source; <c>"aws:kinesis"</c> for Kinesis Data Streams records.</summary>
    [JsonPropertyName("eventSource")]
    public string EventSource { get; set; }

    /// <summary>Gets or sets the unique record id (<c>shardId-…:sequenceNumber</c>).</summary>
    [JsonPropertyName("eventID")]
    public string EventId { get; set; }

    /// <summary>Gets or sets the event name (<c>aws:kinesis:record</c>).</summary>
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    /// <summary>Gets or sets the AWS region the record originated in.</summary>
    [JsonPropertyName("awsRegion")]
    public string AwsRegion { get; set; }

    /// <summary>Gets or sets the ARN of the source stream.</summary>
    [JsonPropertyName("eventSourceARN")]
    public string EventSourceArn { get; set; }

    /// <summary>Gets or sets the Kinesis-specific payload (data, partition key, sequence number).</summary>
    [JsonPropertyName("kinesis")]
    public KinesisRecordData Kinesis { get; set; }
}

/// <summary>
/// The Kinesis payload of a <see cref="KinesisEventRecord"/>.
/// </summary>
public class KinesisRecordData
{
    /// <summary>
    /// Gets or sets the partition key the record was written under. Records with the same partition
    /// key land on the same shard and are ordered — use <c>PartitionBy(r =&gt; r.Kinesis.PartitionKey)</c>
    /// to restore that ordering after the stream is processed.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; }

    /// <summary>Gets or sets the record's sequence number within its shard.</summary>
    [JsonPropertyName("sequenceNumber")]
    public string SequenceNumber { get; set; }

    /// <summary>Gets or sets the record data, base64-encoded (as delivered by Lambda).</summary>
    [JsonPropertyName("data")]
    public string Data { get; set; }

    /// <summary>Gets or sets the approximate time the record was inserted into the stream (epoch seconds).</summary>
    [JsonPropertyName("approximateArrivalTimestamp")]
    public double? ApproximateArrivalTimestamp { get; set; }

    /// <summary>
    /// Decodes <see cref="Data"/> from base64 to its raw bytes.
    /// </summary>
    /// <returns>The decoded bytes, or an empty array if there is no data.</returns>
    public byte[] GetData()
    {
        return string.IsNullOrEmpty(Data) ? Array.Empty<byte>() : Convert.FromBase64String(Data);
    }

    /// <summary>
    /// Decodes <see cref="Data"/> from base64 and interprets it as a UTF-8 string.
    /// </summary>
    /// <returns>The decoded string, or an empty string if there is no data.</returns>
    public string GetDataAsString()
    {
        return string.IsNullOrEmpty(Data) ? string.Empty : Encoding.UTF8.GetString(Convert.FromBase64String(Data));
    }
}
