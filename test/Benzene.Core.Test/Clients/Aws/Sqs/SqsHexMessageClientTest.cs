using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Logging;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Elements.Core.Results;
using Benzene.Results;
using Benzene.Test.Clients.Aws.Samples;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Clients.Aws.Sqs;

public class SqsBenzeneMessageClientTest
{
    [Fact]
    public async Task FireAndForget()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        var client = new SqsBenzeneMessageClientFactory(Defaults.SqsQueueUrl, mockAmazonSqs.Object, Mock.Of<IBenzeneLogger>()).Create();
        var result = await client.SendMessageAsync<ExampleRequestPayload, ExampleResponsePayload>(Defaults.Topic, 
                new ExampleRequestPayload { Id = 42, Name = "hi" });

        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.QueueUrl == Defaults.SqsQueueUrl &&
                message.MessageAttributes["topic"].StringValue == Defaults.Topic &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.MessageBody).Name == "hi"
                ), It.IsAny<CancellationToken>()));

        Assert.Equal(ClientResultStatus.Accepted, result.Status);
    }
    
    [Fact]
    public async Task FireAndForget_Exception()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception());

        var client = new SqsBenzeneMessageClientFactory(Defaults.SqsQueueUrl, mockAmazonSqs.Object, Mock.Of<IBenzeneLogger>()).Create();
        var result = await client.SendMessageAsync<ExampleRequestPayload, ExampleResponsePayload>(Defaults.Topic, 
                new ExampleRequestPayload { Id = 42, Name = "hi" });

        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.QueueUrl == Defaults.SqsQueueUrl &&
                message.MessageAttributes["topic"].StringValue == Defaults.Topic &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.MessageBody).Name == "hi"
                ), It.IsAny<CancellationToken>()));

        Assert.Equal(ClientResultStatus.ServiceUnavailable, result.Status);
    }

}
