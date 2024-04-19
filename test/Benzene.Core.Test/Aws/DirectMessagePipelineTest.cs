using System.Threading.Tasks;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Aws.Core.DirectMessage;
using Benzene.Core.DirectMessage;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Tools;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws;

public class DirectMessagePipelineTest
{
    [Fact]
    public async Task SendDirectMessage()
    {
        var pipeline = new MiddlewarePipelineBuilder<DirectMessageContext>(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        pipeline.Use(null, (context, next) =>
        {
            context.DirectMessageResponse = new DirectMessageResponse
            {
                Message = context.DirectMessageRequest.Message,
                Headers = context.DirectMessageRequest.Headers,
                StatusCode = context.DirectMessageRequest.Topic == Defaults.Topic ? "200" : "503"
            };
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
    public async Task SendDirectMessage_FromStream()
    {
        DirectMessageContext DirectMessageContext = null;
        var app = new AwsEventStreamPipelineBuilder(new MicrosoftBenzeneServiceContainer(new ServiceCollection()));

        app.UseDirectMessage(DirectMessage => DirectMessage
            .Use(null, (context, next) =>
            {
                DirectMessageContext = context;
                context.DirectMessageResponse = new DirectMessageResponse
                {
                    Message = context.DirectMessageRequest.Message,
                    Headers = context.DirectMessageRequest.Headers,
                    StatusCode = context.DirectMessageRequest.Topic == Defaults.Topic ? "200" : "503"
                };
                return next();
            })
        );

        var request = RequestMother.CreateExampleEvent().AsDirectMessage();

        await app.Build().HandleAsync(AwsEventStreamContextBuilder.Build(request), ServiceResolverMother.CreateServiceResolver());

        Assert.Equal(Defaults.ResponseMessage, DirectMessageContext.DirectMessageResponse.Message);
        Assert.Equal("200", DirectMessageContext.DirectMessageResponse.StatusCode);
    }
}
