using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Messages.MessageSender;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Aws.Tests;

/// <summary>
/// Converter-level (no LocalStack) coverage for the SQS outbound converter: an empty topic must not
/// become an empty <c>topic</c> message attribute. SQS rejects empty attribute values, so emitting
/// one fails the send on real AWS (LocalStack happens to tolerate it) - this guards the sender path
/// against that class of failure without needing a live broker.
/// </summary>
public class SqsContextConverterTest
{
    [Fact]
    public async Task CreateRequestAsync_EmptyTopicAndHeaders_EmitsNoMessageAttributes()
    {
        var converter = new SqsContextConverter<ExampleRequestPayload>(
            "https://sqs.us-east-1.amazonaws.com/000000000000/some-queue");

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
    public async Task CreateRequestAsync_NonEmptyTopic_EmitsTopicMessageAttribute()
    {
        var converter = new SqsContextConverter<ExampleRequestPayload>(
            "https://sqs.us-east-1.amazonaws.com/000000000000/some-queue");

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "some-topic",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: new Dictionary<string, string>());
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.True(result.Request.MessageAttributes.ContainsKey("topic"));
        Assert.Equal("some-topic", result.Request.MessageAttributes["topic"].StringValue);
    }
}
