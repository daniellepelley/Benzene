using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs.Client;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsMessageClientTest
{
    [Fact]
    public async Task PublishAsync_DefaultKeys_WritesTopicAndStatusToDefaultAttributes()
    {
        var mockSqs = new Mock<IAmazonSQS>();
        mockSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var client = new SqsMessageClient(mockSqs.Object, "https://queue");

        await client.PublishAsync("some-topic", "{}", "ok");

        mockSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r =>
                r.MessageAttributes["topic"].StringValue == "some-topic" &&
                r.MessageAttributes["status"].StringValue == "ok"),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task PublishAsync_CustomKeys_WritesTopicAndStatusToConfiguredAttributes()
    {
        var mockSqs = new Mock<IAmazonSQS>();
        mockSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var client = new SqsMessageClient(mockSqs.Object, "https://queue",
            topicAttributeKey: "x-my-topic", statusAttributeKey: "x-my-status");

        await client.PublishAsync("some-topic", "{}", "ok");

        mockSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r =>
                r.MessageAttributes.ContainsKey("x-my-topic") &&
                r.MessageAttributes["x-my-topic"].StringValue == "some-topic" &&
                r.MessageAttributes.ContainsKey("x-my-status") &&
                r.MessageAttributes["x-my-status"].StringValue == "ok" &&
                !r.MessageAttributes.ContainsKey("topic") &&
                !r.MessageAttributes.ContainsKey("status")),
            It.IsAny<CancellationToken>()));
    }
}
