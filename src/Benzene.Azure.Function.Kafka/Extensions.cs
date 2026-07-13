using Benzene.Azure.Function.Core;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// Provides extension methods for dispatching Kafka trigger events to a built <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Dispatches Kafka event data to the Azure Function app's Kafka entry point application.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="eventData">The Kafka events to handle.</param>
    /// <returns>A task that completes when the batch has been handled.</returns>
    public static Task HandleKafkaEvents(this IAzureFunctionApp source, params KafkaRecord[] eventData)
    {
        return source.HandleAsync(eventData);
    }
}
