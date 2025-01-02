using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Tests.Fixtures;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageSender;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Xunit;

namespace Benzene.Aws.Tests;

[Collection("Sequential")]
public class SqsMessageSenderBuilderTest : IClassFixture<SqsFixture>
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
    public async Task Sqs_Send()
    {
        await SetUp();
        var amazonSqsClient = CreateAmazonSqsClient();

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton<IAmazonSQS>(amazonSqsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSqs(QueueUrl, builder2 => builder2.UseSqsClient())));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope()
            .GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Name = "some-name" });

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);

        var messages = await GetAllMessagesAsync();
        Assert.Equal("{\"name\":\"some-name\"}", messages[0].Body);
    }

    [Fact]
    public async Task Sqs_Send2()
    {
        await SetUp();
        var amazonSqsClient = CreateAmazonSqsClient();

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton<IAmazonSQS>(amazonSqsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder
                .UseSqs(QueueUrl, builder2 => builder2.UseSqsClient())));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Name = "some-name" });

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);

        var messages = await GetAllMessagesAsync();
        Assert.Equal("{\"name\":\"some-name\"}", messages[0].Body);
    }

    [Fact]
    public async Task Sqs_Send3()
    {
        await SetUp();
        var amazonSqsClient = CreateAmazonSqsClient();

        var serviceContainer = new MicrosoftBenzeneServiceContainer();
        serviceContainer.AddSingleton<IAmazonSQS>(amazonSqsClient);
        var pipelineBuilder = new MiddlewarePipelineBuilder<string>(serviceContainer);

        pipelineBuilder
            .Out(x => x.CreateSender<ExampleRequestPayload>(builder => builder.UseSqs(QueueUrl)));

        var sender = serviceContainer.CreateServiceResolverFactory().CreateScope().GetService<IMessageSender<ExampleRequestPayload>>();
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Name = "some-name" });

        Assert.NotNull(result);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);

        var messages = await GetAllMessagesAsync();
        Assert.Equal("{\"name\":\"some-name\"}", messages[0].Body);
    }
}