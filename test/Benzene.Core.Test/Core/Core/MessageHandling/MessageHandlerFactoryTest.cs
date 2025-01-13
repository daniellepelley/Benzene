using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Messages;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageHandlerFactoryTest
{
    [Fact]
    public async Task FindRoutes()
    {
        var mockRequestFactory = new Mock<IRequestMapperThunk>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var services = ServiceResolverMother.CreateServiceCollection();
        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var messageHandlerLookup = new MessageHandlerDefinitionLookUp(new[] { new ReflectionMessageHandlersFinder(typeof(ExampleRequestPayload).Assembly) }, new VersionSelector());
        var messageHandlerFactory = new MessageHandlerFactory(serviceResolver, new PipelineMessageHandlerWrapper(
            new HandlerPipelineBuilder(Array.Empty<IHandlerMiddlewareBuilder>()),
             serviceResolver), BenzeneLogger.NullLogger);

        var messageHandler = messageHandlerFactory.Create(messageHandlerLookup.FindHandler(new Topic(Defaults.Topic)));

        var result = await messageHandler.HandlerAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }
}
