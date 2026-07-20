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
    public async Task CreateRequestAsync_EmptyValuedHeader_IsSkipped_NonEmptyIsCarried()
    {
        var converter = new SnsContextConverter<ExampleRequestPayload>(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "order:created",
            message: new ExampleRequestPayload { Name = "some-name" },
            // An empty-valued header (e.g. a correlation decorator that emits "" when unset) must be
            // skipped - SNS rejects an empty attribute value and fails the whole publish.
            headers: new Dictionary<string, string> { { "x-empty", "" }, { "x-real", "v" } });
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.False(result.Request.MessageAttributes.ContainsKey("x-empty"));
        Assert.Equal("v", result.Request.MessageAttributes["x-real"].StringValue);
    }

    [Fact]
    public async Task OutboundConverter_EmptyValuedHeader_IsSkipped()
    {
        var converter = new OutboundSnsContextConverter(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var context = new OutboundContext("order:created", new ExampleRequestPayload { Name = "n" },
            new Dictionary<string, string> { { "x-empty", "" }, { "x-real", "v" } });

        var result = await converter.CreateRequestAsync(context);

        Assert.False(result.Request.MessageAttributes.ContainsKey("x-empty"));
        Assert.Equal("v", result.Request.MessageAttributes["x-real"].StringValue);
    }

    [Fact]
    public async Task CreateRequestAsync_FifoHeaders_MapToGroupAndDeduplicationId()
    {
        var converter = new SnsContextConverter<ExampleRequestPayload>(
            "arn:aws:sns:us-east-1:000000000000:some-topic.fifo",
            publishOptions: new SnsPublishOptions
            {
                MessageGroupIdHeader = "x-group",
                MessageDeduplicationIdHeader = "x-dedup"
            });

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "order:created",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: new Dictionary<string, string> { { "x-group", "group-1" }, { "x-dedup", "dedup-1" } });
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.Equal("group-1", result.Request.MessageGroupId);
        Assert.Equal("dedup-1", result.Request.MessageDeduplicationId);
    }

    [Fact]
    public async Task CreateRequestAsync_InferNumericAttributeTypes_SetsNumberDataTypeForNumericHeaders()
    {
        var converter = new SnsContextConverter<ExampleRequestPayload>(
            "arn:aws:sns:us-east-1:000000000000:some-topic",
            publishOptions: new SnsPublishOptions { InferNumericAttributeTypes = true });

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "order:created",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: new Dictionary<string, string> { { "amount", "42" }, { "region", "us-east-1" } });
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.Equal("Number", result.Request.MessageAttributes["amount"].DataType);
        Assert.Equal("String", result.Request.MessageAttributes["region"].DataType);
    }

    [Fact]
    public async Task CreateRequestAsync_WithoutPublishOptions_LeavesFifoUnsetAndAttributesString()
    {
        var converter = new SnsContextConverter<ExampleRequestPayload>(
            "arn:aws:sns:us-east-1:000000000000:some-topic");

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "order:created",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: new Dictionary<string, string> { { "amount", "42" } });
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.Null(result.Request.MessageGroupId);
        Assert.Equal("String", result.Request.MessageAttributes["amount"].DataType);
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
