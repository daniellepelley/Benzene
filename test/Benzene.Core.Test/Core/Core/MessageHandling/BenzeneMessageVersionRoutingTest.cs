using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

/// <summary>
/// The BenzeneMessage transport must resolve the payload schema version through the configurable,
/// priority-ordered <see cref="IMessageVersionGetter{TContext}"/> (default order
/// <c>benzene-version</c> &gt; <c>version</c> &gt; <c>x-version</c>) - not by baking the raw
/// <c>"version"</c> header into the topic in <c>BenzeneMessageGetter.GetTopic</c>. A topic-getter
/// version is treated by <see cref="MessageRouter{TContext}"/> as a deliberate preset override that
/// skips the version getter, so hardcoding one there silently defeats the configured header order.
/// </summary>
public class BenzeneMessageVersionRoutingTest
{
    private static ITopic RouteVersionFor(IDictionary<string, string> headers)
    {
        var request = new BenzeneMessageRequest { Topic = "order:create", Headers = headers, Body = "{}" };
        var context = new BenzeneMessageContext(request);

        var getter = new BenzeneMessageGetter();
        var versionGetter = new HeaderMessageVersionGetter<BenzeneMessageContext>(getter);

        var lookUp = new Mock<IMessageHandlerDefinitionLookUp>();
        ITopic? routed = null;
        lookUp.Setup(x => x.FindHandler(It.IsAny<ITopic>()))
            .Callback<ITopic>(t => routed = t)
            .Returns((IMessageHandlerDefinition?)null);

        var defaultStatuses = new Mock<IDefaultStatuses>();
        defaultStatuses.SetupGet(x => x.NotFound).Returns("not-found");
        defaultStatuses.SetupGet(x => x.ValidationError).Returns("validation-error");

        var router = new MessageRouter<BenzeneMessageContext>(
            Mock.Of<IMessageHandlerFactory>(),
            getter,
            versionGetter,
            lookUp.Object,
            Mock.Of<IRequestMapper<BenzeneMessageContext>>(),
            Mock.Of<IMessageHandlerResultSetter<BenzeneMessageContext>>(),
            defaultStatuses.Object,
            NullLogger<MessageRouter<BenzeneMessageContext>>.Instance);

        router.HandleAsync(context, () => Task.CompletedTask).GetAwaiter().GetResult();
        return routed!;
    }

    [Fact]
    public void BenzeneVersionHeaderWinsOverVersionHeader()
    {
        // Both present: the configured order puts benzene-version first, so it must win. Before the
        // fix, GetTopic read the raw "version" header as a preset override and routed to "1".
        var routed = RouteVersionFor(new Dictionary<string, string>
        {
            { "benzene-version", "2" },
            { "version", "1" }
        });

        Assert.Equal("order:create", routed.Id);
        Assert.Equal("2", routed.Version);
    }

    [Fact]
    public void VersionHeaderAloneStillResolves()
    {
        // The common single-header case is unchanged: "version" alone still routes to that version
        // (now via the version getter rather than the hardcoded topic version).
        var routed = RouteVersionFor(new Dictionary<string, string> { { "version", "3" } });

        Assert.Equal("3", routed.Version);
    }

    [Fact]
    public void NoVersionHeader_LeavesVersionEmpty()
    {
        var routed = RouteVersionFor(new Dictionary<string, string>());

        Assert.Equal("order:create", routed.Id);
        Assert.Equal("", routed.Version);
    }
}
