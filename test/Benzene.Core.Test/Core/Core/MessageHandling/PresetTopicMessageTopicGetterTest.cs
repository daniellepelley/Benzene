using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class PresetTopicMessageTopicGetterTest
{
    private class TestContext
    {
    }

    private class FixedMessageTopicGetter : IMessageTopicGetter<TestContext>
    {
        private readonly ITopic? _topic;

        public FixedMessageTopicGetter(ITopic? topic)
        {
            _topic = topic;
        }

        public ITopic? GetTopic(TestContext context) => _topic;
    }

    [Fact]
    public void GetTopic_PresetTopicSet_ReturnsPresetTopic_EvenWhenInnerHasARealTopic()
    {
        var holder = new PresetTopicHolder { PresetTopic = new Topic("preset-topic") };
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(new Topic("sender-topic")), holder);

        var topic = getter.GetTopic(new TestContext());

        Assert.Equal("preset-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoPresetTopic_FallsBackToInner()
    {
        var holder = new PresetTopicHolder();
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(new Topic("sender-topic")), holder);

        var topic = getter.GetTopic(new TestContext());

        Assert.Equal("sender-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoPresetTopicAndInnerHasNone_ReturnsInnerResultUnchanged()
    {
        var holder = new PresetTopicHolder();
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(null), holder);

        var topic = getter.GetTopic(new TestContext());

        Assert.Null(topic);
    }
}
