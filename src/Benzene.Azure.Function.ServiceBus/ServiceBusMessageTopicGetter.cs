using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Extracts the message topic from a Service Bus message's <c>"topic"</c> application property.
/// </summary>
public class ServiceBusMessageTopicGetter : IMessageTopicGetter<ServiceBusContext>
{
    /// <summary>
    /// Gets the topic from the Service Bus message's <c>"topic"</c> application property.
    /// </summary>
    /// <param name="context">The Service Bus context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(ServiceBusContext context)
    {
        return new Topic(GetTopicProperty(context));
    }

    private static string GetTopicProperty(ServiceBusContext context)
    {
        return context.Message.ApplicationProperties.TryGetValue("topic", out var value) ? value as string : null;
    }
}
