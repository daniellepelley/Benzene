using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Azure.EventHub;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.EventHubWorker;

// Deterministic (no emulator) end-to-end test of the *real* DI pipeline: builds
// AddBenzeneMessage().AddEventHubConsumer() + .UseMessageHandlers() exactly as Extensions.UseEventHub
// does, then runs a real event through EventHubConsumerApplication and asserts the handler ran. The
// mocked-pipeline unit tests can't catch a missing DI registration (e.g. IMessageVersionGetter /
// IRequestMapper / media-format negotiation) because they never build the real message-handler
// routing - this test exists precisely to cover that gap, which the live emulator test caught first.
public class EventHubConsumerRealPipelineTest
{
    [Fact]
    public async Task HandleAsync_RealDependencyInjectionPipeline_RoutesToMessageHandler()
    {
        var mockExampleService = new Mock<IExampleService>();

        var services = new ServiceCollection();
        services.ConfigureServiceCollection().AddSingleton(mockExampleService.Object);

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMessage().AddEventHubConsumer();

        var pipelineBuilder = new MiddlewarePipelineBuilder<EventHubConsumerContext>(container);
        pipelineBuilder.UseMessageHandlers();

        var application = new EventHubConsumerApplication(pipelineBuilder.Build());
        var resolverFactory = new MicrosoftServiceResolverFactory(services);

        // First-class mappers read the "topic" property and the serialized body directly (this
        // package deliberately doesn't use the BenzeneMessage envelope that AsEventHubBenzeneMessage
        // builds), so hand-build the EventData to match how UseEventHub's mappers read it.
        var eventData = new EventData(new BinaryData(new JsonSerializer().Serialize(Defaults.MessageAsObject)));
        eventData.Properties["topic"] = Defaults.Topic;

        await application.HandleAsync(eventData, resolverFactory);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }
}
