using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Core;
using Xunit;

namespace Benzene.Test.Azure.ServiceBus;

public class ServiceBusMessageTopicGetterTest
{
    [Fact]
    public void GetTopic_ReturnsTopicApplicationProperty()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData("some-message"),
            properties: new Dictionary<string, object> { { "topic", "some-topic" } });
        var context = new ServiceBusContext(message);

        var topic = new ServiceBusMessageTopicGetter().GetTopic(context);

        Assert.Equal("some-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_NoTopicProperty_ReturnsMissing()
    {
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData("some-message"),
            properties: new Dictionary<string, object>());
        var context = new ServiceBusContext(message);

        var topic = new ServiceBusMessageTopicGetter().GetTopic(context);

        Assert.Equal(Constants.Missing, topic.Id);
    }
}
