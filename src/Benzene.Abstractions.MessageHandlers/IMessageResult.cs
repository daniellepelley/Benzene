namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Outcome of handling one message, as reported back to a transport's own acknowledgement/completion
/// mechanism (e.g. an Azure Functions trigger's implicit auto-complete, an SQS/Kafka consumer's
/// commit decision) via <see cref="IHasMessageResult"/> and an <c>IMessageHandlerResultSetter{TContext}</c>.
/// </summary>
public interface IMessageResult
{
    /// <summary>Whether the message was handled successfully.</summary>
    bool IsSuccessful { get; }
}