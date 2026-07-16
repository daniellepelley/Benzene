using System;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Thrown by <see cref="KafkaApplication"/> when <see cref="KafkaOptions.RaiseOnFailureStatus"/> is
/// enabled and a message handler reported an unsuccessful result without itself throwing -
/// escalating the failure into an exception so it's treated the same as an unhandled exception for
/// retry purposes.
/// </summary>
public class KafkaMessageProcessingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaMessageProcessingException"/> class.
    /// </summary>
    /// <param name="topic">The Kafka topic the failing record was on.</param>
    public KafkaMessageProcessingException(string topic)
        : base($"Message handler reported an unsuccessful result for a Kafka record on topic {topic}.")
    {
        Topic = topic;
    }

    /// <summary>
    /// Gets the Kafka topic the failing record was on.
    /// </summary>
    public string Topic { get; }
}
