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

    [Fact]
    public void HeadersGetter_MultipleHeaderEntries_DecodesAllOfThem()
    {
        // The AWS Kafka wire format emits each record header as a SEPARATE single-entry element in the
        // Headers list (preserving Kafka's ordered, duplicate-key-capable headers). The getter must
        // flatten all entries, not take only the first - otherwise every header after the first (very
        // commonly `traceparent`, which follows app/correlation headers) is silently dropped, breaking
        // trace propagation, version routing, and correlation.
        var context = CreateContext(new KafkaEvent.KafkaEventRecord
        {
            Topic = "my-topic",
            Headers = new List<IDictionary<string, byte[]>>
            {
                new Dictionary<string, byte[]> { ["x-correlation-id"] = Encoding.UTF8.GetBytes("abc-123") },
                new Dictionary<string, byte[]> { ["traceparent"] = Encoding.UTF8.GetBytes("00-tid-sid-01") }
            }
        });

        var headers = new KafkaMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("abc-123", headers["x-correlation-id"]);
        Assert.Equal("00-tid-sid-01", headers["traceparent"]);
        Assert.Equal("my-topic", headers["topic"]);
    }
}
