namespace Benzene.Azure.ServiceBus;

/// <summary>
/// The explicit settlement a handler can request for a Service Bus message (in
/// <see cref="ServiceBusConsumerAckMode.Explicit"/> mode), overriding the default
/// success→complete/failure→abandon behavior. Set via <see cref="ServiceBusSettlementHolder"/>.
/// </summary>
public enum ServiceBusSettlement
{
    /// <summary>Complete the message (remove it from the entity).</summary>
    Complete,

    /// <summary>Abandon the message so it's redelivered (subject to max-delivery-count).</summary>
    Abandon,

    /// <summary>
    /// Move the message to the entity's dead-letter sub-queue with the reason/description from
    /// <see cref="ServiceBusSettlementHolder"/> - so a poison message is quarantined instead of
    /// abandon-looping to max-delivery-count.
    /// </summary>
    DeadLetter,

    /// <summary>
    /// Defer the message: it stays in the entity but is only retrievable by sequence number. Receiving
    /// deferred messages back is an advanced path the caller owns (track the sequence number yourself);
    /// this only defers.
    /// </summary>
    Defer,
}
