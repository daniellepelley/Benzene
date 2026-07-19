using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Extracts the message topic from a Service Bus message's topic application property.
/// </summary>
public class ServiceBusMessageTopicGetter : IMessageTopicGetter<ServiceBusContext>
{
    /// <summary>
    /// The default application-property key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="ServiceBusMessageTopicGetter(string)"/>
    /// (or via <c>DependencyInjectionExtensions.AddAzureServiceBus(topicPropertyKey)</c> /
    /// <c>UseServiceBus(..., topicPropertyKey)</c>) to consume messages a non-Benzene producer routes
    /// on another application property.
    /// </summary>
    public const string DefaultTopicProperty = "topic";

    private readonly string _topicPropertyKey;

    /// <summary>
    /// Initializes a new instance that reads the topic from the given application-property key.
    /// </summary>
    /// <param name="topicPropertyKey">
    /// The application property the topic is carried on. Defaults to
    /// <see cref="DefaultTopicProperty"/> (<c>"topic"</c>).
    /// </param>
    public ServiceBusMessageTopicGetter(string topicPropertyKey = DefaultTopicProperty)
    {
        _topicPropertyKey = topicPropertyKey;
    }

    /// <summary>
    /// Gets the topic from the Service Bus message's topic application property.
    /// </summary>
    /// <param name="context">The Service Bus context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(ServiceBusContext context)
    {
        return new Topic(GetTopicProperty(context));
    }

    private string GetTopicProperty(ServiceBusContext context)
    {
        return context.Message.ApplicationProperties.TryGetValue(_topicPropertyKey, out var value) ? value as string : null;
    }
}
