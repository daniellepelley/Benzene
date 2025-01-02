using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Aws.Tests.Fixtures;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageSender;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Moq;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class SqsConsumerTest : IClassFixture<SqsFixture>
{
    private const string ServiceUrl = "http://localhost:4566";
    private const string QueueUrl = $"{ServiceUrl}/000000000000/{QueueName}";
    private const string QueueName = "platform-eventbus-main-queue";
    private const string AccessKey = "123";
    private const string SecretKey = "xyz";

    private static async Task SetUp()
    {
        var amazonSqsClient = CreateAmazonSqsClient();

        await amazonSqsClient.CreateQueueAsync(new CreateQueueRequest(QueueName));
        await GetAllMessagesAsync();
    }

    private static AmazonSQSClient CreateAmazonSqsClient()
    {
        return new AmazonSQSClient(new BasicAWSCredentials(AccessKey, SecretKey), new AmazonSQSConfig
        {
            ServiceURL = ServiceUrl,
        });
    }

    public static async Task<Message[]> GetAllMessagesAsync()
    {
        var client = CreateAmazonSqsClient();
        int count;

        var messages = new List<Message>();

        do
        {
            var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = QueueUrl,
                MessageAttributeNames = new[] { "All" }.ToList(),
                MaxNumberOfMessages = 10
            });

            count = result.Messages.Count;
            messages.AddRange(result.Messages);
        } while (count > 0);

        foreach (var message in messages)
        {
            await client.DeleteMessageAsync(QueueUrl, message.ReceiptHandle);
        }

        return messages.ToArray();
    }

    [Fact]
    public async Task Sqs_Receive()
    {
        await SetUp();
        var amazonSqsClient = CreateAmazonSqsClient();
        var mockFactory = new Mock<ISqsClientFactory>();
        mockFactory.Setup(x => x.Create(It.IsAny<string>())).Returns(amazonSqsClient);
        
        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton<IAmazonSQS>(amazonSqsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(serviceContainer);

        // pipelineBuilder

        var sqsConsumer = new SqsConsumer(serviceContainer.CreateServiceResolverFactory(),
            new SqsConsumerApplication(pipelineBuilder.Build()),
            new SqsConsumerConfig(), mockFactory.Object);

        await sqsConsumer.StartAsync(new CancellationToken());
        
        var messages = await GetAllMessagesAsync();
        Assert.Equal("{\"name\":\"some-name\"}", messages[0].Body);
    }

   }