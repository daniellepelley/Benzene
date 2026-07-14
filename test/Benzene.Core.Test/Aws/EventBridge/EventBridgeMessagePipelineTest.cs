using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Aws.Lambda.EventBridge;
using Benzene.Aws.Lambda.EventBridge.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Aws.Helpers;
using Benzene.Test.Examples;
using Benzene.Testing;
using Xunit;

namespace Benzene.Test.Aws.EventBridge;

public class EventBridgeMessagePipelineTest
{
    [Fact]
    public async Task Send()
    {
        IMessageResult messageResult = null;

        var host = new EntryPointMiddleApplicationBuilder<EventBridgeEvent, EventBridgeContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddEventBridge());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageHandlers())
            .Build(x => new EventBridgeApplication(x));

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsEventBridge();

        await host.SendAsync(request);

        Assert.True(messageResult.IsSuccessful);
    }

    [Fact]
    public async Task Send_UnknownDetailType_ReturnsNotFoundResult()
    {
        IMessageResult messageResult = null;

        var host = new EntryPointMiddleApplicationBuilder<EventBridgeEvent, EventBridgeContext>()
            .ConfigureServices(services =>
            {
                services
                    .ConfigureServiceCollection()
                    .UsingBenzene(x => x.AddEventBridge());
            })
            .Configure(app => app
                .OnResponse("Check Response", context =>
                {
                    messageResult = context.MessageResult;
                })
                .UseMessageHandlers())
            .Build(x => new EventBridgeApplication(x));

        var request = MessageBuilder.Create("no.such.detail-type", Defaults.MessageAsObject).AsEventBridge();

        await host.SendAsync(request);

        Assert.False(messageResult.IsSuccessful);
    }
}
