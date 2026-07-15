using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class PresetTopicMiddlewareTest
{
    private class TestContext : IHasPresetTopic
    {
        public ITopic? PresetTopic { get; set; }
    }

    [Fact]
    public async Task HandleAsync_SetsPresetTopicOnContext_ThenCallsNext()
    {
        var context = new TestContext();
        var nextCalled = false;
        var middleware = new PresetTopicMiddleware<TestContext>(new Topic("preset-topic"));

        await middleware.HandleAsync(context, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.Equal("preset-topic", context.PresetTopic?.Id);
        Assert.True(nextCalled);
    }
}
