using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageHandlerDefinitionIndexTest
{
    [Fact]
    public void GetByTopicId_ReturnsDefinitionsFromEveryFinder()
    {
        var list = new MessageHandlersList();
        list.Add(MessageHandlerDefinition.CreateInstance("topic-a", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)));

        var index = new MessageHandlerDefinitionIndex(new[] { list }, list);

        var definitions = index.GetByTopicId("topic-a");

        Assert.Single(definitions);
        Assert.Equal("topic-a", definitions[0].Topic.Id);
    }

    [Fact]
    public void GetByTopicId_UnknownTopic_ReturnsEmptyArray()
    {
        var list = new MessageHandlersList();
        var index = new MessageHandlerDefinitionIndex(new[] { list }, list);

        Assert.Empty(index.GetByTopicId("no-such-topic"));
    }

    [Fact]
    public void GetByTopicId_DeduplicatesByIdAndVersion()
    {
        var list = new MessageHandlersList();
        list.Add(MessageHandlerDefinition.CreateInstance("topic-a", "1.0", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)));
        list.Add(MessageHandlerDefinition.CreateInstance("topic-a", "1.0", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandlerV2)));

        var index = new MessageHandlerDefinitionIndex(new[] { list }, list);

        var definitions = index.GetByTopicId("topic-a");

        Assert.Single(definitions);
        Assert.Equal(typeof(ExampleMessageHandler), definitions[0].HandlerType);
    }

    [Fact]
    public void GetByTopicId_RuntimeAddition_IsPickedUpOnNextCall()
    {
        var list = new MessageHandlersList();
        var index = new MessageHandlerDefinitionIndex(new[] { list }, list);

        Assert.Empty(index.GetByTopicId("topic-b"));

        list.Add(MessageHandlerDefinition.CreateInstance("topic-b", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)));

        var definitions = index.GetByTopicId("topic-b");
        Assert.Single(definitions);
    }

    [Fact]
    public void GetByTopicId_WithoutMessageHandlersList_BuildsOnceAndNeverInvalidates()
    {
        var list = new MessageHandlersList();
        // No MessageHandlersList passed for version tracking - simulates direct construction (e.g. in
        // a test) where runtime mutation isn't a concern.
        var index = new MessageHandlerDefinitionIndex(new[] { list });

        Assert.Empty(index.GetByTopicId("topic-c"));

        list.Add(MessageHandlerDefinition.CreateInstance("topic-c", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)));

        // Still empty: the index was built once (against the empty list) and has no version signal to
        // detect the later addition.
        Assert.Empty(index.GetByTopicId("topic-c"));
    }

    [Fact]
    public void GetAll_ReturnsEveryDeduplicatedDefinition()
    {
        var list = new MessageHandlersList();
        list.Add(MessageHandlerDefinition.CreateInstance("topic-a", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)));
        list.Add(MessageHandlerDefinition.CreateInstance("topic-b", typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler)));

        var index = new MessageHandlerDefinitionIndex(new[] { list }, list);

        Assert.Equal(2, index.GetAll().Length);
    }
}
