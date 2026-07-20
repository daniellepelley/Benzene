using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients.Aws.Sns;
using Benzene.Core.Messages.MessageSender;
using Xunit;
using OutboundContext = Benzene.Clients.OutboundContext;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Aws.Tests;

/// <summary>
/// Converter-level (no LocalStack) coverage for the SNS outbound converters: an empty topic must not
/// become an empty <c>topic</c> message attribute. SNS rejects empty attribute values ("must contain
/// non-empty message attribute value"), so emitting one fails the publish - this guards the sender
/// path against that class of failure without needing a live broker. The paired non-empty case locks
/// in that a real topic is still carried (the #22 routing fix), so the guard can't be "fixed" by
/// dropping the attribute entirely.
/// </summary>
public class SnsContextConverterTest
{
    [Fact]
    public async Task CreateRequestAsync_EmptyTopicAndHeaders_EmitsNoMessageAttributes()
    {
        var converter = new SnsContextConverter<ExampleRequestPayload>(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: string.Empty,
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: new Dictionary<string, string>());
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.Empty(result.Request.MessageAttributes);
        Assert.False(result.Request.MessageAttributes.ContainsKey("topic"));
    }

    [Fact]
    public async Task CreateRequestAsync_NonEmptyTopic_CarriesItAsTheTopicAttribute()
    {
        var converter = new SnsContextConverter<ExampleRequestPayload>(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "order:created",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: new Dictionary<string, string>());
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.True(result.Request.MessageAttributes.ContainsKey("topic"));
        Assert.Equal("order:created", result.Request.MessageAttributes["topic"].StringValue);
    }

    [Fact]
    public async Task OutboundConverter_EmptyTopicAndHeaders_EmitsNoMessageAttributes()
    {
        var converter = new OutboundSnsContextConverter(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var context = new OutboundContext(
            topic: string.Empty,
            request: new ExampleRequestPayload { Name = "some-name" });

        var result = await converter.CreateRequestAsync(context);

        Assert.Empty(result.Request.MessageAttributes);
        Assert.False(result.Request.MessageAttributes.ContainsKey("topic"));
    }

    [Fact]
    public async Task OutboundConverter_NonEmptyTopic_CarriesItAsTheTopicAttribute()
    {
        var converter = new OutboundSnsContextConverter(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var context = new OutboundContext(
            topic: "order:created",
            request: new ExampleRequestPayload { Name = "some-name" });

        var result = await converter.CreateRequestAsync(context);

        Assert.True(result.Request.MessageAttributes.ContainsKey("topic"));
        Assert.Equal("order:created", result.Request.MessageAttributes["topic"].StringValue);
    }
}
