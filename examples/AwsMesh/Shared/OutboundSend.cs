namespace Benzene.Examples.AwsMesh.Shared;

/// <summary>
/// The AWS messaging transport an <see cref="OutboundSend"/> is carried over. Each is used for what
/// it's idiomatically good at: <see cref="Sqs"/> for a point-to-point command to one consumer,
/// <see cref="Sns"/> for an event fanned out to many subscribers, <see cref="EventBridge"/> for a
/// routed integration event.
/// </summary>
public enum OutboundTransport
{
    /// <summary>Point-to-point command queue — one consumer. <c>TargetEnvVar</c> holds the queue URL.</summary>
    Sqs,

    /// <summary>Pub/sub event, one publisher → many subscribers. <c>TargetEnvVar</c> holds the topic ARN.</summary>
    Sns,

    /// <summary>Routed integration event. <c>TargetEnvVar</c> holds the event bus name (source = the sending service).</summary>
    EventBridge
}

/// <summary>
/// Declares that a service sends <see cref="Topic"/> (payload <see cref="MessageType"/>) downstream over
/// <see cref="Transport"/>, routed at runtime to the target whose identifier is in the
/// <see cref="TargetEnvVar"/> environment variable — the ingress the target service already consumes.
/// Used by <see cref="MeshServiceWiring.ConfigureServices"/> to both surface the topic in the spec's
/// <c>events</c> (→ structural topology) and register the outbound route on the chosen transport.
/// </summary>
/// <param name="Topic">The topic sent downstream (e.g. <c>payments:capture</c>).</param>
/// <param name="MessageType">The payload type (for the spec schema).</param>
/// <param name="Transport">Which AWS transport carries it (SQS / SNS / EventBridge).</param>
/// <param name="TargetEnvVar">
/// The env var holding the transport's target identifier — an SQS queue URL, an SNS topic ARN, or an
/// EventBridge event bus name (e.g. <c>PAYMENTS_QUEUE_URL</c>, <c>ORDER_PLACED_TOPIC_ARN</c>, <c>EVENT_BUS_NAME</c>).
/// </param>
public record OutboundSend(string Topic, Type MessageType, OutboundTransport Transport, string TargetEnvVar)
{
    /// <summary>Convenience for the common SQS command hop.</summary>
    public static OutboundSend Sqs(string topic, Type messageType, string queueUrlEnvVar)
        => new(topic, messageType, OutboundTransport.Sqs, queueUrlEnvVar);

    /// <summary>Convenience for an SNS fan-out event.</summary>
    public static OutboundSend Sns(string topic, Type messageType, string topicArnEnvVar)
        => new(topic, messageType, OutboundTransport.Sns, topicArnEnvVar);

    /// <summary>Convenience for an EventBridge routed integration event.</summary>
    public static OutboundSend EventBridge(string topic, Type messageType, string eventBusNameEnvVar)
        => new(topic, messageType, OutboundTransport.EventBridge, eventBusNameEnvVar);
}
