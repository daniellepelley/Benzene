namespace Benzene.Aws.Lambda.S3;

/// <summary>
/// The message payload an S3 event notification is deserialized into for message handlers. A handler
/// can declare this (or any type with matching properties) as its request type to receive the bucket,
/// object key, and related metadata of the S3 event.
/// </summary>
public class S3Notification
{
    /// <summary>
    /// Gets or sets the S3 event name, e.g. <c>ObjectCreated:Put</c> or <c>ObjectRemoved:Delete</c>.
    /// This is also the topic the record is routed by.
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    /// Gets or sets the AWS region the event originated in.
    /// </summary>
    public string AwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the name of the bucket the object belongs to.
    /// </summary>
    public string BucketName { get; set; }

    /// <summary>
    /// Gets or sets the object key.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the object size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the object ETag.
    /// </summary>
    public string ETag { get; set; }
}
