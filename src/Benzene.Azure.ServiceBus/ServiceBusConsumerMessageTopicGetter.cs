using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Extracts the message topic from a Service Bus message's topic application property.
/// </summary>
public class ServiceBusConsumerMessageTopicGetter : IMessageTopicGetter<ServiceBusConsumerContext>
{
    /// <summary>
    /// The default application-property key the topic is read from. It is a single default, not a
    /// hard-coded value — pass a different key to <see cref="ServiceBusConsumerMessageTopicGetter(string)"/>
    /// (or via <see cref="BenzeneServiceBusConfig.TopicPropertyKey"/> /
    /// <c>DependencyInjectionExtensions.AddServiceBusConsumer(topicPropertyKey)</c>) to consume messages
    /// a non-Benzene producer routes on another application property.
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
    public ServiceBusConsumerMessageTopicGetter(string topicPropertyKey = DefaultTopicProperty)
    {
        _topicPropertyKey = topicPropertyKey;
    }

    /// <summary>
    /// Gets the topic from the Service Bus message's topic application property.
    /// </summary>
    /// <param name="context">The Service Bus consumer context to extract the topic from.</param>
    /// <returns>
    /// The topic, or a topic with <see cref="Benzene.Core.Constants.Missing"/> as its ID if the
    /// topic property isn't present.
    /// </returns>
    public ITopic GetTopic(ServiceBusConsumerContext context)
    {
        // Topic(null) resolves to Constants.Missing, same as the other consumer packages.
        return new Topic(GetTopicProperty(context)!);
    }

    private string? GetTopicProperty(ServiceBusConsumerContext context)
    {
        return context.Message.ApplicationProperties.TryGetValue(_topicPropertyKey, out var value) ? value as string : null;
    }
}
