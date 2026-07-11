namespace Benzene.Abstractions;

/// <summary>
/// Provides a fluent builder interface for constructing message-based requests in test scenarios.
/// This interface enables message queue and event-based testing of Benzene handlers and middleware.
/// </summary>
/// <typeparam name="T">The type of the message body.</typeparam>
public interface IMessageBuilder<T>
{
    /// <summary>
    /// Gets the message headers or metadata.
    /// </summary>
    IDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the topic or queue name for the message.
    /// </summary>
    string Topic { get; }

    /// <summary>
    /// Gets the message body.
    /// </summary>
    T? Message { get; }
}