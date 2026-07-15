using Amazon.Lambda.S3Events;
using Benzene.Aws.S3;
using Xunit;

namespace Benzene.Test.Aws.S3;

public class S3MessageMapperTests
{
    private static S3RecordContext CreateContext(string eventName)
    {
        var record = new S3Event.S3EventNotificationRecord
        {
            EventName = eventName
        };
        return S3RecordContext.CreateInstance(new S3Event { Records = [record] }, record);
    }

    [Fact]
    public void GetTopic_UsesTheS3EventName()
    {
        var topic = new S3MessageTopicGetter().GetTopic(CreateContext("ObjectCreated:Put"));

        Assert.Equal("ObjectCreated:Put", topic.Id);
    }

    [Fact]
    public void GetBody_IncludesTheEventName()
    {
        var body = new S3MessageBodyGetter().GetBody(CreateContext("ObjectCreated:Put"));

        Assert.Contains("ObjectCreated:Put", body);
    }

    [Fact]
    public void GetHeaders_IncludesTheEventName()
    {
        var headers = new S3MessageHeadersGetter().GetHeaders(CreateContext("ObjectCreated:Put"));

        Assert.Equal("ObjectCreated:Put", headers["eventName"]);
    }

    [Fact]
    public void GetHeaders_NoS3Entity_OmitsBucketAndKeyHeaders()
    {
        // The record's S3 property (bucket/object metadata) is null unless explicitly populated -
        // GetHeaders chains through it with null-conditional access, so this proves it degrades to
        // omitting those headers rather than throwing.
        var headers = new S3MessageHeadersGetter().GetHeaders(CreateContext("ObjectCreated:Put"));

        Assert.False(headers.ContainsKey("bucketName"));
        Assert.False(headers.ContainsKey("key"));
    }

    [Fact]
    public void GetBody_NoS3Entity_SerializesWithoutThrowing()
    {
        var body = new S3MessageBodyGetter().GetBody(CreateContext("ObjectCreated:Put"));

        Assert.Contains("\"bucketName\":null", body);
        Assert.Contains("\"key\":null", body);
    }
}
