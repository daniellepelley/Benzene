using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Aws.Sqs.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Testing;
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
    //     var benzeneResult = await client.ReceiveMessageAsync(new ReceiveMessageRequest
    //     {
    //         QueueUrl = createQueueResponse.QueueUrl,
    //         MessageAttributeNames = new[] { "All" }.ToList(),
    //         MaxNumberOfMessages = 10
    //     });
    //
    //     foreach (var message in benzeneResult.Messages)
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
    //         .UseMessageHandlers();
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
        mockSqsClientFactory.Setup(x => x.Create())
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

        pipeline.UseMessageHandlers();

        var application = new SqsConsumerApplication(pipeline.Build());

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object);

        var tokenSource = new CancellationTokenSource(5000);
        var token = tokenSource.Token;

        var task = consumer.StartAsync(token);

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
    public async Task SendAsync_PerMessageAckMode_DeletesOnlyTheSuccessfulMessage()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();

        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        mockSqsClientFactory.Setup(x => x.Create())
            .Returns(mockSqsClient.Object);

        var succeedingMessage = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqsMessage();
        var failingMessage = RequestMother.CreateSerializationErrorPayload().AsSqsMessage();

        mockSqsClient
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ReceiveMessageResponse
            {
                Messages = new List<Message> { succeedingMessage, failingMessage }
            })
            .ReturnsAsync(() => new ReceiveMessageResponse { Messages = new List<Message>() });

        List<DeleteMessageBatchRequestEntry> deletedEntries = null;
        mockSqsClient
            .Setup(x => x.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteMessageBatchRequest, CancellationToken>((request, _) => deletedEntries = request.Entries)
            .ReturnsAsync(new DeleteMessageBatchResponse());

        var mockDefaultService = new Mock<IExampleService>();

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsConsumerMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsConsumerMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .AddScoped(_ => mockDefaultService.Object)
            .UsingBenzene(x => x.AddSqsConsumer());

        var pipeline =
            new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline.UseMessageHandlers();

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var application = new SqsConsumerApplication(pipeline.Build(), new SqsConsumerOptions { AckMode = SqsConsumerAckMode.PerMessage });

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.PerMessage });

        var tokenSource = new CancellationTokenSource(5000);

        try
        {
            await consumer.StartAsync(tokenSource.Token);
        }
        catch
        {
        }

        Assert.NotNull(deletedEntries);
        Assert.Single(deletedEntries);
        Assert.Equal(succeedingMessage.MessageId, deletedEntries[0].Id);
    }

    [Fact]
    public async Task StartAsync_DeleteBatchPartiallyFails_LogsTheUndeletedMessages()
    {
        // A batch delete can succeed as a call while individual entries land in Failed - those
        // messages were NOT removed and will be redelivered. The worker must log that rather than
        // let the redelivery look unexplained.
        var mockSqsClient = new Mock<IAmazonSQS>();

        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        mockSqsClientFactory.Setup(x => x.Create())
            .Returns(mockSqsClient.Object);

        var message = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqsMessage();
        message.MessageId = "undeleted-message-id";

        mockSqsClient
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ReceiveMessageResponse { Messages = new List<Message> { message } })
            .ReturnsAsync(() => new ReceiveMessageResponse { Messages = new List<Message>() });

        var tokenSource = new CancellationTokenSource(5000);
        mockSqsClient
            .Setup(x => x.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            // Stop the poll loop once we've observed the one delete we care about.
            .Callback<DeleteMessageBatchRequest, CancellationToken>((_, __) => tokenSource.Cancel())
            .ReturnsAsync(new DeleteMessageBatchResponse
            {
                Failed = new List<BatchResultErrorEntry> { new BatchResultErrorEntry { Id = message.MessageId } }
            });

        var mockLogger = new Mock<ILogger<SqsConsumer>>();

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsConsumerMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsConsumerMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .UsingBenzene(x => x.AddSqsConsumer());
        // Register after UsingBenzene so this closed ILogger<SqsConsumer> is what the worker resolves.
        services.AddSingleton(mockLogger.Object);

        var pipeline =
            new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline.UseMessageHandlers();

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        // WholeBatch deletes every received message that ran without throwing, so the delete is reached.
        var application = new SqsConsumerApplication(pipeline.Build(), new SqsConsumerOptions { AckMode = SqsConsumerAckMode.WholeBatch });

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object, new SqsConsumerOptions { AckMode = SqsConsumerAckMode.WholeBatch });

        await consumer.StartAsync(tokenSource.Token);

        mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString().Contains(message.MessageId)),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ReceiveMessageThrowsTaskCanceled_StopsWithoutRethrowing()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();

        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        mockSqsClientFactory.Setup(x => x.Create())
            .Returns(mockSqsClient.Object);

        mockSqsClient
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsConsumerMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsConsumerMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .UsingBenzene(x => x.AddSqsConsumer());

        var pipeline =
            new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline.UseMessageHandlers();

        var application = new SqsConsumerApplication(pipeline.Build());
        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object);

        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(50);

        await consumer.StartAsync(tokenSource.Token);

        mockSqsClient.Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_ReceiveMessageThrowsTransientException_LogsAndContinuesPolling()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();

        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        mockSqsClientFactory.Setup(x => x.Create())
            .Returns(mockSqsClient.Object);

        mockSqsClient
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("throttled"))
            .ReturnsAsync(() => new ReceiveMessageResponse { Messages = new List<Message>() });

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsConsumerMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsConsumerMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .UsingBenzene(x => x.AddSqsConsumer());

        var pipeline =
            new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        pipeline.UseMessageHandlers();

        var application = new SqsConsumerApplication(pipeline.Build());
        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object);

        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(100);

        await consumer.StartAsync(tokenSource.Token);

        mockSqsClient.Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var mockSqsClientFactory = new Mock<ISqsClientFactory>();
        var serviceResolverFactory = new MicrosoftServiceResolverFactory(ServiceResolverMother.CreateServiceCollection());
        var application = new SqsConsumerApplication(
            new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection())).Build());

        var consumer = new SqsConsumer(serviceResolverFactory, application, new SqsConsumerConfig
        {
            MaxNumberOfMessages = 10,
            QueueUrl = "some-url"
        }, mockSqsClientFactory.Object);

        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void SqsMessageMapper()
    {
        var sqsMessageMapper = new MessageGetter<SqsConsumerMessageContext>(new SqsConsumerMessageTopicGetter(), new SqsConsumerMessageBodyGetter(), new SqsConsumerMessageHeadersGetter());

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
