using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Aws.S3;

/// <summary>
/// Builds the message body for an S3 record by serializing its bucket, object, and event metadata
/// to JSON (as an <see cref="S3Notification"/>), so it can be deserialized into a handler's request type.
/// </summary>
public class S3MessageBodyGetter : IMessageBodyGetter<S3RecordContext>
{
    private static readonly JsonSerializer Serializer = new();

    /// <summary>
    /// Gets the JSON body describing the S3 event.
    /// </summary>
    /// <param name="context">The S3 record context to build the body from.</param>
    /// <returns>An <see cref="S3Notification"/> serialized to JSON.</returns>
    public string GetBody(S3RecordContext context)
    {
        var record = context.S3EventNotificationRecord;

        var notification = new S3Notification
        {
            EventName = record.EventName,
            AwsRegion = record.AwsRegion,
            BucketName = record.S3?.Bucket?.Name,
            Key = record.S3?.Object?.Key,
            Size = record.S3?.Object?.Size ?? 0,
            ETag = record.S3?.Object?.ETag
        };

        return Serializer.Serialize(notification);
    }
}
