using System.Text;
using Benzene.Azure.Function.Kafka;
using Microsoft.Azure.Functions.Worker;
using Xunit;

namespace Benzene.Test.Azure;

public class KafkaGettersTest
{
    [Fact]
    public void KafkaMessageBodyGetter_ReturnsTheUtf8DecodedValue()
    {
        var context = new KafkaContext(new KafkaRecord { Value = Encoding.UTF8.GetBytes("hello") });

        Assert.Equal("hello", new KafkaMessageBodyGetter().GetBody(context));
    }

    [Fact]
    public void KafkaMessageBodyGetter_NullValue_ReturnsNull()
    {
        var context = new KafkaContext(new KafkaRecord { Value = null });

        Assert.Null(new KafkaMessageBodyGetter().GetBody(context));
    }

    [Fact]
    public void KafkaMessageTopicGetter_ReturnsTheRecordTopic()
    {
        var context = new KafkaContext(new KafkaRecord { Topic = "my-topic" });

        Assert.Equal("my-topic", new KafkaMessageTopicGetter().GetTopic(context).Id);
    }

    [Fact]
    public void KafkaMessageHeadersGetter_AlwaysReturnsAnEmptyDictionary()
    {
        var context = new KafkaContext(new KafkaRecord());

        Assert.Empty(new KafkaMessageHeadersGetter().GetHeaders(context));
    }
}
