namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Scoped, per-message holder a handler resolves to request an explicit settlement outcome
/// (dead-letter, defer, complete, abandon), overriding the default success→complete/failure→abandon
/// behavior in <see cref="ServiceBusConsumerAckMode.Explicit"/> mode. Follows the "scoped DI state,
/// not context" convention (like <c>PresetTopicHolder</c>) so <see cref="ServiceBusConsumerContext"/>
/// stays a pure description of the message. Registered scoped by <c>AddServiceBusConsumer</c>; a fresh
/// instance per message, default (no override) unless the handler sets it.
/// </summary>
/// <remarks>
/// Only honored in <see cref="ServiceBusConsumerAckMode.Explicit"/> mode - in
/// <see cref="ServiceBusConsumerAckMode.AutoComplete"/> the processor settles the message itself, so
/// an override has no effect.
/// </remarks>
public class ServiceBusSettlementHolder
{
    /// <summary>The requested settlement, or <c>null</c> to use the default outcome-based settlement.</summary>
    public ServiceBusSettlement? Override { get; set; }

    /// <summary>The dead-letter reason (a short code), used when <see cref="Override"/> is <see cref="ServiceBusSettlement.DeadLetter"/>. Never a secret.</summary>
    public string? DeadLetterReason { get; set; }

    /// <summary>The dead-letter description, used when <see cref="Override"/> is <see cref="ServiceBusSettlement.DeadLetter"/>. Never a secret.</summary>
    public string? DeadLetterDescription { get; set; }
}
