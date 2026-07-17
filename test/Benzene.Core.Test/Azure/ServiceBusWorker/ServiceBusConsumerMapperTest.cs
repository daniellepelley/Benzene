using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using Benzene.Azure.ServiceBus;
using Benzene.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Xunit;

namespace Benzene.Test.Azure.ServiceBusWorker;

public class ServiceBusConsumerMapperTest
{
    private static ServiceBusConsumerContext CreateContext(IDictionary<string, object> properties, string body = "some-message")
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(body),
            properties: properties);
        return ServiceBusConsumerContext.CreateInstance(message);
    }

    [Fact]
    public void GetTopic_ReturnsTopicApplicationProperty()
    {
        var context = CreateContext(new Dictionary<string, object> { { "topic", "some-topic" } });

        var topic = new ServiceBusConsumerMessageTopicGetter().GetTopic(context);

        Assert.Equal("some-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoTopicProperty_ReturnsMissing()
    {
        var context = CreateContext(new Dictionary<string, object>());

        var topic = new ServiceBusConsumerMessageTopicGetter().GetTopic(context);

        Assert.Equal(Benzene.Core.Constants.Missing, topic.Id);
    }

    [Fact]
    public void PresetTopicMessageTopicGetter_PresetSet_OverridesMissingTopicProperty()
    {
        var context = CreateContext(new Dictionary<string, object>());
        var holder = new PresetTopicHolder { PresetTopic = new Topic("preset-topic") };

        var getter = new PresetTopicMessageTopicGetter<ServiceBusConsumerContext>(new ServiceBusConsumerMessageTopicGetter(), holder);

        var topic = getter.GetTopic(context);

        Assert.Equal("preset-topic", topic.Id);
    }

    [Fact]
    public void GetHeaders_ReturnsOnlyStringTypedApplicationProperties()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            { "some-header", "some-value" },
            { "some-number", 42 }
        });

        var headers = new ServiceBusConsumerMessageHeadersGetter().GetHeaders(context);

        Assert.Single(headers);
        Assert.Equal("some-value", headers["some-header"]);
    }

    [Fact]
    public void GetBody_ReturnsBodyAsString()
    {
        var context = CreateContext(new Dictionary<string, object>(), "{\"name\":\"some-name\"}");

        var body = new ServiceBusConsumerMessageBodyGetter().GetBody(context);

        Assert.Equal("{\"name\":\"some-name\"}", body);
    }
}
