using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.MessageHandlers.ToDelete;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Sns;
using Benzene.Aws.Lambda.Sns.TestHelpers;
using Benzene.Clients;
using Benzene.Clients.Aws.Sns;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.FluentValidation;
using Benzene.Kafka.Core.Kafka;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Aws.Sns.Examples;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Xml;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;
using Topic = Benzene.Core.Messages.Topic;
using XmlSerializer = Benzene.Xml.XmlSerializer;

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
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddSns());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageHandlers())
            .Build(x => new SnsApplication(x));

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSns();

        await host.SendAsync(request);

        Assert.True(messageResult.IsSuccessful);
    }

    [Fact]
    public async Task Send_Xml()
    {
        IMessageResult messageResult = null;

        var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x
                        .AddXml()
                        .AddSns());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageHandlers(x => x.UseFluentValidation()))
            .Build(x => new SnsApplication(x));

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload { Name = "foo" })
            .WithHeader("content-type", "application/xml")
            .AsSns(new XmlSerializer());

        await host.SendAsync(request);

        Assert.True(messageResult.IsSuccessful);
    }

    [Fact]
    public async Task Send_Unprocessable_Entity()
    {
        var mockExampleService = new Mock<IExampleService>();

        var services = ServiceResolverMother.CreateServiceCollection();
        services
            .AddTransient(_ => mockExampleService.Object)
            .UsingBenzene(x => x.AddSns());

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);

        var pipeline = new MiddlewarePipelineBuilder<SnsRecordContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        IMessageResult messageResult = null;

        pipeline
            .OnResponse("Check Response", context =>
            {
                messageResult = context.MessageResult;
            })
            .UseMessageHandlers();

        var aws = new SnsApplication(pipeline.Build());

        var request = CreateRequest();

        await aws.HandleAsync(request, serviceResolverFactory);
        Assert.True(messageResult.IsSuccessful);
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

        await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());

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
        var sqsMessageMapper = new MessageGetter<SnsRecordContext>(new SnsMessageTopicGetter(), new SnsMessageBodyGetter(), new SnsMessageHeadersGetter());

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
                .UseMessageHandlers(x => x
                    .UseFluentValidation()
                ))
            .Build(x => new SnsApplication(x));

        var request = CreateRequest();
        request.Records[0].Sns.MessageId = "foo";

        await host.SendAsync(request);

        Assert.True(messageResult.IsSuccessful);
    }

    [Fact]
    public async Task SendSnsToSqs()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();

        var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddSns());
            })
            .Configure(app => Benzene.Core.Middleware.Extensions
                .Convert<SnsRecordContext, BenzeneMessageContext>(app, new InlineContextConverter<SnsRecordContext, BenzeneMessageContext>(x => new BenzeneMessageContext(new BenzeneMessageRequest
                {
                    Body = x.SnsRecord.Sns.Message,
                    Headers = x.SnsRecord.Sns.MessageAttributes.ToDictionary(d => d.Key, d => d.Value.Value)
                }), (x1, x2) => x1.MessageResult = new MessageResult(true)), builder =>
                {
                    Benzene.Core.Middleware.Extensions.Convert(builder, new InlineContextConverter<BenzeneMessageContext, SqsSendMessageContext>(x =>
                            new SqsSendMessageContext(new SendMessageRequest
                            {
                                MessageBody = x.BenzeneMessageRequest.Body,
                                MessageAttributes = x.BenzeneMessageRequest.Headers.ToDictionary(d => d.Key, d => new MessageAttributeValue { StringValue = d.Value })
                            }), (x1, x2) => x1.BenzeneMessageResponse.StatusCode = BenzeneResultStatus.Ok),
                        builder1 => builder1.UseSqsClient(mockSqsClient.Object)
                    );
                }))
            .Build(x => new SnsApplication(x));

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSns();

        await host.SendAsync(request);

        mockSqsClient.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequest>(m =>
            m.MessageBody == Defaults.Message &&
            m.MessageAttributes["topic"].StringValue == Defaults.Topic
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendSnsToSqs2()
    {
        var mockSqsClient = new Mock<IAmazonSQS>();

        var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddSns());
            })
            .Configure(app => app
                .ToBenzeneMessage(builder =>
                {
                    Benzene.Core.Middleware.Extensions.Convert(builder, new InlineContextConverter<BenzeneMessageContext, SqsSendMessageContext>(x =>
                            new SqsSendMessageContext(new SendMessageRequest
                            {
                                MessageBody = x.BenzeneMessageRequest.Body,
                                MessageAttributes = x.BenzeneMessageRequest.Headers.ToDictionary(d => d.Key, d => new MessageAttributeValue { StringValue = d.Value })
                            }), (x1, x2) => x1.BenzeneMessageResponse.StatusCode = BenzeneResultStatus.Ok),
                        builder1 => builder1.UseSqsClient(mockSqsClient.Object)
                    );
                }))
            .Build(x => new SnsApplication(x));

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSns();

        await host.SendAsync(request);

        mockSqsClient.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequest>(m =>
            m.MessageBody == Defaults.Message &&
            m.MessageAttributes["topic"].StringValue == Defaults.Topic
            ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SendSnsToSqs3()
    {
        var isSuccessful = false;
        var mockSqsClient = new Mock<IAmazonSQS>();
        mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        var host = new EntryPointMiddleApplicationBuilder<SNSEvent, SnsRecordContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddSns());
            })
            .Configure(app => app
                .OnResponse(x => isSuccessful = x.MessageResult.IsSuccessful)
                .ToSqsClientMessage(builder => builder.UseSqsClient(mockSqsClient.Object)
                ))
            .Build(x => new SnsApplication(x));

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsSns();

        await host.SendAsync(request);

        mockSqsClient.Verify(x => x.SendMessageAsync(It.Is<SendMessageRequest>(m =>
            m.MessageBody == Defaults.Message &&
            m.MessageAttributes["topic"].StringValue == Defaults.Topic
            ), It.IsAny<CancellationToken>()));

        Assert.True(isSuccessful);
    }
}

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> ToBenzeneMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> source, Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>> builder)
    {
        var pipeline = source.CreateMiddlewarePipeline(builder);

        return source.Use(resolver =>
            new ConverterMiddleware<TContext, BenzeneMessageContext>(new BenzeneMessageContextConverter<TContext>(
                    resolver.GetService<IMessageBodyGetter<TContext>>(),
                    resolver.GetService<IMessageHeadersGetter<TContext>>(),
                    resolver.GetService<IMessageTopicGetter<TContext>>(),
                    resolver.GetService<IMessageHandlerResultSetter<TContext>>()
                ),
                pipeline,
                resolver
            ));
    }

    public static IMiddlewarePipelineBuilder<TContext> ToSqsClientMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> source, Action<IMiddlewarePipelineBuilder<SqsSendMessageContext>> builder)
    {
        var pipeline = source.CreateMiddlewarePipeline(builder);

        return source.Use(resolver =>
            new ConverterMiddleware<TContext, SqsSendMessageContext>(new SqsMessageContextConverter<TContext>(
                    resolver.GetService<IMessageBodyGetter<TContext>>(),
                    resolver.GetService<IMessageHeadersGetter<TContext>>(),
                    resolver.TryGetService<IMessageHandlerResultSetter<TContext>>(),
                    resolver.TryGetService<IBenzeneResponseAdapter<TContext>>()
                ),
                pipeline,
                resolver
            ));
    }

    public static IMiddlewarePipelineBuilder<TContext> ToSnsClientMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> source, Action<IMiddlewarePipelineBuilder<SnsSendMessageContext>> builder)
    {
        var pipeline = source.CreateMiddlewarePipeline(builder);

        return source.Use(resolver =>
            new ConverterMiddleware<TContext, SnsSendMessageContext>(new SnsMessageContextConverter<TContext>(
                    resolver.GetService<IMessageBodyGetter<TContext>>(),
                    resolver.GetService<IMessageHeadersGetter<TContext>>(),
                    resolver.TryGetService<IMessageHandlerResultSetter<TContext>>(),
                    resolver.TryGetService<IBenzeneResponseAdapter<TContext>>()
                ),
                pipeline,
                resolver
            ));
    }

    public static IMiddlewarePipelineBuilder<TContext> ToKafkaClientMessage<TContext>(
        this IMiddlewarePipelineBuilder<TContext> source, Action<IMiddlewarePipelineBuilder<KafkaSendMessageContext>> builder)
    {
        var pipeline = source.CreateMiddlewarePipeline(builder);

        return source.Use(resolver =>
            new ConverterMiddleware<TContext, KafkaSendMessageContext>(new KafkaMessageContextConverter<TContext>(
                    resolver.GetService<IMessageTopicGetter<TContext>>(),
                    resolver.GetService<IMessageBodyGetter<TContext>>(),
                    resolver.GetService<IMessageHeadersGetter<TContext>>(),
                    resolver.TryGetService<IMessageHandlerResultSetter<TContext>>(),
                    resolver.TryGetService<IBenzeneResponseAdapter<TContext>>()
                ),
                pipeline,
                resolver
            ));
    }

    public static IMiddlewarePipelineBuilder<TContext> SendToSns<TContext>(
        this IMiddlewarePipelineBuilder<TContext> source,
        IAmazonSimpleNotificationService amazonSns)
    {
        return source.ToSnsClientMessage(x => x
            .UseSnsClient(amazonSns));
    }

    public static IMiddlewarePipelineBuilder<TContext> SendToKafka<TContext>(
        this IMiddlewarePipelineBuilder<TContext> source,
        IProducer<string, string> producer)
    {
        return source.ToKafkaClientMessage(x => x
            .UseKafkaClient(producer));
    }

    public static IMiddlewarePipelineBuilder<TContext> WhenIsTopic<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string topic, Action<IMiddlewarePipelineBuilder<TContext>> builder)
    {
        return app.WhenIsTopic(topic, false, builder);
    }

    public static IMiddlewarePipelineBuilder<TContext> WhenIsTopic<TContext>(this IMiddlewarePipelineBuilder<TContext> app,
        string topic, bool isCaseSensitive, Action<IMiddlewarePipelineBuilder<TContext>> builder)
    {
        var newApp = app.Create<TContext>();
        builder(newApp);

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("WhenIsTopic", async (context, next) =>
        {
            var getTopic = resolver.GetService<IMessageTopicGetter<TContext>>();
            if (getTopic.Is(context, topic, isCaseSensitive))
            {
                await newApp.Build().HandleAsync(context, resolver);
            }
            else
            {
                await next();
            }
        }));
    }
}

