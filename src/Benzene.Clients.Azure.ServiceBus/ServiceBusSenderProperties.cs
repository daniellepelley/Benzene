namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// Optional, opt-in mapping of outbound message <em>headers</em> onto broker-level
/// <see cref="Azure.Messaging.ServiceBus.ServiceBusMessage"/> properties. Each property names the
/// header key to read; a <c>null</c> key (the default) leaves that broker property unset. Mirrors the
/// configurable-header pattern used elsewhere (e.g. the Event Hub <c>partitionKeyHeader</c>).
/// </summary>
public class ServiceBusSenderProperties
{
    /// <summary>
    /// The header whose value sets <see cref="Azure.Messaging.ServiceBus.ServiceBusMessage.MessageId"/>,
    /// enabling broker-side duplicate detection (valuable under at-least-once delivery). <c>null</c>
    /// leaves the SDK-generated id.
    /// </summary>
    public string? MessageIdHeader { get; set; }

    /// <summary>
    /// The header whose value sets <see cref="Azure.Messaging.ServiceBus.ServiceBusMessage.SessionId"/>
    /// — required to produce to a session-enabled entity. <c>null</c> sends without a session.
    /// </summary>
    public string? SessionIdHeader { get; set; }

    /// <summary>
    /// The header whose value (parsed as an ISO-8601 timestamp) sets
    /// <see cref="Azure.Messaging.ServiceBus.ServiceBusMessage.ScheduledEnqueueTime"/>, so the broker
    /// holds the message until then. <c>null</c> enqueues immediately.
    /// </summary>
    public string? ScheduledEnqueueTimeHeader { get; set; }

    /// <summary>
    /// The header whose value sets <see cref="Azure.Messaging.ServiceBus.ServiceBusMessage.TimeToLive"/>.
    /// The value is parsed as a number of seconds, or as an ISO-8601 duration (e.g. <c>PT30S</c>), or
    /// as a <see cref="System.TimeSpan"/> string (e.g. <c>00:00:30</c>). <c>null</c> uses the entity's
    /// default TTL.
    /// </summary>
    public string? TimeToLiveHeader { get; set; }
}
