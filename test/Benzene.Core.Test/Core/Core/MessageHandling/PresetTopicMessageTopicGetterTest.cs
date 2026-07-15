using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class PresetTopicMessageTopicGetterTest
{
    private class TestContext : IHasPresetTopic
    {
        public ITopic? PresetTopic { get; set; }
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
        var context = new TestContext { PresetTopic = new Topic("preset-topic") };
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(new Topic("sender-topic")));

        var topic = getter.GetTopic(context);

        Assert.Equal("preset-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoPresetTopic_FallsBackToInner()
    {
        var context = new TestContext();
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(new Topic("sender-topic")));

        var topic = getter.GetTopic(context);

        Assert.Equal("sender-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoPresetTopicAndInnerHasNone_ReturnsInnerResultUnchanged()
    {
        var context = new TestContext();
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(null));

        var topic = getter.GetTopic(context);

        Assert.Null(topic);
    }
}