public class ConverterMiddleware<TContextIn, TContextOut> : IMiddleware<TContextIn>
{
    private readonly IContextConverter<TContextIn, TContextOut> _converter;
    private readonly IMiddlewarePipeline<TContextOut> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    public ConverterMiddleware(IContextConverter<TContextIn, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _converter = converter;
    }

    public string Name => "Convert";

    public async Task HandleAsync(TContextIn context, Func<Task> next)
    {
        var contextOut = await _converter.CreateRequestAsync(context);
        await _middlewarePipeline.HandleAsync(contextOut, _serviceResolver);
        await _converter.MapResponseAsync(context, contextOut);
    }
}

public class BenzeneMessageContextConverter<TContext> : IContextConverter<TContext, BenzeneMessageContext>
{
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageTopicGetter<TContext> _messageTopicGetter;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;

    public BenzeneMessageContextConverter(IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter, IMessageTopicGetter<TContext> messageTopicGetter, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter)
    {
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _messageTopicGetter = messageTopicGetter;
        _messageHeadersGetter = messageHeadersGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    public Task<BenzeneMessageContext> CreateRequestAsync(TContext contextIn)
    {
        return Task.FromResult(new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Topic = _messageTopicGetter?.GetTopic(contextIn)?.Id,
            Body = _messageBodyGetter?.GetBody(contextIn),
            Headers = _messageHeadersGetter.GetHeaders(contextIn),
        }));
    }

    public Task MapResponseAsync(TContext contextIn, BenzeneMessageContext contextOut)
    {
        _messageHandlerResultSetter.SetResultAsync(contextIn,
            new MessageHandlerResult(new Topic(contextOut.BenzeneMessageRequest.Topic),
                MessageHandlerDefinition.Empty(), BenzeneResult.Set(contextOut.BenzeneMessageResponse.StatusCode)));
        return Task.CompletedTask;
    }
}

