using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

/// <summary>
/// Coverage for the version-wiring change in <see cref="MessageRouter{TContext}"/>: it combines the
/// topic id from <see cref="IMessageTopicGetter{TContext}"/> (via <see cref="IMessageGetter{TContext}"/>)
/// with the version from the new <see cref="IMessageVersionGetter{TContext}"/> into one
/// <see cref="ITopic"/> before handler lookup (docs/specification/versioning.md §2.3), unless the
/// topic getter already supplied a non-empty version (e.g. an explicit preset).
/// </summary>
public class MessageRouterVersionWiringTest
{
    // Public (not private) because Moq needs to build a dynamic proxy for the closed generic
    // interfaces (IMessageGetter<TestContext>, etc.), which requires the type argument to be
    // accessible - see BenzeneMessageHttpMiddlewareTest for the same convention.
    public class TestContext
    {
    }

    private static (MessageRouter<TestContext> Router, Mock<IMessageHandlerDefinitionLookUp> LookUp) CreateRouter(
        ITopic topicFromGetter, string? version)
    {
        var messageGetter = new Mock<IMessageGetter<TestContext>>();
        messageGetter.Setup(x => x.GetTopic(It.IsAny<TestContext>())).Returns(topicFromGetter);

        var versionGetter = new Mock<IMessageVersionGetter<TestContext>>();
        versionGetter.Setup(x => x.GetVersion(It.IsAny<TestContext>())).Returns(version);

        var lookUp = new Mock<IMessageHandlerDefinitionLookUp>();
        lookUp.Setup(x => x.FindHandler(It.IsAny<ITopic>())).Returns((IMessageHandlerDefinition?)null);

        var defaultStatuses = new Mock<IDefaultStatuses>();
        defaultStatuses.SetupGet(x => x.NotFound).Returns("NotFound");
        defaultStatuses.SetupGet(x => x.ValidationError).Returns("ValidationError");

        var router = new MessageRouter<TestContext>(
            Mock.Of<IMessageHandlerFactory>(),
            messageGetter.Object,
            versionGetter.Object,
            lookUp.Object,
            Mock.Of<IRequestMapper<TestContext>>(),
            Mock.Of<IMessageHandlerResultSetter<TestContext>>(),
            defaultStatuses.Object,
            NullLogger<MessageRouter<TestContext>>.Instance);

        return (router, lookUp);
    }

    [Fact]
    public async Task HandleAsync_TopicHasNoVersion_UsesMessageVersionGetterResult()
    {
        var (router, lookUp) = CreateRouter(new Topic("order:create"), "V1");

        await router.HandleAsync(new TestContext(), () => Task.CompletedTask);

        lookUp.Verify(x => x.FindHandler(It.Is<ITopic>(t => t.Id == "order:create" && t.Version == "V1")), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TopicAlreadyHasAVersion_PresetVersionWinsOverMessageVersionGetter()
    {
        var (router, lookUp) = CreateRouter(new Topic("order:create", "preset-version"), "V1");

        await router.HandleAsync(new TestContext(), () => Task.CompletedTask);

        lookUp.Verify(x => x.FindHandler(It.Is<ITopic>(t => t.Id == "order:create" && t.Version == "preset-version")), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoVersionSignalled_TopicVersionStaysEmpty()
    {
        var (router, lookUp) = CreateRouter(new Topic("order:create"), version: null);

        await router.HandleAsync(new TestContext(), () => Task.CompletedTask);

        lookUp.Verify(x => x.FindHandler(It.Is<ITopic>(t => t.Id == "order:create" && t.Version == "")), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TopicIdMissing_NeverConsultsVersionGetter()
    {
        // A getter that can't resolve a topic at all returns null (not a Topic("<missing>")
        // placeholder, which - like an unmatched HTTP route - is a non-empty, non-null Id and
        // would still be looked up).
        var messageGetter = new Mock<IMessageGetter<TestContext>>();
        messageGetter.Setup(x => x.GetTopic(It.IsAny<TestContext>())).Returns((ITopic?)null);

        var versionGetter = new Mock<IMessageVersionGetter<TestContext>>();

        var defaultStatuses = new Mock<IDefaultStatuses>();
        defaultStatuses.SetupGet(x => x.ValidationError).Returns("ValidationError");

        var router = new MessageRouter<TestContext>(
            Mock.Of<IMessageHandlerFactory>(),
            messageGetter.Object,
            versionGetter.Object,
            Mock.Of<IMessageHandlerDefinitionLookUp>(),
            Mock.Of<IRequestMapper<TestContext>>(),
            Mock.Of<IMessageHandlerResultSetter<TestContext>>(),
            defaultStatuses.Object,
            NullLogger<MessageRouter<TestContext>>.Instance);

        await router.HandleAsync(new TestContext(), () => Task.CompletedTask);

        versionGetter.Verify(x => x.GetVersion(It.IsAny<TestContext>()), Times.Never);
    }
}
