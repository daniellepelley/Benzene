using System;
using Azure.Messaging.EventHubs;
using Benzene.Azure.EventHub;
using Benzene.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Xunit;

namespace Benzene.Test.Azure.EventHubWorker;

public class EventHubConsumerMapperTest
{
    private static EventHubConsumerContext CreateContext(string body = "some-message", params (string Key, object Value)[] properties)
    {
        var eventData = new EventData(new BinaryData(body));
        foreach (var (key, value) in properties)
        {
            eventData.Properties[key] = value;
        }
        return EventHubConsumerContext.CreateInstance(eventData);
    }

    [Fact]
    public void GetTopic_ReturnsTopicProperty()
    {
        var context = CreateContext(properties: ("topic", "some-topic"));

        var topic = new EventHubConsumerMessageTopicGetter().GetTopic(context);

        Assert.Equal("some-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoTopicProperty_ReturnsMissing()
    {
        var context = CreateContext();

        var topic = new EventHubConsumerMessageTopicGetter().GetTopic(context);

        Assert.Equal(Benzene.Core.Constants.Missing, topic.Id);
    }

    [Fact]
    public void PresetTopicMessageTopicGetter_PresetSet_OverridesMissingTopicProperty()
    {
        var context = CreateContext();
        var holder = new PresetTopicHolder { PresetTopic = new Topic("preset-topic") };

        var getter = new PresetTopicMessageTopicGetter<EventHubConsumerContext>(new EventHubConsumerMessageTopicGetter(), holder);

        var topic = getter.GetTopic(context);

        Assert.Equal("preset-topic", topic.Id);
    }

    [Fact]
    public void GetHeaders_ReturnsOnlyStringTypedProperties()
    {
        var context = CreateContext(properties: new[] { ("some-header", (object)"some-value"), ("some-number", (object)42) });

        var headers = new EventHubConsumerMessageHeadersGetter().GetHeaders(context);

        Assert.Single(headers);
        Assert.Equal("some-value", headers["some-header"]);
    }

    [Fact]
    public void GetBody_ReturnsBodyAsString()
    {
        var context = CreateContext("{\"name\":\"some-name\"}");

        var body = new EventHubConsumerMessageBodyGetter().GetBody(context);

        Assert.Equal("{\"name\":\"some-name\"}", body);
    }
}
