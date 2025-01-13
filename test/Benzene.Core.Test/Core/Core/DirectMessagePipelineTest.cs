using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Serialization;
using Benzene.Core.DI;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.BenzeneMessage.TestHelpers;
using Benzene.Core.Logging;
using Benzene.Core.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Constants = Benzene.Core.Constants;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;
using Extensions = Benzene.Core.MessageHandlers.Extensions;

namespace Benzene.Test.Core.Core;

public class BenzeneMessagePipelineTest
{
    private const string OrderId = "some-order";

    [Fact]
    public async Task Send()
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        services
                .AddTransient<ISerializer, JsonSerializer>()
                .AddTransient<JsonSerializer>()
                .UsingBenzene(x => Extensions.AddMessageHandlers(x
                        .AddBenzeneMessage(), typeof(ExampleRequestPayload).Assembly));


        var pipeline = PipelineMother.BasicBenzeneMessagePipeline(new MicrosoftBenzeneServiceContainer(services));

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();

        var response = await aws.HandleAsync(request, serviceResolverFactory);

        Assert.NotNull(response);
        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task SendV2()
    {
        var services = new ServiceCollection();
        services
                .AddTransient<ISerializer, JsonSerializer>()
                .AddTransient<JsonSerializer>()
                .UsingBenzene(x => x
                        .AddBenzene() 
                        .AddBenzeneMessage()
                        .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));


        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline.UseMessageHandlers();

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = RequestMother
            .CreateExampleEvent()
            .WithHeaders(new Dictionary<string, string>
            {
                { "sender", "some-sender" },
                { "version", "2.0"}
            })
            .AsBenzeneMessage();

        var response = await aws.HandleAsync(request, serviceResolverFactory);

        Assert.NotNull(response);
        Assert.Equal(BenzeneResultStatus.Deleted, response.StatusCode);
    }

    [Fact]
    public async Task SendNoResponse()
    {
        var services = new ServiceCollection();
        services
                .AddTransient<IBenzeneLogger, BenzeneLogger>()
                .AddTransient<ISerializer, JsonSerializer>()
                .AddTransient<JsonSerializer>()
                .UsingBenzene(x => x
                        .AddBenzene()
                        .AddContextItems()
                        .AddBenzeneMessage()
                        .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));


        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline.UseMessageHandlers();

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.TopicNoResponse,
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = "foo"
            }),
            Headers = new Dictionary<string, string>
            {
                { "sender", "some-sender" }
            }
        };

        var serviceResolverFactory = new MicrosoftServiceResolverFactory(services);
        var response = await aws.HandleAsync(request, serviceResolverFactory);

        Assert.NotNull(response);
        Assert.Equal(BenzeneResultStatus.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task SendBenzeneMessage_UseFunc()
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(null, (context, next) =>
        {
            context.BenzeneMessageResponse = new BenzeneMessageResponse
            {
                Body = context.BenzeneMessageRequest.Body,
                Headers = context.BenzeneMessageRequest.Headers,
                StatusCode = context.BenzeneMessageRequest.Topic == Defaults.Topic ? "200" : "503",
            };
            // context.MessageResult = new MessageResult(new Topic(Defaults.Topic), null, "", true, Defaults.ResponseMessage, Array.Empty<string>());
            return next();
        });

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();

        var response = await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());

        Assert.NotNull(response);
        Assert.Equal(Defaults.ResponseMessage, response.Body);
        Assert.Equal("200", response.StatusCode);
    }

    [Fact]
    public async Task SendBenzeneMessage_MultiApplication()
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        var responseStatus = string.Empty;

        pipeline.Use(null, (context, next) =>
        {
            context.BenzeneMessageResponse = new BenzeneMessageResponse
            {
                Body = context.BenzeneMessageRequest.Body,
                Headers = context.BenzeneMessageRequest.Headers,
                StatusCode = context.BenzeneMessageRequest.Topic == Defaults.Topic ? "200" : "503",
            };
            responseStatus = context.BenzeneMessageResponse.StatusCode;
            // context.MessageResult = new MessageResult(new Topic(Defaults.Topic), null, "", true, Defaults.ResponseMessage, Array.Empty<string>());
            return next();
        });

        var aws = new MiddlewareMultiApplication<BenzeneMessageRequest, BenzeneMessageContext>(pipeline.Build(), x => new[]
        {
            new BenzeneMessageContext(x)
        });

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = Defaults.Message,
            Headers = new Dictionary<string, string>
            {
                { "header1", "foo" }
            }
        };

        await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());
        Assert.Equal("200", responseStatus);
    }

    [Fact]
    public async Task SendBenzeneMessage_UseMiddleware()
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(new FuncWrapperMiddleware<BenzeneMessageContext>((context, next) =>
        {
            context.BenzeneMessageResponse = new BenzeneMessageResponse
            {
                Body = context.BenzeneMessageRequest.Body,
                Headers = context.BenzeneMessageRequest.Headers,
                StatusCode = context.BenzeneMessageRequest.Topic == Defaults.Topic ? "200" : "503"
            };
            return next();
        }));

        var aws = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = Defaults.Message,
            Headers = new Dictionary<string, string>
            {
                { "header1", "foo" }
            }
        };

        var response = await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolverFactory());

        Assert.NotNull(response);
        Assert.Equal(Defaults.Message, response.Body);
        Assert.Equal("200", response.StatusCode);
    }

    [Fact]
    public void BenzeneMessageMapper()
    {
        var benzeneMessageMapper = new BenzeneMessageGetter();

        var benzeneMessageContext = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Topic = Defaults.Topic,
            Body = Defaults.Message,
            Headers = new Dictionary<string, string>
            {
                { "orderId", OrderId }
            }
        });

        var actualMessage = benzeneMessageMapper.GetBody(benzeneMessageContext);
        var actualTopic = benzeneMessageMapper.GetTopic(benzeneMessageContext);
        var actualOrder = benzeneMessageMapper.GetHeader(benzeneMessageContext, "orderId");

        Assert.Equal(Defaults.Message, actualMessage);
        Assert.Equal(Defaults.Topic, actualTopic.Id);
        Assert.Equal(OrderId, actualOrder);
    }

    [Fact]
    public void BenzeneMessageMapper_Empty()
    {
        var benzeneMessageMapper = new BenzeneMessageGetter();

        var benzeneMessageContext = new BenzeneMessageContext(new BenzeneMessageRequest());

        var actualMessage = benzeneMessageMapper.GetBody(benzeneMessageContext);
        var actualTopic = benzeneMessageMapper.GetTopic(benzeneMessageContext);
        var actualOrder = benzeneMessageMapper.GetHeader(benzeneMessageContext, "orderId");

        Assert.Null(actualMessage);
        Assert.Equal(Constants.Missing, actualTopic.Id);
        Assert.Null(actualOrder);
    }
}
