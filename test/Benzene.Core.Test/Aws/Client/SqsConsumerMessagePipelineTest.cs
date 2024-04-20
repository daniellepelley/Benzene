using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Client;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Tools;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client;

public class SqsBenzeneMessageClientTest
{
    [Fact]
    public async Task SendAsync()
    {
        const string queueUrl = "some-queue-url";
        
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

       
        mockSqsClient
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ReceiveMessageResponse
            {
                Messages = new List<Message>
                {
                    MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqsMessage(),
                    MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqsMessage()
                }
            })
            .ReturnsAsync(() => new ReceiveMessageResponse
            {
                Messages = new List<Message>()
            });


        var sqsClient = new SqsMessageClient(mockSqsClient.Object, queueUrl);
        await sqsClient.PublishAsync(Defaults.Topic, Defaults.Message, ServiceResultStatus.Ok);

        mockSqsClient.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(sendMessageRequest => sendMessageRequest.QueueUrl == queueUrl), It.IsAny<CancellationToken>()));
    }
}
