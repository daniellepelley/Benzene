using Azure.Messaging.EventHubs;
using Benzene.Abstractions;
using Benzene.Core.BenzeneMessage.TestHelpers;

namespace Benzene.Azure.EventHub.TestHelpers;

public static class MessageBuilderExtensions
{
    public static EventData AsEventHubBenzeneMessage(this IMessageBuilder source)
    {
        return new EventData
        {
            EventBody = new BinaryData(source.AsBenzeneMessage())
        };
    }
}
