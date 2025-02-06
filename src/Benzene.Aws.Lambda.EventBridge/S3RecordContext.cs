using Amazon.Lambda.S3Events;

namespace Benzene.Aws.EventBridge;

public class S3RecordContext 
{
    private S3RecordContext(S3Event s3Event,  S3Event.S3EventNotificationRecord s3EventNotificationRecord)
    {
        S3EventNotificationRecord = s3EventNotificationRecord;
        S3Event = s3Event;
    }
    public static S3RecordContext CreateInstance(S3Event s3Event, S3Event.S3EventNotificationRecord s3EventNotificationRecord)
    {
        return new S3RecordContext(s3Event, s3EventNotificationRecord);
    }
    
    public S3Event S3Event { get; }
    public S3Event.S3EventNotificationRecord S3EventNotificationRecord { get; }
}
