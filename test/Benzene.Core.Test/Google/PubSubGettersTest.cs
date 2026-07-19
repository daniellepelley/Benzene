using Benzene.Core;
using Benzene.GoogleCloud.Functions.PubSub;
using Benzene.GoogleCloud.Functions.PubSub.TestHelpers;
using Xunit;

namespace Benzene.Test.Google;

public class PubSubGettersTest
{
    [Fact]
    public void PubSubMessageBodyGetter_ReturnsTheUtf8DecodedBody()
    {
        var data = new PubSubMessageBuilder().WithRawBody("hello").Build();
        var context = new PubSubContext(data);

        Assert.Equal("hello", new PubSubMessageBodyGetter().GetBody(context));
    }

    [Fact]
    public void PubSubMessageTopicGetter_ReturnsTheTopicAttribute()
    {
        var data = new PubSubMessageBuilder().WithTopic("my-topic").Build();
        var context = new PubSubContext(data);

        Assert.Equal("my-topic", new PubSubMessageTopicGetter().GetTopic(context).Id);
    }

    [Fact]
    public void PubSubMessageTopicGetter_MissingTopicAttribute_ReturnsMissing()
    {
        var data = new PubSubMessageBuilder().Build();
        var context = new PubSubContext(data);

        Assert.Equal(Constants.Missing, new PubSubMessageTopicGetter().GetTopic(context).Id);
    }

    [Fact]
    public void PubSubMessageTopicGetter_ReadsCustomAttributeKey_WhenConfigured()
    {
        var data = new PubSubMessageBuilder().WithAttribute("x-my-topic", "my-topic").Build();
        var context = new PubSubContext(data);

        Assert.Equal("my-topic", new PubSubMessageTopicGetter("x-my-topic").GetTopic(context).Id);
    }

    [Fact]
    public void PubSubMessageHeadersGetter_ReturnsAttributesAsHeaders()
    {
        var data = new PubSubMessageBuilder().WithAttribute("correlation-id", "abc-123").Build();
        var context = new PubSubContext(data);

        var headers = new PubSubMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("abc-123", headers["correlation-id"]);
    }
}
