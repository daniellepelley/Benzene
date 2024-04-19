using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Serialization;
using Benzene.Core.DI;
using Benzene.Core.DirectMessage;
using Benzene.Core.Logging;
using Benzene.Core.Mappers;
using Benzene.Core.Middleware;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Core.Response;
using Benzene.Core.Results;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Constants = Benzene.Core.Constants;
using JsonSerializer = Benzene.Core.Serialization.JsonSerializer;

namespace Benzene.Test.Core.Core;

public class DirectMessagePipelineTest
{
    private const string OrderId = "some-order";

    [Fact]
    public async Task Send()
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        services
                .AddTransient<ResponseMiddleware<DirectMessageContext>>()
                .AddTransient<ISerializer, JsonSerializer>()
                .AddTransient<JsonSerializer>()
                .UsingBenzene(x => x
                    .AddDirectMessage()
                    .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));


        var pipeline = PipelineMother.BasicDirectMessagePipeline(new MicrosoftBenzeneServiceContainer(services));

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        var aws = new DirectMessageApplication(pipeline.Build());

        var request = RequestMother.CreateExampleEvent().AsDirectMessage();

        var response = await aws.HandleAsync(request, serviceResolver);

        Assert.NotNull(response);
        Assert.Equal(ServiceResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public async Task SendV2()
    {
        var services = new ServiceCollection();
        services
                .AddTransient<ResponseMiddleware<DirectMessageContext>>()
                .AddTransient<ISerializer, JsonSerializer>()
                .AddTransient<JsonSerializer>()
                .UsingBenzene(x => x
                    .AddBenzene() 
                    .AddDirectMessage()
                    .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));


        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
            .UseProcessResponse()
            .UseMessageRouter();

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        var aws = new DirectMessageApplication(pipeline.Build());

        var request = RequestMother
            .CreateExampleEvent()
            .WithHeaders(new Dictionary<string, string>
            {
                { "sender", "some-sender" },
                { "version", "2.0"}
            })
            .AsDirectMessage();

        var response = await aws.HandleAsync(request, serviceResolver);

        Assert.NotNull(response);
        Assert.Equal(ServiceResultStatus.Deleted, response.StatusCode);
    }

    [Fact]
    public async Task SendNoResponse()
    {
        var services = new ServiceCollection();
        services
                .AddTransient<IBenzeneLogger, BenzeneLogger>()
                .AddTransient<ResponseMiddleware<DirectMessageContext>>()
                .AddTransient<ISerializer, JsonSerializer>()
                .AddTransient<JsonSerializer>()
                .UsingBenzene(x => x
                    .AddBenzene()
                    .AddDirectMessage()
                    .AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));


        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(services));

        pipeline
            .UseProcessResponse()
            .UseMessageRouter();

        var aws = new DirectMessageApplication(pipeline.Build());

        var request = new DirectMessageRequest
        {
            Topic = Defaults.TopicNoResponse,
            Message = JsonConvert.SerializeObject(new ExampleRequestPayload
            {
                Name = "foo"
            }),
            Headers = new Dictionary<string, string>
            {
                { "sender", "some-sender" }
            }
        };

        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();
        var response = await aws.HandleAsync(request, serviceResolver);

        Assert.NotNull(response);
        Assert.Equal(ServiceResultStatus.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task SendDirectMessage_UseFunc()
    {
        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(null, (context, next) =>
        {
            context.DirectMessageResponse = new DirectMessageResponse
            {
                Message = context.DirectMessageRequest.Message,
                Headers = context.DirectMessageRequest.Headers,
                StatusCode = context.DirectMessageRequest.Topic == Defaults.Topic ? "200" : "503",
            };
            context.MessageResult = new MessageResult(Defaults.Topic, null, "", true, Defaults.ResponseMessage, Array.Empty<string>());
            return next();
        });

        var aws = new DirectMessageApplication(pipeline.Build());

        var request = RequestMother.CreateExampleEvent().AsDirectMessage();

        var response = await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolver());

        Assert.NotNull(response);
        Assert.Equal(Defaults.ResponseMessage, response.Message);
        Assert.Equal("200", response.StatusCode);
    }

    [Fact]
    public async Task SendDirectMessage_MultiApplication()
    {
        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));
        var responseStatus = string.Empty;

        pipeline.Use(null, (context, next) =>
        {
            context.DirectMessageResponse = new DirectMessageResponse
            {
                Message = context.DirectMessageRequest.Message,
                Headers = context.DirectMessageRequest.Headers,
                StatusCode = context.DirectMessageRequest.Topic == Defaults.Topic ? "200" : "503",
            };
            responseStatus = context.DirectMessageResponse.StatusCode;
            context.MessageResult = new MessageResult(Defaults.Topic, null, "", true, Defaults.ResponseMessage, Array.Empty<string>());
            return next();
        });

        var aws = new MiddlewareMultiApplication<DirectMessageRequest, DirectMessageContext>("foo", pipeline.Build(), x => new[]
        {
            DirectMessageContext.CreateInstance(x)
        });

        var request = new DirectMessageRequest
        {
            Topic = Defaults.Topic,
            Message = Defaults.Message,
            Headers = new Dictionary<string, string>
            {
                { "header1", "foo" }
            }
        };

        await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolver());
        Assert.Equal("200", responseStatus);
    }

    [Fact]
    public async Task SendDirectMessage_UseMiddleware()
    {
        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(new FuncWrapperMiddleware<DirectMessageContext>((context, next) =>
        {
            context.DirectMessageResponse = new DirectMessageResponse
            {
                Message = context.DirectMessageRequest.Message,
                Headers = context.DirectMessageRequest.Headers,
                StatusCode = context.DirectMessageRequest.Topic == Defaults.Topic ? "200" : "503"
            };
            return next();
        }));

        var aws = new DirectMessageApplication(pipeline.Build());

        var request = new DirectMessageRequest
        {
            Topic = Defaults.Topic,
            Message = Defaults.Message,
            Headers = new Dictionary<string, string>
            {
                { "header1", "foo" }
            }
        };

        var response = await aws.HandleAsync(request, ServiceResolverMother.CreateServiceResolver());

        Assert.NotNull(response);
        Assert.Equal(Defaults.Message, response.Message);
        Assert.Equal("200", response.StatusCode);
    }

    [Fact]
    public void DirectMessageMapper()
    {
        var benzeneMessageMapper = new DirectMessageMapper();

        var benzeneMessageContext = DirectMessageContext.CreateInstance(new DirectMessageRequest
        {
            Topic = Defaults.Topic,
            Message = Defaults.Message,
            Headers = new Dictionary<string, string>
            {
                { "orderId", OrderId }
            }
        });

        var actualMessage = benzeneMessageMapper.GetMessage(benzeneMessageContext);
        var actualTopic = benzeneMessageMapper.GetTopic(benzeneMessageContext);
        var actualOrder = benzeneMessageMapper.GetHeader(benzeneMessageContext, "orderId");

        Assert.Equal(Defaults.Message, actualMessage);
        Assert.Equal(Defaults.Topic, actualTopic.Id);
        Assert.Equal(OrderId, actualOrder);
    }

    [Fact]
    public void DirectMessageMapper_Empty()
    {
        var benzeneMessageMapper = new DirectMessageMapper();

        var benzeneMessageContext = DirectMessageContext.CreateInstance(new DirectMessageRequest());

        var actualMessage = benzeneMessageMapper.GetMessage(benzeneMessageContext);
        var actualTopic = benzeneMessageMapper.GetTopic(benzeneMessageContext);
        var actualOrder = benzeneMessageMapper.GetHeader(benzeneMessageContext, "orderId");

        Assert.Null(actualMessage);
        Assert.Equal(Constants.Missing, actualTopic.Id);
        Assert.Null(actualOrder);
    }
}
