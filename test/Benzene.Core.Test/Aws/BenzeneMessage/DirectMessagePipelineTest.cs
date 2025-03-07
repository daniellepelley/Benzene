﻿using System.Threading.Tasks;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.BenzeneMessage.TestHelpers;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.BenzeneMessage;

public class BenzeneMessagePipelineTest
{
    [Fact]
    public async Task SendBenzeneMessage()
    {
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(null, (context, next) =>
        {
            context.BenzeneMessageResponse = new BenzeneMessageResponse
            {
                Body = context.BenzeneMessageRequest.Body,
                Headers = context.BenzeneMessageRequest.Headers,
                StatusCode = context.BenzeneMessageRequest.Topic == Defaults.Topic ? "200" : "503"
            };
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
    public async Task SendBenzeneMessage_FromStream()
    {
        BenzeneMessageContext BenzeneMessageContext = null;
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        app.UseBenzeneMessage(BenzeneMessage => BenzeneMessage
            .Use(null, (context, next) =>
            {
                BenzeneMessageContext = context;
                context.BenzeneMessageResponse = new BenzeneMessageResponse
                {
                    Body = context.BenzeneMessageRequest.Body,
                    Headers = context.BenzeneMessageRequest.Headers,
                    StatusCode = context.BenzeneMessageRequest.Topic == Defaults.Topic ? "200" : "503"
                };
                return next();
            })
        );

        var request = RequestMother.CreateExampleEvent().AsBenzeneMessage();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.Equal(Defaults.ResponseMessage, BenzeneMessageContext.BenzeneMessageResponse.Body);
        Assert.Equal("200", BenzeneMessageContext.BenzeneMessageResponse.StatusCode);
    }
}
