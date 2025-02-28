﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.Aws.Sqs.Client;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Aws.Sns;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Benzene.Xml;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsMessagePipelineTest
{
    [Fact]
    public async Task Send()
    {
        var mockExampleService = new Mock<IExampleService>();

        bool? isSuccessful = null;

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ => NullLogger<MessageRouter<SqsMessageContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .AddTransient(_ => mockExampleService.Object)
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddSqs())
                )
            .Configure(app => app
                .UseSqs(sqs => sqs
                    .OnResponse("Check Response", context =>
                    {
                        isSuccessful = context.IsSuccessful;
                    }).UseMessageHandlers()
            )
        ).BuildHost();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs();

        SQSBatchResponse batchResponse = await host.SendSqsAsync(request);
        Assert.True(isSuccessful);
        Assert.Empty(batchResponse.BatchItemFailures);
    }

    [Fact]
    public async Task Send_Xml()
    {
        var mockExampleService = new Mock<IExampleService>();

        bool? isSuccessful = null;

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ => NullLogger<MessageRouter<SqsMessageContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .AddTransient(_ => mockExampleService.Object)
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddXml()
                    .AddSqs())
                )
            .Configure(app => app
                .UseSqs(sqs => sqs
                    .OnResponse("Check Response", context =>
                    {
                        isSuccessful = context.IsSuccessful;
                    }).UseMessageHandlers()
            )
        ).BuildHost();

        var request = MessageBuilder
            .Create(Defaults.Topic, new ExampleRequestPayload { Name = "some-name"})
            .WithHeader("content-type", "application/xml")
            .AsSqs(new XmlSerializer());

        SQSBatchResponse batchResponse = await host.SendSqsAsync(request);
        Assert.True(isSuccessful);
        Assert.Empty(batchResponse.BatchItemFailures);
    }


    [Fact]
    public async Task Send_SerializationError()
    {
        var mockExampleService = new Mock<IExampleService>();

        bool? isSuccessful = null;

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ => NullLogger<MessageRouter<SqsMessageContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .AddTransient(_ => mockExampleService.Object)
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddSqs())
                )
            .Configure(app => app
                .UseSqs(sqs => sqs
                    .OnResponse("Check Response", context =>
                    {
                        isSuccessful = context.IsSuccessful;
                    }).UseMessageHandlers()
            )
        ).BuildHost();

        var request = RequestMother.CreateSerializationErrorPayload();
        SQSBatchResponse batchResponse = await host.SendSqsAsync(request);

        Assert.False(isSuccessful);
        Assert.NotEmpty(batchResponse.BatchItemFailures);
    }

    [Fact]
    public async Task Send_UnprocessableEntity()
    {
        var mockExampleService = new Mock<IExampleService>();

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .AddTransient(_ => mockExampleService.Object)
            .UsingBenzene(x => x
                .AddSqs());

        var pipeline = new MiddlewarePipelineBuilder<SqsMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        bool? isSuccessful = null;

        pipeline
                .OnResponse("Check Response", context =>
                {
                    isSuccessful = context.IsSuccessful;
                }).UseMessageHandlers();

        var aws = new SqsApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs();

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        await aws.HandleAsync(request, serviceResolverFactory);
        Assert.True(isSuccessful);
    }

    [Fact]
    public async Task SendSqsMessage()
    {
        var pipeline = new MiddlewarePipelineBuilder<SqsMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        var messageSent = "";

        pipeline.Use(null, (context, next) =>
        {
            messageSent = context.SqsMessage.Body;
            return next();
        });

        var aws = new SqsApplication(pipeline.Build());

        var request = MessageBuilder.Create("some-topic", Defaults.MessageAsObject).AsSqs();

        await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());

        Assert.Equal(Defaults.Message, messageSent);
    }

    [Fact]
    public async Task Send_FromStream()
    {
        SqsMessageContext sqsMessageContext = null;
        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));

        app.UseSqs(message => message
            .Use(null, (context, next) =>
            {
                sqsMessageContext = context;
                return next();
            })
        );

        var request = MessageBuilder.Create(null, Defaults.MessageAsObject).AsSqs();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        Assert.Equal(Defaults.Message, sqsMessageContext.SqsMessage.Body);
    }

    [Fact]
    public void SqsMessageMapper()
    {
        var sqsMessageMapper = new MessageGetter<SqsMessageContext>(new SqsMessageTopicGetter(), new SqsMessageBodyGetter(), new SqsMessageHeadersGetter());

        var sqsMessageContext = SqsMessageContext.CreateInstance(new SQSEvent(),
            new SQSEvent.SQSMessage
            {
                Body = Defaults.Message,
                MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
                {
                    {"topic", new SQSEvent.MessageAttribute { StringValue = Defaults.Topic}}
                }
            });

        var actualMessage = sqsMessageMapper.GetBody(sqsMessageContext);
        var actualTopic = sqsMessageMapper.GetTopic(sqsMessageContext);

        Assert.Equal(Defaults.Message, actualMessage);
        Assert.Equal(Defaults.Topic, actualTopic.Id);
    }

    [Fact]
    public async Task Send_BatchProcessFailureOnError()
    {
        var mockSqsClient = new Mock<ISqsClient>();
        mockSqsClient.Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("foo");

        var mockExampleService = new Mock<IExampleService>();
        mockExampleService.Setup(x => x.Register(It.IsAny<string>())).Throws<Exception>();
        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ =>
                NullLogger<MessageRouter<SqsMessageContext>>.Instance)
            .AddTransient<ILogger>(_ => NullLogger.Instance)
            .AddTransient(_ => mockExampleService.Object)
            .AddTransient(_ => mockSqsClient.Object)
            .UsingBenzene(x => x
                .AddSqs());

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        var pipeline = new MiddlewarePipelineBuilder<SqsMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline.UseMessageHandlers();

        var aws = new SqsApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs(5);

        SQSBatchResponse batchResponse = await aws.HandleAsync(request, serviceResolverFactory);
        Assert.Equal(5, batchResponse.BatchItemFailures.Count);
    }
    
    [Fact]
    public async Task SqsInSnsOut()
    {
        var mockAmazonSimpleNotificationService = new Mock<IAmazonSimpleNotificationService>();
        mockAmazonSimpleNotificationService
            .Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse());
        var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
        services.AddBenzeneMiddleware()
            .AddBenzene()
            .AddBenzeneMessage()
            .AddSqs();
        
        var appBuilder = new MiddlewarePipelineBuilder<SqsMessageContext>(services);
        
        var app = appBuilder
            .WhenIsTopic(Defaults.Topic.ToUpperInvariant(), 
                x => x
                    .SendToSns(mockAmazonSimpleNotificationService.Object))
            .Build();

        var entryPoint =
            new EntryPointMiddlewareApplication<SQSEvent, SQSBatchResponse>(new SqsApplication(app),
                services.CreateServiceResolverFactory());

        var exampleRequestPayload = new ExampleRequestPayload
        {
            Name = "foo"
        };

        var sqsEvent = MessageBuilder.Create(Defaults.Topic, exampleRequestPayload).AsSqs();
        var batchItems = await entryPoint.SendAsync(sqsEvent);
        
        mockAmazonSimpleNotificationService.Verify(x => 
            x.PublishAsync(It.Is<PublishRequest>(x => x
                .Message.Contains("foo") &&
                x.MessageAttributes["topic"].StringValue == Defaults.Topic
            ), It.IsAny<CancellationToken>()));
        Assert.Empty(batchItems.BatchItemFailures);
    }

    [Fact]
    public async Task SqsInKafkaOut()
    {
        var producer = new Mock<IProducer<string, string>>();
        producer
            .Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryReport<string, string>());
        var services = new MicrosoftBenzeneServiceContainer(new ServiceCollection());
        services.AddBenzeneMiddleware()
            .AddBenzene()
            .AddBenzeneMessage()
            .AddSqs();
        
        var appBuilder = new MiddlewarePipelineBuilder<SqsMessageContext>(services);
        
        var app = appBuilder
            .WhenIsTopic(Defaults.Topic.ToUpperInvariant(), 
                x => x
                    .SendToKafka(producer.Object))
            .Build();

        var entryPoint =
            new EntryPointMiddlewareApplication<SQSEvent, SQSBatchResponse>(new SqsApplication(app),
                services.CreateServiceResolverFactory());

        var exampleRequestPayload = new ExampleRequestPayload
        {
            Name = "foo"
        };

        var sqsEvent = MessageBuilder.Create(Defaults.Topic, exampleRequestPayload).AsSqs();
        var batchItems = await entryPoint.SendAsync(sqsEvent);
        
        producer.Verify(x => 
            x.ProduceAsync(Defaults.Topic, It.Is<Message<string, string>>(x => x
                .Value.Contains("foo") 
            ), It.IsAny<CancellationToken>()));
        Assert.Empty(batchItems.BatchItemFailures);
    }
}
