using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Carries a Service Bus trigger's messages together with the <see cref="ServiceBusMessageActions"/>
/// needed to complete/abandon them individually - a distinct request type from a plain
/// <c>ServiceBusReceivedMessage[]</c> so <see cref="ServiceBusAckMode.Explicit"/> can be dispatched
/// to specifically, without changing the existing <c>HandleServiceBusMessages(params
/// ServiceBusReceivedMessage[])</c> overload's behavior for callers that don't use it.
/// </summary>
public class ServiceBusTriggerBatch
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusTriggerBatch"/> class.
    /// </summary>
    /// <param name="messageActions">The actions object used to complete/abandon each message.</param>
    /// <param name="messages">The messages delivered for this invocation.</param>
    public ServiceBusTriggerBatch(ServiceBusMessageActions messageActions, ServiceBusReceivedMessage[] messages)
    {
        MessageActions = messageActions;
        Messages = messages;
    }

    /// <summary>Gets the actions object used to complete/abandon each message.</summary>
    public ServiceBusMessageActions MessageActions { get; }

    /// <summary>Gets the messages delivered for this invocation.</summary>
    public ServiceBusReceivedMessage[] Messages { get; }
}
