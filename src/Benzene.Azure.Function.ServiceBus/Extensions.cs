using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;

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
}