public class SqsMessageContextConverter<TContext> : IContextConverter<TContext, SqsSendMessageContext>
{
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public SqsMessageContextConverter(IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter, IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _messageHeadersGetter = messageHeadersGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    public Task<SqsSendMessageContext> CreateRequestAsync(TContext contextIn)
    {
        return Task.FromResult(new SqsSendMessageContext(new SendMessageRequest
        {
            QueueUrl = "",
            MessageBody = _messageBodyGetter.GetBody(contextIn),
            MessageAttributes = _messageHeadersGetter.GetHeaders(contextIn).ToDictionary(d => d.Key, d => new MessageAttributeValue { StringValue = d.Value })
        }));
    }

    public Task MapResponseAsync(TContext contextIn, SqsSendMessageContext contextOut)
    {
        if (_benzeneResponseAdapter != null)
        {
            _benzeneResponseAdapter.SetStatusCode(contextIn, contextOut.Response.HttpStatusCode.ToString());
            return Task.CompletedTask;
        }

        _messageHandlerResultSetter.SetResultAsync(contextIn,
            new MessageHandlerResult(new Topic(contextOut.Request.MessageAttributes["topic"].StringValue),
                MessageHandlerDefinition.Empty(), BenzeneResult.Set(contextOut.Response.HttpStatusCode.ToString())));

        return Task.CompletedTask;
    }
}

public class SnsMessageContextConverter<TContext> : IContextConverter<TContext, SnsSendMessageContext>
{
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public SnsMessageContextConverter(IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter, IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _messageHeadersGetter = messageHeadersGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    public Task<SnsSendMessageContext> CreateRequestAsync(TContext contextIn)
    {
        return Task.FromResult(new SnsSendMessageContext(new PublishRequest
        {
            Message = _messageBodyGetter.GetBody(contextIn),
            MessageAttributes = _messageHeadersGetter.GetHeaders(contextIn).ToDictionary(d => d.Key, d => new Amazon.SimpleNotificationService.Model.MessageAttributeValue { StringValue = d.Value })
        }));
    }

    public Task MapResponseAsync(TContext contextIn, SnsSendMessageContext contextOut)
    {
        if (_benzeneResponseAdapter != null)
        {
            _benzeneResponseAdapter.SetStatusCode(contextIn, contextOut.Response.HttpStatusCode.ToString());
            return Task.CompletedTask;
        }

        _messageHandlerResultSetter.SetResultAsync(contextIn,
            new MessageHandlerResult(new Topic(contextOut.Request.MessageAttributes["topic"].StringValue),
                MessageHandlerDefinition.Empty(), BenzeneResult.Set(contextOut.Response.HttpStatusCode.ToString())));

        return Task.CompletedTask;
    }
}

public class KafkaMessageContextConverter<TContext> : IContextConverter<TContext, KafkaSendMessageContext>
{
    private readonly IMessageTopicGetter<TContext> _messageTopicGetter;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;
    private readonly IMessageHeadersGetter<TContext> _messageHeadersGetter;
    private readonly IMessageHandlerResultSetter<TContext> _messageHandlerResultSetter;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public KafkaMessageContextConverter(IMessageTopicGetter<TContext> messageTopicGetter, IMessageBodyGetter<TContext> messageBodyGetter, IMessageHeadersGetter<TContext> messageHeadersGetter, IMessageHandlerResultSetter<TContext> messageHandlerResultSetter, IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _messageTopicGetter = messageTopicGetter;
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _messageHandlerResultSetter = messageHandlerResultSetter;
        _messageHeadersGetter = messageHeadersGetter;
        _messageBodyGetter = messageBodyGetter;
    }

    public Task<KafkaSendMessageContext> CreateRequestAsync(TContext contextIn)
    {
        return Task.FromResult(new KafkaSendMessageContext(
            _messageTopicGetter.GetTopic(contextIn).Id,
            new Message<string, string>
            {
                Value = _messageBodyGetter.GetBody(contextIn)
            }
        ));
    }

    public Task MapResponseAsync(TContext contextIn, KafkaSendMessageContext contextOut)
    {
        if (_benzeneResponseAdapter != null)
        {
            _benzeneResponseAdapter.SetStatusCode(contextIn, "Ok");
            return Task.CompletedTask;
        }

        _messageHandlerResultSetter.SetResultAsync(contextIn,
            new MessageHandlerResult(new Topic(contextOut.Topic),
                MessageHandlerDefinition.Empty(), BenzeneResult.Set("Ok")));

        return Task.CompletedTask;
    }
}

