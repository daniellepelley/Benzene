using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Extracts the message body from a Service Bus message.
/// </summary>
public class ServiceBusMessageBodyGetter : IMessageBodyGetter<ServiceBusContext>
{
    /// <summary>
    /// Gets the Service Bus message's body as a string.
    /// </summary>
    /// <param name="context">The Service Bus context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(ServiceBusContext context)
    {
        return context.Message.Body?.ToString();
    }
}
