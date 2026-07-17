using System.Threading.Tasks;
using Benzene.Azure.Function.ServiceBus.TestHelpers;
using Benzene.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Azure.ServiceBusWorker;

// Deterministic (no emulator) end-to-end test of the *real* DI pipeline: builds
// AddBenzeneMessage().AddServiceBusConsumer() + .UseMessageHandlers() exactly as Extensions.UseServiceBus
// does, then runs a real message through ServiceBusConsumerApplication and asserts the handler ran.
// The mocked-pipeline unit tests can't catch a missing DI registration (e.g. IMessageVersionGetter /
// IRequestMapper / media-format negotiation) because they never build the real message-handler
// routing - this test exists precisely to cover that gap, which the live emulator test caught first.
public class ServiceBusConsumerRealPipelineTest
{
    [Fact]
    public async Task HandleAsync_RealDependencyInjectionPipeline_RoutesToMessageHandler()
    {
        var mockExampleService = new Mock<IExampleService>();

        var services = new ServiceCollection();
        services.ConfigureServiceCollection().AddSingleton(mockExampleService.Object);

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMessage().AddServiceBusConsumer();

        var pipelineBuilder = new MiddlewarePipelineBuilder<ServiceBusConsumerContext>(container);
        pipelineBuilder.UseMessageHandlers();

        var application = new ServiceBusConsumerApplication(pipelineBuilder.Build());
        var resolverFactory = new MicrosoftServiceResolverFactory(services);

        var message = MessageBuilder.Create(Defaults.Topic, Defaults.MessageAsObject).AsAzureServiceBusMessage();

        await application.HandleAsync(message, resolverFactory);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }
}
