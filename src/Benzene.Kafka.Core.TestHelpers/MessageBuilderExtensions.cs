using System.Text;
using Benzene.Abstractions;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Serialization;
using Confluent.Kafka;

namespace Benzene.Kafka.Core.TestHelpers;

/// <summary>
/// Test helpers that turn a <see cref="IMessageBuilder{T}"/> into a
/// <see cref="ConsumeResult{TKey, TValue}"/>, so a component test can push the demo message through a
/// <see cref="KafkaBenzeneTestHost{TKey, TValue}"/> exactly as the broker would deliver it. Kafka routes
/// on the literal record <c>Topic</c> (the builder's topic must be the Kafka topic name, not a
/// colon-separated id), and the message body is the raw serialized payload carried as the string value.
/// </summary>
public static class MessageBuilderExtensions
{
    /// <summary>
    /// Builds a keyless (<c>Ignore</c> key), string-valued <see cref="ConsumeResult{TKey, TValue}"/> from
    /// the message, using the default JSON serializer — matching the common
    /// <c>UseKafka&lt;Ignore, string&gt;</c> shape.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <returns>The Kafka record.</returns>
    public static ConsumeResult<Ignore, string> AsKafkaBenzeneMessage<T>(this IMessageBuilder<T> source)
    {
        return source.AsKafkaBenzeneMessage(new JsonSerializer());
    }

    /// <summary>
    /// Builds a keyless (<c>Ignore</c> key), string-valued <see cref="ConsumeResult{TKey, TValue}"/> from
    /// the message, using the supplied serializer for the value.
    /// </summary>
    /// <typeparam name="T">The message body type.</typeparam>
    /// <param name="source">The message builder.</param>
    /// <param name="serializer">The serializer used to render the record value.</param>
    /// <returns>The Kafka record.</returns>
    public static ConsumeResult<Ignore, string> AsKafkaBenzeneMessage<T>(this IMessageBuilder<T> source, ISerializer serializer)
    {
        var headers = new Headers();
        foreach (var header in source.Headers)
        {
            headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
        }

        return new ConsumeResult<Ignore, string>
        {
            Topic = source.Topic,
            Partition = new Partition(0),
            Offset = new Offset(0),
            Message = new Message<Ignore, string>
            {
                Value = serializer.Serialize(source.Message),
                Headers = headers
            }
        };
    }
}
