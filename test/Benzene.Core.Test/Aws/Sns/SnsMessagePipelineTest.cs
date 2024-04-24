using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Results;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Sns;
using Benzene.Aws.Sns.TestHelpers;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandling;
using Benzene.Core.MiddlewareBuilder;
using Benzene.FluentValidation;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Aws.Sns.Examples;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sns;

public class SnsMessagePipelineTest
{
    private static SNSEvent CreateRequest()
    {
        return MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSns();
    }

    [Fact]
    public async Task Send()
    {
        IMessageResult messageResult = null;

        var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
            .ConfigureServices(services =>
            {
                ServiceResolverMother.ConfigureServiceCollection(services);
                services
                    .UsingBenzene(x => x.AddSns());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageRouter(x => x.UseFluentValidation()))
            .Build(x => new SnsApplication(x));

        var request = CreateRequest();

        await host.HandleAsync(request);

        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);
    }

    [Fact]
    public async Task Send_Unprocessable_Entity()
    {
        var mockExampleService = new Mock<IExampleService>();

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient(_ => mockExampleService.Object)
            .UsingBenzene(x => x.AddSns());

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var pipeline = new MiddlewarePipelineBuilder<SnsRecordContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        IMessageResult messageResult = null;

        pipeline
            .OnResponse("Check Response", context =>
            {
                messageResult = context.MessageResult;
            })
            .UseMessageRouter();

        var aws = new SnsApplication(pipeline.Build());

        var request = CreateRequest();

        await aws.HandleAsync(request, serviceResolver);
        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);
    }

    [Fact]
    public async Task Send_Pipeline()
    {
        var pipeline = new MiddlewarePipelineBuilder<SnsRecordContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        var messageSent = "";

        pipeline.Use(null, (context, next) =>
        {
            messageSent = context.SnsRecord.Sns.Message;
            return next();
        });

        var aws = new SnsApplication(pipeline.Build());

        var request = new SNSEvent
        {
            Records = new[]
            {
                new SNSEvent.SNSRecord
                {
                    Sns = new SNSEvent.SNSMessage
                    {
                        Message = Defaults.Message
                    }
                }
            }
        };

        await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolver());

        Assert.Equal(Defaults.Message, messageSent);
    }

    [Fact]
    public async Task Send_FromStream()
    {
        SnsRecordContext snsRecordContext = null;

        var services = ServiceResolverMother.CreateServiceCollection();
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(services));

        app.UseSns(message => message
            .Use(null, (context, next) =>
            {
                snsRecordContext = context;
                return next();
            })
        );

        var request = new SNSEvent
        {
            Records = new[]
            {
                new SNSEvent.SNSRecord
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>(),
                        Message = Defaults.Message
                    }
                }
            }
        };

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()));

        Assert.Equal(Defaults.Message, snsRecordContext.SnsRecord.Sns.Message);
    }

    [Fact]
    public void SnsMessageMapper()
    {
        var sqsMessageMapper = new MessageMapper<SnsRecordContext>(new SnsMessageTopicMapper(), new SnsMessageBodyMapper(), new SnsMessageHeadersMapper());

        var sqsMessageContext = SnsRecordContext.CreateInstance(new SNSEvent(), new SNSEvent.SNSRecord
        {
            Sns = new SNSEvent.SNSMessage
            {
                Message = Defaults.Message,
                MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>
                {
                    { "topic", new SNSEvent.MessageAttribute { Value = Defaults.Topic } },
                }
            }
        });

        var actualMessage = sqsMessageMapper.GetBody(sqsMessageContext);
        var actualTopic = sqsMessageMapper.GetTopic(sqsMessageContext);
        var actualOrder = sqsMessageMapper.GetHeader(sqsMessageContext, "orderId");

        Assert.Equal(Defaults.Message, actualMessage);
        Assert.Equal(Defaults.Topic, actualTopic.Id);
    }

    [Fact]
    public async Task Send_Enricher()
    {
        IMessageResult messageResult = null;

        var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
            .ConfigureServices(services =>
            {
                ServiceResolverMother.ConfigureServiceCollection(services);
                services
                    .AddScoped<IRequestEnricher<SnsRecordContext>, CustomSnsEnricher>()
                    .UsingBenzene(x => x.AddSns());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageRouter(x => x
                    .UseFluentValidation()
                ))
            .Build(x => new SnsApplication(x));

        var request = CreateRequest();
        request.Records[0].Sns.MessageId = "foo";

        await host.HandleAsync(request);

        Assert.Equal(ServiceResultStatus.Ok, messageResult.Status);
    }

}
