using System;
using Azure.Messaging.EventHubs;
using Benzene.Azure.Function.EventHub.Function;
using Xunit;

namespace Benzene.Test.Azure;

public class EventHubGettersTest
{
    private static EventHubContext CreateContext(string body = "some-message", params (string Key, object Value)[] properties)
    {
        var eventData = new EventData(new BinaryData(body));
        foreach (var (key, value) in properties)
        {
            eventData.Properties[key] = value;
        }
        return EventHubContext.CreateInstance(eventData);
    }

    [Fact]
    public void GetHeaders_ReturnsOnlyStringTypedProperties()
    {
        var context = CreateContext(properties: new[] { ("some-header", (object)"some-value"), ("some-number", (object)42) });

        var headers = new EventHubMessageHeadersGetter().GetHeaders(context);

        Assert.Single(headers);
        Assert.Equal("some-value", headers["some-header"]);
    }

    [Fact]
    public void GetHeaders_TraceparentProperty_RoundTrips()
    {
        const string traceparent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        var context = CreateContext(properties: ("traceparent", (object)traceparent));

        var headers = new EventHubMessageHeadersGetter().GetHeaders(context);

        Assert.Equal(traceparent, headers["traceparent"]);
    }

    [Fact]
    public void GetHeaders_NoProperties_ReturnsEmptyDictionary()
    {
        var context = CreateContext();

        var headers = new EventHubMessageHeadersGetter().GetHeaders(context);

        Assert.Empty(headers);
    }
}
