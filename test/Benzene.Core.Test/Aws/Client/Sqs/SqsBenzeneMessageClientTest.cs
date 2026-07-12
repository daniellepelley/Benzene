using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client.Sqs;

public class SqsBenzeneMessageClientTest
{
    [Fact]
    public async Task SendMessageAsync_OkResponse_ReturnsAccepted()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var client = new SqsBenzeneMessageClient("some-queue-url", mockSqsClient.Object, NullLogger<SqsBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_NonOkResponse_ReturnsMappedResult()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.BadRequest });

        var client = new SqsBenzeneMessageClient("some-queue-url", mockSqsClient.Object, NullLogger<SqsBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.NotEqual(BenzeneResultStatus.Accepted, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_ThrowingClient_ReturnsServiceUnavailable()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var client = new SqsBenzeneMessageClient("some-queue-url", mockSqsClient.Object, NullLogger<SqsBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task SendMessageAsync_PrebuiltPipeline_OkResponse_ReturnsAccepted()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var pipeline = new MiddlewarePipelineBuilder<SqsSendMessageContext>(new NullBenzeneServiceContainer())
            .UseSqsClient(mockSqsClient.Object)
            .Build();

        var client = new SqsBenzeneMessageClient("some-queue-url", pipeline, NullLogger<SqsBenzeneMessageClient>.Instance, new NullServiceResolver());

        var result = await client.SendMessageAsync<string, string>("some-topic", "some-message");

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}
