using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsConsumerMessagePipelineTest
{
    // [Fact(Skip = "docker-compose")]
    // public async Task SendUsingDockerCompose()
    // {
    //     const string queueName = "some-test-queue";
    //     const string serviceUrl = "http://localhost:4566";
    //
    //     var client = new AmazonSQSClient(new AmazonSQSConfig
    //     {
    //         ServiceURL = serviceUrl
    //     });
    //
    //     var createQueueResponse = await client.CreateQueueAsync(queueName);
    //
    //     var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
    //     {
    //         QueueUrl = createQueueResponse.QueueUrl,
    //         MessageAttributeNames = new[] { "All" }.ToList(),
    //         MaxNumberOfMessages = 10
    //     });
    //
    //     foreach (var message in result.Messages)
    //     {
    //         await client.DeleteMessageAsync(createQueueResponse.QueueUrl, message.ReceiptHandle);
    //     }
    //
    //     var mockDefaultService = new Mock<IDefaultService>();
    //
    //     var services = new ServiceCollection();
    //     services
    //         .AddTransient<ILogger<MessageRouter<SqsConsumerMessageContext>>>(_ =>
    //             NullLogger<MessageRouter<SqsConsumerMessageContext>>.Instance)
    //         .AddTransient<ILogger>(_ => NullLogger.Instance)
    //         .AddTransient(_ => mockDefaultService.Object)
    //         .UsingBenzene(x => x.AddAwsMessageHandlers(GetType().Assembly));
    //
    //     var pipeline =
    //         new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(services));
    //
    //     var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
    //
    //     pipeline
    //         .UseMessageRouter();
    //
    //     var application = new SqsConsumerApplication(pipeline.Build());
    //
    //     var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
    //     {
    //         ServiceUrl = serviceUrl,
    //         MaxNumberOfMessages = 10,
    //         QueueUrl = createQueueResponse.QueueUrl,
    //     }, new SqsBenzeneMessageClientFactory());
    //
    //     var tokenSource = new CancellationTokenSource(5000);
    //     var token = tokenSource.Token;
    //
    //     var task = consumer.Start(token);
    //
    //     await client.SendMessageAsync(AwsEventBuilder.CreateSqsSendMessageRequest(
    //         createQueueResponse.QueueUrl, Defaults.Topic, Defaults.MessageAsObject
    //     ), token);
    //
    //     await client.SendMessageAsync(AwsEventBuilder.CreateSqsSendMessageRequest(
    //         createQueueResponse.QueueUrl, Defaults.Topic, Defaults.MessageAsObject
    //     ), token);
    //
    //     try
    //     {
    //         await task;
    //     }
    //     catch
    //     {
    //
    //     }
    //
    //     mockDefaultService.Verify(x => x.Register("some-message"), Times.Exactly(2));
    // }

    [Fact]
    public async Task SendAsync()
    {
        const string serviceUrl = "some-service-url";

        var mockSqsClient = new Mock<IAmazonSQS>();

        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        mockSqsClientFactory.Setup(x => x.Create(serviceUrl))
            .Returns(mockSqsClient.Object);

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


        var mockDefaultService = new Mock<IExampleService>();

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsConsumerMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsConsumerMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .AddScoped(_ => mockDefaultService.Object)
            .UsingBenzene(x => x
                .AddSqsConsumer());

        var pipeline =
            new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        pipeline
            .UseMessageRouter();

        var application = new SqsConsumerApplication(pipeline.Build());

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            ServiceUrl = serviceUrl,
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object);

        var tokenSource = new CancellationTokenSource(5000);
        var token = tokenSource.Token;

        var task = consumer.Start(token);

        try
        {
            await task;
        }
        catch
        {

        }

        mockDefaultService.Verify(x => x.Register("some-name"), Times.Exactly(2));
    }


    [Fact]
    public void SqsMessageMapper()
    {
        var sqsMessageMapper = new MessageMapper<SqsConsumerMessageContext>(new SqsConsumerMessageTopicMapper(), new SqsConsumerMessageBodyMapper(), new SqsConsumerMessageHeadersMapper());

        var sqsMessageContext = SqsConsumerMessageContext.CreateInstance(new Message
        {
            Body = Defaults.Message,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {"topic", new MessageAttributeValue { StringValue = Defaults.Topic}}
            }
        });

        var actualMessage = sqsMessageMapper.GetBody(sqsMessageContext);
        var actualTopic = sqsMessageMapper.GetTopic(sqsMessageContext);

        Assert.Equal(Defaults.Message, actualMessage);
        Assert.Equal(Defaults.Topic, actualTopic.Id);
    }
}
