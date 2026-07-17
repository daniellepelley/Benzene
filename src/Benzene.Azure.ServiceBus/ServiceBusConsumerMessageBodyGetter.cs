using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.ServiceBus;

/// <summary>
/// Extracts the message body from a Service Bus message received by the self-hosted consumer.
/// </summary>
public class ServiceBusConsumerMessageBodyGetter : IMessageBodyGetter<ServiceBusConsumerContext>
{
    /// <summary>
    /// Gets the Service Bus message's body as a string.
    /// </summary>
    /// <param name="context">The Service Bus consumer context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string? GetBody(ServiceBusConsumerContext context)
    {
        return context.Message.Body?.ToString();
    }
}
