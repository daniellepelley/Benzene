using Benzene.Abstractions.Results;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// The outcome of running a Service Bus message through the pipeline: the handler's recorded
/// <see cref="IBenzeneResult"/> plus any explicit settlement the handler requested via
/// <see cref="ServiceBusSettlementHolder"/>. Returned by <see cref="ServiceBusConsumerApplication"/>
/// so <see cref="BenzeneServiceBusWorker"/> can settle the message in
/// <see cref="ServiceBusConsumerAckMode.Explicit"/> mode.
/// </summary>
public class ServiceBusSettlementDecision
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusSettlementDecision"/> class.
    /// </summary>
    /// <param name="messageResult">The handler's recorded result, or <c>null</c> if none was set.</param>
    /// <param name="settlement">The scoped settlement holder (its <see cref="ServiceBusSettlementHolder.Override"/> may be <c>null</c>), or <c>null</c> if not registered.</param>
    public ServiceBusSettlementDecision(IBenzeneResult? messageResult, ServiceBusSettlementHolder? settlement)
    {
        MessageResult = messageResult;
        Settlement = settlement;
    }

    /// <summary>Gets the handler's recorded result, or <c>null</c> if nothing set one.</summary>
    public IBenzeneResult? MessageResult { get; }

    /// <summary>Gets the settlement holder the handler may have set an override on, or <c>null</c>.</summary>
    public ServiceBusSettlementHolder? Settlement { get; }
}
