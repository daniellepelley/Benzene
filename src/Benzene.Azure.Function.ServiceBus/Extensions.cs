using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Provides extension methods for dispatching Service Bus trigger messages to a built <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Dispatches Service Bus messages to the Azure Function app's Service Bus entry point application.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="messages">The Service Bus messages to handle (a single message for a non-batched trigger, or a batch for one configured with <c>IsBatched = true</c>).</param>
    /// <returns>A task that completes when the batch has been handled.</returns>
    public static Task HandleServiceBusMessages(this IAzureFunctionApp source, params ServiceBusReceivedMessage[] messages)
    {
        return source.HandleAsync(messages);
    }

    /// <summary>
    /// Dispatches Service Bus messages, together with the <see cref="ServiceBusMessageActions"/>
    /// needed to complete/abandon them individually, to the Azure Function app's Service Bus entry
    /// point application. Use this overload - and bind <see cref="ServiceBusMessageActions"/> as a
    /// trigger function parameter - when <see cref="ServiceBusOptions.AckMode"/> is set to
    /// <see cref="ServiceBusAckMode.Explicit"/>; it has no effect for the default
    /// <see cref="ServiceBusAckMode.AutoComplete"/>.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="messageActions">The actions object bound by the trigger, used to complete/abandon each message.</param>
    /// <param name="messages">The Service Bus messages to handle (a single message for a non-batched trigger, or a batch for one configured with <c>IsBatched = true</c>).</param>
    /// <returns>A task that completes when the batch has been handled.</returns>
    /// <remarks>
    /// Requires the trigger's <c>[ServiceBusTrigger]</c> attribute to set <c>AutoCompleteMessages = false</c>
    /// - otherwise the Functions host completes the message itself before this overload's explicit
    /// completion ever runs.
    /// </remarks>
    public static Task HandleServiceBusMessages(this IAzureFunctionApp source, ServiceBusMessageActions messageActions, params ServiceBusReceivedMessage[] messages)
    {
        return source.HandleAsync(new ServiceBusTriggerBatch(messageActions, messages));
    }
}
