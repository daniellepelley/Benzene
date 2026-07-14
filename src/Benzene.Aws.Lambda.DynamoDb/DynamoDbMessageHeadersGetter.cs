using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Exposes the stream record's envelope metadata as <c>dynamodb-</c>-prefixed headers (plan
/// decision DS4). Unlike EventBridge there is no embedded Benzene wire-header convention here —
/// these events originate from table writes, not from a Benzene publisher.
/// </summary>
public class DynamoDbMessageHeadersGetter : IMessageHeadersGetter<DynamoDbRecordContext>
{
    /// <summary>
    /// Gets the record's envelope metadata as headers.
    /// </summary>
    /// <param name="context">The DynamoDB record context to extract headers from.</param>
    /// <returns>A dictionary of <c>dynamodb-</c>-prefixed metadata headers.</returns>
    public IDictionary<string, string> GetHeaders(DynamoDbRecordContext context)
    {
        var record = context.Record;
        var headers = new Dictionary<string, string>();

        AddIfPresent(headers, "dynamodb-event-name", record.EventName);
        AddIfPresent(headers, "dynamodb-event-id", record.EventId);
        AddIfPresent(headers, "dynamodb-table", DynamoDbUtils.GetTableName(record.EventSourceArn));
        AddIfPresent(headers, "dynamodb-sequence-number", record.Dynamodb?.SequenceNumber);
        AddIfPresent(headers, "dynamodb-stream-view-type", record.Dynamodb?.StreamViewType);
        AddIfPresent(headers, "dynamodb-event-source-arn", record.EventSourceArn);
        AddIfPresent(headers, "dynamodb-aws-region", record.AwsRegion);

        return headers;
    }

    private static void AddIfPresent(IDictionary<string, string> headers, string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            headers[key] = value;
        }
    }
}
