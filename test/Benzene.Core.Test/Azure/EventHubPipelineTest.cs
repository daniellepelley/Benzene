using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventHub.Function;
using Benzene.Azure.Function.EventHub.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Testing;
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
                    .UseMessageHandlers())))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsEventHubBenzeneMessage();

        await app.HandleEventHub(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

    [Fact]
    public async Task Send_PropertyBasedRouting_RoutesByTopicPropertyAndRawBody()
    {
        // The property-based path (topic in an event property + raw serialized body), matching what the
        // OutboundContext Event Hub sender writes - so a Benzene->Benzene Event Hub hop round-trips over
        // Azure Functions without the sender having to wrap a Benzene message envelope.
        var mockExampleService = new Mock<IExampleService>();

        var app = new InlineAzureFunctionStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object)
            ).Configure(app => app
                .UseEventHub(eventHub => eventHub
                    .UseMessageHandlers()))
            .Build();

        var eventData = new EventData(new BinaryData(new JsonSerializer().Serialize(Defaults.MessageAsObject)));
        eventData.Properties["topic"] = Defaults.Topic;

        await app.HandleEventHub(eventData);
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
                    .UseMessageHandlers())))
            .Build();

        var request = MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload { Name = "some-name" })
            .WithHeader("content-type", "application/xml")
            .AsEventHubBenzeneMessage(new XmlSerializer());

        await app.HandleEventHub(request);
        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }

}
