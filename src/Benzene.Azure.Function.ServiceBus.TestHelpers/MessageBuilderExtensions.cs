using Azure.Messaging.ServiceBus;
using Benzene.Abstractions;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.Function.ServiceBus.TestHelpers;

public static class MessageBuilderExtensions
{
    public static ServiceBusReceivedMessage AsAzureServiceBusMessage<T>(this IMessageBuilder<T> source)
    {
        var properties = new Dictionary<string, object> { { "topic", source.Topic } };
        foreach (var header in source.Headers)
        {
            properties[header.Key] = header.Value;
        }

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(new JsonSerializer().Serialize(source.Message)),
            properties: properties);
    }
}
