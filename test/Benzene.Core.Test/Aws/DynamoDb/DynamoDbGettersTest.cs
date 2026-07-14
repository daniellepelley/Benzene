using System.Text.Json;
using Benzene.Aws.Lambda.DynamoDb;
using Xunit;

namespace Benzene.Test.Aws.DynamoDb;

public class DynamoDbGettersTest
{
    private const string StreamArn = "arn:aws:dynamodb:eu-west-1:123456789012:table/orders/stream/2026-01-01T00:48:05.899";

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static DynamoDbRecordContext CreateContext(DynamoDbStreamRecord record)
    {
        return DynamoDbRecordContext.CreateInstance(new DynamoDbEvent(), record);
    }

    private static DynamoDbStreamRecord CreateRecord() => new DynamoDbStreamRecord
    {
        EventId = "event-id-1",
        EventName = "INSERT",
        EventSource = "aws:dynamodb",
        EventSourceArn = StreamArn,
        AwsRegion = "eu-west-1",
        Dynamodb = new DynamoDbStreamData
        {
            Keys = Parse("{\"Id\":{\"N\":\"101\"}}"),
            NewImage = Parse("{\"Name\":{\"S\":\"some-name\"},\"Count\":{\"N\":\"3\"}}"),
            SequenceNumber = "111",
            StreamViewType = "NEW_AND_OLD_IMAGES"
        }
    };

    [Fact]
    public void Topic_IsTableNameAndEventName()
    {
        // The stream ARN's resource segment contains colons (the timestamp) — the parse must survive them.
        var topic = new DynamoDbMessageTopicGetter().GetTopic(CreateContext(CreateRecord()));

        Assert.Equal("orders:INSERT", topic.Id);
    }

    [Fact]
    public void Topic_WhenArnIsMissing_FallsBackToEventName()
    {
        var record = CreateRecord();
        record.EventSourceArn = null;

        var topic = new DynamoDbMessageTopicGetter().GetTopic(CreateContext(record));

        Assert.Equal("INSERT", topic.Id);
    }

    [Fact]
    public void Body_IsTheUnmarshalledNewImage()
    {
        var body = new DynamoDbMessageBodyGetter().GetBody(CreateContext(CreateRecord()));

        Assert.Equal("{\"Name\":\"some-name\",\"Count\":3}", body);
    }

    [Fact]
    public void Body_WithoutNewImage_FallsBackToOldImage()
    {
        var record = CreateRecord();
        record.EventName = "REMOVE";
        record.Dynamodb.NewImage = default;
        record.Dynamodb.OldImage = Parse("{\"Name\":{\"S\":\"old-name\"}}");

        var body = new DynamoDbMessageBodyGetter().GetBody(CreateContext(record));

        Assert.Equal("{\"Name\":\"old-name\"}", body);
    }

    [Fact]
    public void Body_KeysOnlyView_FallsBackToKeys()
    {
        var record = CreateRecord();
        record.Dynamodb.NewImage = default;
        record.Dynamodb.OldImage = default;
        record.Dynamodb.StreamViewType = "KEYS_ONLY";

        var body = new DynamoDbMessageBodyGetter().GetBody(CreateContext(record));

        Assert.Equal("{\"Id\":101}", body);
    }

    [Fact]
    public void Headers_ContainPrefixedEnvelopeMetadata()
    {
        var headers = new DynamoDbMessageHeadersGetter().GetHeaders(CreateContext(CreateRecord()));

        Assert.Equal("INSERT", headers["dynamodb-event-name"]);
        Assert.Equal("event-id-1", headers["dynamodb-event-id"]);
        Assert.Equal("orders", headers["dynamodb-table"]);
        Assert.Equal("111", headers["dynamodb-sequence-number"]);
        Assert.Equal("NEW_AND_OLD_IMAGES", headers["dynamodb-stream-view-type"]);
        Assert.Equal(StreamArn, headers["dynamodb-event-source-arn"]);
        Assert.Equal("eu-west-1", headers["dynamodb-aws-region"]);
    }

    [Fact]
    public void GetTableName_NonTableArn_ReturnsNull()
    {
        Assert.Null(DynamoDbUtils.GetTableName("arn:aws:sqs:eu-west-1:123456789012:some-queue"));
        Assert.Null(DynamoDbUtils.GetTableName("not-an-arn"));
        Assert.Null(DynamoDbUtils.GetTableName(null));
    }
}
