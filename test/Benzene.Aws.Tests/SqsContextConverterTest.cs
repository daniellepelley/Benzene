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

    [Fact]
    public async Task CreateRequestAsync_MoreThan10Attributes_ThrowsClearlyBeforeSend()
    {
        var converter = new SqsContextConverter<ExampleRequestPayload>(
            "https://sqs.us-east-1.amazonaws.com/000000000000/some-queue");

        // 10 headers + the topic attribute = 11, over the SQS limit of 10.
        var headers = new Dictionary<string, string>();
        for (var i = 0; i < 10; i++)
        {
            headers[$"h{i}"] = $"v{i}";
        }

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "some-topic",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: headers);
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var exception = await Assert.ThrowsAsync<System.InvalidOperationException>(() => converter.CreateRequestAsync(context));
        Assert.Contains("11", exception.Message);
        Assert.Contains("10 message attributes", exception.Message);
    }

    [Fact]
    public async Task CreateRequestAsync_Exactly10Attributes_IsAllowed()
    {
        var converter = new SqsContextConverter<ExampleRequestPayload>(
            "https://sqs.us-east-1.amazonaws.com/000000000000/some-queue");

        // 9 headers + the topic attribute = 10, exactly at the limit.
        var headers = new Dictionary<string, string>();
        for (var i = 0; i < 9; i++)
        {
            headers[$"h{i}"] = $"v{i}";
        }

        var request = new BenzeneClientRequest<ExampleRequestPayload>(
            topic: "some-topic",
            message: new ExampleRequestPayload { Name = "some-name" },
            headers: headers);
        var context = new BenzeneClientContext<ExampleRequestPayload, Void>(request);

        var result = await converter.CreateRequestAsync(context);

        Assert.Equal(10, result.Request.MessageAttributes.Count);
    }
}
