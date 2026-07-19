namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// What <see cref="ResponseEventsMiddleware{TRequest,TResponse}"/> does when publishing a response
/// event fails (the publisher throws, or returns an unsuccessful result).
/// </summary>
public enum PublishFailureMode
{
    /// <summary>
    /// Replace the handler's response with an <c>UnexpectedError</c> result, so the transport
    /// reports the message as failed and (for queue transports) redelivers it. This is honest
    /// at-least-once delivery: the handler and the event's consumers must be idempotent, because a
    /// redelivered message re-runs the handler. The default.
    /// </summary>
    FailMessage,

    /// <summary>
    /// Log a warning and keep the handler's response - the message is acknowledged even though the
    /// event was lost. For pipelines where the follow-up event is best-effort.
    /// </summary>
    LogAndContinue,
}
