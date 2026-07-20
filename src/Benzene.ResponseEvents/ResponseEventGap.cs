using Benzene.Abstractions.Messages;

namespace Benzene.ResponseEvents;

/// <summary>
/// One finding from the unmapped-response-handler diagnostic
/// (<see cref="ResponseEventDiagnosticsExtensions.FindUnmappedResponseHandlers"/>): a handler that
/// returns a response payload on a topic no response-event mapping covers. On a fire-and-forget
/// transport (SQS, SNS, EventBridge, ...) that payload is discarded after the message is
/// acknowledged; if it should become an event, a <c>UseResponseEvents</c> mapping is missing.
/// </summary>
public sealed class ResponseEventGap
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseEventGap"/> class.
    /// </summary>
    /// <param name="topic">The topic the handler answers.</param>
    /// <param name="handlerType">The handler CLR type.</param>
    /// <param name="responseType">The response payload type the handler returns.</param>
    public ResponseEventGap(ITopic topic, Type handlerType, Type responseType)
    {
        Topic = topic;
        HandlerType = handlerType;
        ResponseType = responseType;
    }

    /// <summary>The topic the handler answers.</summary>
    public ITopic Topic { get; }

    /// <summary>The handler CLR type.</summary>
    public Type HandlerType { get; }

    /// <summary>The response payload type the handler returns but that has nowhere to go on a fire-and-forget transport.</summary>
    public Type ResponseType { get; }

    /// <summary>A human-readable summary of the gap, for logging.</summary>
    public string Description =>
        $"Handler {HandlerType.Name} on topic '{Topic.Id}' returns a {ResponseType.Name} response, " +
        "but no response-event mapping covers it - on a fire-and-forget transport that payload is " +
        "discarded. Add a UseResponseEvents mapping if you intend to publish it as an event " +
        "(safe to ignore for a topic served only over HTTP/gRPC, where the response is the reply).";
}
