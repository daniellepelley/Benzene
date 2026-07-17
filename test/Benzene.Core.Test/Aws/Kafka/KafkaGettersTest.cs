using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.Lambda.KafkaEvents;
using Benzene.Aws.Lambda.Kafka;
using Xunit;

namespace Benzene.Test.Aws.Kafka;

public class KafkaGettersTest
{
    private static MemoryStream ValueStream(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    private static KafkaContext CreateContext(KafkaEvent.KafkaEventRecord record)
    {
        return new KafkaContext(new KafkaEvent(), record);
    }

    [Fact]
    public void BodyGetter_ReturnsTheUtf8DecodedValue()
    {
        var context = CreateContext(new KafkaEvent.KafkaEventRecord { Value = ValueStream("hello") });

        Assert.Equal("hello", new KafkaMessageBodyGetter().GetBody(context));
    }

    [Fact]
    public void TopicGetter_ReturnsTheRecordTopic()
    {
        var context = CreateContext(new KafkaEvent.KafkaEventRecord { Topic = "my-topic" });

        Assert.Equal("my-topic", new KafkaMessageTopicGetter().GetTopic(context).Id);
    }

    [Fact]
    public void HeadersGetter_NoHeaderBatches_ReturnsEmptyDictionary()
    {
        var context = CreateContext(new KafkaEvent.KafkaEventRecord
        {
            Topic = "my-topic",
            Headers = new List<IDictionary<string, byte[]>>()
        });

        Assert.Empty(new KafkaMessageHeadersGetter().GetHeaders(context));
    }

    [Fact]
    public void HeadersGetter_DecodesFirstHeaderBatchAndAddsTopic()
    {
        var context = CreateContext(new KafkaEvent.KafkaEventRecord
        {
            Topic = "my-topic",
            Headers = new List<IDictionary<string, byte[]>>
            {
                new Dictionary<string, byte[]>
                {
                    ["x-correlation-id"] = Encoding.UTF8.GetBytes("abc-123")
                }
            }
        });

        var headers = new KafkaMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("abc-123", headers["x-correlation-id"]);
        Assert.Equal("my-topic", headers["topic"]);
    }
}
