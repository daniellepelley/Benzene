using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.S3;

/// <summary>
/// Exposes an S3 record's bucket, key, event name, and region as message headers.
/// </summary>
public class S3MessageHeadersGetter : IMessageHeadersGetter<S3RecordContext>
{
    /// <summary>
    /// Gets the S3 event metadata as headers.
    /// </summary>
    /// <param name="context">The S3 record context to extract headers from.</param>
    /// <returns>A dictionary of header names to values, omitting any that aren't present on the record.</returns>
    public IDictionary<string, string> GetHeaders(S3RecordContext context)
    {
        var record = context.S3EventNotificationRecord;
        var headers = new Dictionary<string, string>();

        if (record.EventName != null)
        {
            headers["eventName"] = record.EventName;
        }

        if (record.AwsRegion != null)
        {
            headers["awsRegion"] = record.AwsRegion;
        }

        if (record.S3?.Bucket?.Name != null)
        {
            headers["bucketName"] = record.S3.Bucket.Name;
        }

        if (record.S3?.Object?.Key != null)
        {
            headers["key"] = record.S3.Object.Key;
        }

        return headers;
    }
}
