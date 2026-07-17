using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Extracts the message topic from a Service Bus message's <c>"topic"</c> application property.
/// </summary>
public class ServiceBusConsumerMessageTopicGetter : IMessageTopicGetter<ServiceBusConsumerContext>
{
    /// <summary>
    /// Gets the topic from the Service Bus message's <c>"topic"</c> application property.
    /// </summary>
    /// <param name="context">The Service Bus consumer context to extract the topic from.</param>
    /// <returns>
    /// The topic, or a topic with <see cref="Benzene.Core.Constants.Missing"/> as its ID if the
    /// <c>"topic"</c> property isn't present.
    /// </returns>
    public ITopic GetTopic(ServiceBusConsumerContext context)
    {
        // Topic(null) resolves to Constants.Missing, same as the other consumer packages.
        return new Topic(GetTopicProperty(context)!);
    }

    private static string? GetTopicProperty(ServiceBusConsumerContext context)
    {
        return context.Message.ApplicationProperties.TryGetValue("topic", out var value) ? value as string : null;
    }
}
