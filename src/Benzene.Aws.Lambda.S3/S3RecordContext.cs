using Amazon.Lambda.S3Events;
using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Aws.Lambda.S3;

/// <summary>
/// Provides the middleware pipeline context for a single record within an S3 event notification batch.
/// </summary>
public class S3RecordContext : IHasMessageResult
{
    private S3RecordContext(S3Event s3Event,  S3Event.S3EventNotificationRecord s3EventNotificationRecord)
    {
        S3EventNotificationRecord = s3EventNotificationRecord;
        S3Event = s3Event;
    }

    /// <summary>
    /// Creates a new <see cref="S3RecordContext"/> for a single record within an S3 event notification batch.
    /// </summary>
    /// <param name="s3Event">The full S3 event notification batch.</param>
    /// <param name="s3EventNotificationRecord">The specific record within the batch this context represents.</param>
    /// <returns>The created context.</returns>
    public static S3RecordContext CreateInstance(S3Event s3Event, S3Event.S3EventNotificationRecord s3EventNotificationRecord)
    {
        return new S3RecordContext(s3Event, s3EventNotificationRecord);
    }

    /// <summary>
    /// Gets the full S3 event notification batch this record belongs to.
    /// </summary>
    public S3Event S3Event { get; }

    /// <summary>
    /// Gets the specific S3 event notification record this context represents.
    /// </summary>
    public S3Event.S3EventNotificationRecord S3EventNotificationRecord { get; }

    /// <summary>
    /// Gets or sets the result of handling this record. Set by
    /// <see cref="S3MessageHandlerResultSetter"/>. S3 events are fire-and-forget, so this is
    /// recorded for diagnostics rather than written back to a response.
    /// </summary>
    public IMessageResult MessageResult { get; set; }
}
