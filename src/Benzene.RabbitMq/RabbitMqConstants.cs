namespace Benzene.RabbitMq;

/// <summary>
/// Shared constants for the RabbitMQ transport.
/// </summary>
public static class RabbitMqConstants
{
    /// <summary>
    /// The default AMQP message-property header key a Benzene RabbitMQ producer writes the topic to,
    /// and a consumer reads it from. It is a single default, not a hard-coded value: override it per
    /// side when integrating with a non-Benzene producer/consumer that carries the topic on a
    /// different header — on the consumer via <see cref="RabbitMqConfig.TopicHeaderKey"/> (or the
    /// <c>DependencyInjectionExtensions.AddRabbitMq(topicHeaderKey)</c> overload / the
    /// <see cref="RabbitMqMessage.RabbitMqMessageTopicGetter"/> constructor), and on the producer via
    /// the <c>topicHeaderKey</c> argument on the outbound <c>UseRabbitMq(...)</c> extensions,
    /// <see cref="RabbitMqSendMessage.RabbitMqBenzeneMessageClient"/>, or the
    /// <see cref="RabbitMqSendMessage.RabbitMqContextConverter{T}"/> constructor.
    /// </summary>
    public const string DefaultTopicHeader = "topic";
}
