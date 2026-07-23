using Azure.Messaging.ServiceBus;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.ServiceBus.TestHelpers;

/// <summary>
/// Test helpers that turn a <see cref="IMessageBuilder{T}"/> into a <see cref="ServiceBusReceivedMessage"/>,
/// so a component test can push the demo message through a <see cref="ServiceBusWorkerBenzeneTestHost"/>
/// exactly as the broker would deliver it. The topic rides as the <c>"topic"</c> application property
/// and the message body is the raw serialized payload.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds a <see cref="ServiceBusReceivedMessage"/> from the message, using the default JSON serializer.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <returns>The Service Bus message.</returns>
    public static ServiceBusReceivedMessage AsAzureServiceBusMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsAzureServiceBusMessage(new JsonSerializer());
    }

    /// <summary>
    /// Builds a <see cref="ServiceBusReceivedMessage"/> from the message, using the supplied serializer
    /// for the body.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <param name="serializer">The serializer used to render the message body.</param>
    /// <returns>The Service Bus message.</returns>
    public static ServiceBusReceivedMessage AsAzureServiceBusMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        var properties = new Dictionary<string, object> { { "topic", source.Topic } };
        foreach (var header in source.Headers)
        {
            properties[header.Key] = header.Value;
        }

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(serializer.Serialize(source.Message)),
            properties: properties);
    }
}
