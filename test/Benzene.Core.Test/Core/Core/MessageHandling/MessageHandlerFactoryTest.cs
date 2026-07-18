using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageHandlerFactoryTest
{
    [Fact]
    public async Task FindRoutes()
    {
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload
            {
                Name = Defaults.Name
            });

        var services = ServiceResolverMother.CreateServiceCollection();
        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var messageHandlerIndex = new MessageHandlerDefinitionIndex(new[] { new ReflectionMessageHandlersFinder(typeof(ExampleRequestPayload).Assembly) });
        var messageHandlerLookup = new MessageHandlerDefinitionLookUp(messageHandlerIndex, new VersionSelector());
        var messageHandlerFactory = new MessageHandlerFactory(serviceResolver, new PipelineMessageHandlerWrapper(
            new HandlerPipelineBuilder(Array.Empty<IHandlerMiddlewareBuilder>()),
             serviceResolver), NullLoggerFactory.Instance, new DefaultStatuses());

        var messageHandler = messageHandlerFactory.Create(messageHandlerLookup.FindHandler(new Topic(Defaults.Topic)));

        var result = await messageHandler.HandleAsync(mockRequestFactory.Object);
        Assert.Equal(BenzeneResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Create_DistinctDefinitions_EachDispatchToItsOwnHandlerType()
    {
        // Proves the per-(handler,request,response)-triple compiled dispatcher cache differentiates
        // correctly: v1 and v2 share a topic id and request type but differ in handler type and
        // response type, so a cache keyed incorrectly would cross-wire them.
        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload { Name = Defaults.Name });

        var services = ServiceResolverMother.CreateServiceCollection();
        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var messageHandlerIndex = new MessageHandlerDefinitionIndex(new[] { new ReflectionMessageHandlersFinder(typeof(ExampleRequestPayload).Assembly) });
        var messageHandlerLookup = new MessageHandlerDefinitionLookUp(messageHandlerIndex, new VersionSelector());
        var messageHandlerFactory = new MessageHandlerFactory(serviceResolver, new PipelineMessageHandlerWrapper(
            new HandlerPipelineBuilder(Array.Empty<IHandlerMiddlewareBuilder>()),
             serviceResolver), NullLoggerFactory.Instance, new DefaultStatuses());

        var v1Handler = messageHandlerFactory.Create(messageHandlerLookup.FindHandler(new Topic(Defaults.Topic, "")));
        var v2Handler = messageHandlerFactory.Create(messageHandlerLookup.FindHandler(new Topic(Defaults.Topic, "2.0")));

        var v1Result = await v1Handler.HandleAsync(mockRequestFactory.Object);
        var v2Result = await v2Handler.HandleAsync(mockRequestFactory.Object);

        Assert.Equal(BenzeneResultStatus.Ok, v1Result.Status);
        Assert.Equal(BenzeneResultStatus.Deleted, v2Result.Status);
    }

    [Fact]
    public async Task Create_RepeatedCallsForSameDefinition_ProduceIndependentlyResolvedHandlers()
    {
        // Proves the cached dispatcher delegate re-resolves the handler instance (and its
        // dependencies) from the factory's own resolver on every call, rather than reusing a stale
        // closed-over instance from the first build.
        var services = ServiceResolverMother.CreateServiceCollection();
        var serviceResolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var messageHandlerIndex = new MessageHandlerDefinitionIndex(new[] { new ReflectionMessageHandlersFinder(typeof(ExampleRequestPayload).Assembly) });
        var messageHandlerLookup = new MessageHandlerDefinitionLookUp(messageHandlerIndex, new VersionSelector());
        var messageHandlerFactory = new MessageHandlerFactory(serviceResolver, new PipelineMessageHandlerWrapper(
            new HandlerPipelineBuilder(Array.Empty<IHandlerMiddlewareBuilder>()),
             serviceResolver), NullLoggerFactory.Instance, new DefaultStatuses());

        var definition = messageHandlerLookup.FindHandler(new Topic(Defaults.Topic));

        var first = messageHandlerFactory.Create(definition);
        var second = messageHandlerFactory.Create(definition);

        Assert.NotSame(first, second);

        var mockRequestFactory = new Mock<IDeferredRequestMapper>();
        mockRequestFactory.Setup(x => x.GetRequest<ExampleRequestPayload>())
            .Returns(new ExampleRequestPayload { Name = Defaults.Name });

        Assert.Equal(BenzeneResultStatus.Ok, (await first.HandleAsync(mockRequestFactory.Object)).Status);
        Assert.Equal(BenzeneResultStatus.Ok, (await second.HandleAsync(mockRequestFactory.Object)).Status);
    }
}
