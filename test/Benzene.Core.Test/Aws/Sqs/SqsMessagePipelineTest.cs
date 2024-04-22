using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.Results;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Sqs;
using Benzene.Aws.Sqs.Client;
using Benzene.Aws.Sqs.TestHelpers;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
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

        IMessageResult messageResult = null;

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ => NullLogger<MessageRouter<SqsMessageContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .AddTransient(_ => mockExampleService.Object)
                .UsingBenzene(x => x
                    .AddSqs())
                )
            .Configure(app => app
                .UseSqs(sqs => sqs
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageRouter()
            )
        ).BuildHost();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs();

        SQSBatchResponse batchResponse = await host.SendSqsAsync(request);
        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);
        Assert.Empty(batchResponse.BatchItemFailures);
    }

    [Fact]
    public async Task Send_SerializationError()
    {
        var mockExampleService = new Mock<IExampleService>();

        IMessageResult messageResult = null;

        var host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .AddTransient<ILogger<MessageRouter<SqsMessageContext>>>(_ => NullLogger<MessageRouter<SqsMessageContext>>.Instance)
                .AddTransient<ILogger>(_ => NullLogger.Instance)
                .AddTransient(_ => mockExampleService.Object)
                .UsingBenzene(x => x
                    .AddSqs())
                )
            .Configure(app => app
                .UseSqs(sqs => sqs
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageRouter()
            )
        ).BuildHost();

        var request = RequestMother.CreateSerializationErrorPayload();
        SQSBatchResponse batchResponse = await host.SendSqsAsync(request);

        Assert.Equal(ServiceResultStatus.BadRequest, messageResult.Status);
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

        IMessageResult messageResult = null;

        pipeline
            .OnResponse("Check Response", context =>
            {
                messageResult = context.MessageResult;
            })
            .UseMessageRouter();

        var aws = new SqsApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs();

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        await aws.HandleAsync(request, serviceResolver);
        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);
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

        await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolver());

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
        var sqsMessageMapper = new MessageMapper<SqsMessageContext>(new SqsMessageTopicMapper(), new SqsMessageBodyMapper(), new SqsMessageHeadersMapper());

        var sqsMessageContext = SqsMessageContext.CreateInstance(new SQSEvent(),
            new SQSEvent.SQSMessage
            {
                Body = Defaults.Message,
                MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
                {
                    {"topic", new SQSEvent.MessageAttribute { StringValue = Defaults.Topic}}
                }
            });

        var actualMessage = sqsMessageMapper.GetMessage(sqsMessageContext);
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

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var pipeline = new MiddlewarePipelineBuilder<SqsMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
            .UseMessageRouter();

        var aws = new SqsApplication(pipeline.Build());

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSqs(5);

        SQSBatchResponse batchResponse = await aws.HandleAsync(request, serviceResolver);
        Assert.Equal(5, batchResponse.BatchItemFailures.Count);
    }

}
