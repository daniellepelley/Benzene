using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers.Response;

/// <summary>
/// Records a handler's outcome onto a transport context that can carry one
/// (<see cref="IHasMessageResult"/>), so a cross-cutting observer of the completed pipeline can read a
/// real success/failure signal.
/// </summary>
/// <remarks>
/// Event-style transports (SQS, SNS, EventBridge, Service Bus, …) already report their outcome by
/// setting <see cref="IHasMessageResult.MessageResult"/> from their result-setter. Request/response
/// transports (HTTP/API Gateway, ASP.NET, BenzeneMessage) instead report by <em>writing a response</em>
/// and historically left <see cref="IHasMessageResult.MessageResult"/> unset — which meant a
/// non-throwing completion looked like it had no result signal at all (the source of the
/// <c>&lt;missing&gt;</c> <c>result</c> tag on <c>benzene.messages.processed</c>). Those setters now call
/// this after writing the response so the same signal is available on every transport whose context
/// implements <see cref="IHasMessageResult"/>. Only sets the result when it hasn't already been set, so
/// a transport that records its own outcome earlier keeps it.
/// </remarks>
public static class MessageResultRecorder
{
    /// <summary>
    /// Copies <paramref name="messageHandlerResult"/>'s outcome onto <paramref name="context"/> when the
    /// context implements <see cref="IHasMessageResult"/> and hasn't already recorded a result.
    /// </summary>
    /// <param name="context">The transport context the pipeline just completed for.</param>
    /// <param name="messageHandlerResult">The outcome of routing and handling the message.</param>
    public static void Record(object context, IMessageHandlerResult messageHandlerResult)
    {
        if (context is IHasMessageResult { MessageResult: null } hasResult)
        {
            hasResult.MessageResult = messageHandlerResult.BenzeneResult;
        }
    }
}
