namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Implemented by a transport's message-handler context (e.g. <c>KafkaContext</c>, <c>SnsRecordContext</c>,
/// <c>GrpcContext</c>) to carry the handler's <see cref="IMessageResult"/> back out to that transport's
/// own result-setter, so it can decide how to acknowledge/complete the message. Many transports (Kafka,
/// Azure Functions triggers) use a no-op result setter and rely on the trigger's default auto-complete
/// behavior instead; see each package's own result-setter implementation for its actual handling.
/// </summary>
public interface IHasMessageResult
{
    /// <summary>The outcome of handling this message, set by the message handler pipeline.</summary>
    IMessageResult MessageResult { get; set; }
}