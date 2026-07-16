using System.Threading.Tasks;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class PresetTopicMiddlewareTest
{
    private class TestContext
    {
    }

    [Fact]
    public async Task HandleAsync_SetsPresetTopicOnTheHolder_ThenCallsNext()
    {
        var holder = new PresetTopicHolder();
        var nextCalled = false;
        var middleware = new PresetTopicMiddleware<TestContext>(holder, new Topic("preset-topic"));

        await middleware.HandleAsync(new TestContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.Equal("preset-topic", holder.PresetTopic?.Id);
        Assert.True(nextCalled);
    }
}
