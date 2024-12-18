using System.Threading.Tasks;
using Benzene.Azure.Core;
using Benzene.Azure.EventHub;
using Benzene.Azure.EventHub.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Xml;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure;

public class EventHubPipelineTest
{
    [Fact]
    public async Task Send()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseEventHub(eventHub => eventHub
                    .UseBenzeneMessage(direct => direct
                    .UseProcessResponse()
                    .UseMessageHandlers())))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsEventHubBenzeneMessage();

        await app.HandleEventHub(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task Send_Xml()
    {
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
                .UsingBenzene(x => x.AddXml())
            ).Configure(app => app
                .UseEventHub(eventHub => eventHub
                    .UseBenzeneMessage(direct => direct
                    .UseProcessResponse()
                    .UseMessageHandlers())))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload { Name = "some-name" })
            .WithHeader("content-type", "application/xml")
            .AsEventHubBenzeneMessage(new XmlSerializer());

        await app.HandleEventHub(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

}
