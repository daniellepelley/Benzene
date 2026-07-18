namespace Benzene.Examples.AwsMesh.Shared;

/// <summary>
/// Declares that a service sends <see cref="Topic"/> (payload <see cref="MessageType"/>) downstream,
/// routed at runtime to the SQS queue whose URL is in the <see cref="QueueUrlEnvVar"/> environment
/// variable — the ingress the target service already consumes. Used by
/// <see cref="MeshServiceWiring.ConfigureServices"/> to both surface the topic in the spec's
/// <c>events</c> (→ structural topology) and register the outbound route.
/// </summary>
/// <param name="Topic">The topic sent downstream (e.g. <c>payments:capture</c>).</param>
/// <param name="MessageType">The payload type (for the spec schema).</param>
/// <param name="QueueUrlEnvVar">The env var holding the target queue's URL (e.g. <c>PAYMENTS_QUEUE_URL</c>).</param>
public record OutboundSend(string Topic, Type MessageType, string QueueUrlEnvVar);
