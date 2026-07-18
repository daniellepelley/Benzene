using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.RabbitMq;
using Benzene.RabbitMq.RabbitMqMessage;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Benzene.Test.RabbitMq;

// Deterministic (no broker) end-to-end test of the *real* DI pipeline: builds
// AddBenzeneMessage().AddRabbitMq() + .UseMessageHandlers() exactly as Extensions.UseRabbitMq does,
// then runs a real delivery through RabbitMqApplication and asserts the handler ran. The
// mocked-pipeline unit tests can't catch a missing DI registration (IMessageVersionGetter /
// IRequestMapper / media-format negotiation) because they never build the real message-handler
// routing - and the worker nacks handler faults, so a gap would otherwise surface only as "messages
// never handled". This test covers that gap without a live broker.
public class RabbitMqRealPipelineTest
{
    [Fact]
    public async Task HandleAsync_RealDependencyInjectionPipeline_RoutesToMessageHandler()
    {
        var mockExampleService = new Mock<IExampleService>();

        var services = new ServiceCollection();
        services.ConfigureServiceCollection().AddSingleton(mockExampleService.Object);

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMessage().AddRabbitMq();

        var pipelineBuilder = new MiddlewarePipelineBuilder<RabbitMqContext>(container);
        pipelineBuilder.UseMessageHandlers();

        var application = new RabbitMqApplication(pipelineBuilder.Build());
        var resolverFactory = new MicrosoftServiceResolverFactory(services);

        var body = Encoding.UTF8.GetBytes(new JsonSerializer().Serialize(Defaults.MessageAsObject));
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { ["topic"] = Encoding.UTF8.GetBytes(Defaults.Topic) },
        };
        var delivery = new BasicDeliverEventArgs("tag", 1, false, "exchange", Defaults.Topic, properties, body);

        await application.HandleAsync(delivery, resolverFactory);

        mockExampleService.Verify(x => x.Register(Defaults.Name));
    }
}
