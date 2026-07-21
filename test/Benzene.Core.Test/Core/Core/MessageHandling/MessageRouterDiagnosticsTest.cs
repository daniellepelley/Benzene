using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Test.Examples;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

/// <summary>
/// Root-cause-diagnostics coverage for <see cref="MessageRouter{TContext}"/>: the two failure paths a
/// developer most needs an actionable message from - a message that carries no resolvable topic, and a
/// discovered handler whose resolved instance doesn't implement the handler interface for its declared
/// request/response types (a wiring bug the router used to mislabel as "no handler found").
/// </summary>
public class MessageRouterDiagnosticsTest
{
    public class TestContext
    {
    }

    private sealed class MisdeclaredHandler
    {
    }

    private static (MessageRouter<TestContext> Router, Func<IMessageHandlerResult?> Captured) CreateRouter(
        ITopic? topicFromGetter, IMessageHandlerDefinition? definition)
    {
        var messageGetter = new Mock<IMessageGetter<TestContext>>();
        messageGetter.Setup(x => x.GetTopic(It.IsAny<TestContext>())).Returns(topicFromGetter);

        var versionGetter = new Mock<IMessageVersionGetter<TestContext>>();
        versionGetter.Setup(x => x.GetVersion(It.IsAny<TestContext>())).Returns((string?)null);

        var lookUp = new Mock<IMessageHandlerDefinitionLookUp>();
        lookUp.Setup(x => x.FindHandler(It.IsAny<ITopic>())).Returns(definition);

        // Default IMessageHandlerFactory mock returns null from Create(...) - exactly the "definition
        // found but instance implements neither handler interface" case B1 targets.
        var defaultStatuses = new Mock<IDefaultStatuses>();
        defaultStatuses.SetupGet(x => x.NotFound).Returns("NotFound");
        defaultStatuses.SetupGet(x => x.ValidationError).Returns("ValidationError");

        IMessageHandlerResult? captured = null;
        var resultSetter = new Mock<IMessageHandlerResultSetter<TestContext>>();
        resultSetter.Setup(x => x.SetResultAsync(It.IsAny<TestContext>(), It.IsAny<IMessageHandlerResult>()))
            .Callback<TestContext, IMessageHandlerResult>((_, r) => captured = r)
            .Returns(Task.CompletedTask);

        var router = new MessageRouter<TestContext>(
            Mock.Of<IMessageHandlerFactory>(),
            messageGetter.Object,
            versionGetter.Object,
            lookUp.Object,
            Mock.Of<IRequestMapper<TestContext>>(),
            resultSetter.Object,
            defaultStatuses.Object,
            NullLogger<MessageRouter<TestContext>>.Instance);

        return (router, () => captured);
    }

    [Fact]
    public async Task HandleAsync_TopicMissing_ResultNamesTheRemedy()
    {
        var (router, captured) = CreateRouter(topicFromGetter: null, definition: null);

        await router.HandleAsync(new TestContext(), () => Task.CompletedTask);

        var result = captured();
        Assert.NotNull(result);
        Assert.Equal("ValidationError", result!.BenzeneResult.Status);
        // The message must point the developer at how to supply a topic, not just say it's missing.
        Assert.Contains("UsePresetTopic", string.Join(" ", result.BenzeneResult.Errors));
    }

    [Fact]
    public async Task HandleAsync_HandlerResolvedButInterfaceMismatch_ReportsWiringBugNotNotFound()
    {
        var definition = new Mock<IMessageHandlerDefinition>();
        definition.SetupGet(x => x.Topic).Returns(new Topic("order:create"));
        definition.SetupGet(x => x.HandlerType).Returns(typeof(MisdeclaredHandler));
        definition.SetupGet(x => x.RequestType).Returns(typeof(ExampleRequestPayload));
        definition.SetupGet(x => x.ResponseType).Returns(typeof(ExampleResponsePayload));

        var (router, captured) = CreateRouter(new Topic("order:create"), definition.Object);

        await router.HandleAsync(new TestContext(), () => Task.CompletedTask);

        var result = captured();
        Assert.NotNull(result);
        // Not a routing miss (NotFound) - a wiring/signature bug, reported as UnexpectedError and
        // naming the handler type + expected interface so the developer fixes the right thing.
        Assert.Equal("UnexpectedError", result!.BenzeneResult.Status);
        var message = string.Join(" ", result.BenzeneResult.Errors);
        Assert.Contains(nameof(MisdeclaredHandler), message);
        Assert.Contains("does not implement", message);
    }
}
