using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// Extracts headers from a Service Bus message's string-typed application properties.
/// </summary>
public class ServiceBusMessageHeadersGetter : IMessageHeadersGetter<ServiceBusContext>
{
    /// <summary>
    /// Gets the headers for the Service Bus message from its string-typed application properties.
    /// </summary>
    /// <param name="context">The Service Bus context to extract headers from.</param>
    /// <returns>The message headers.</returns>
    public IDictionary<string, string> GetHeaders(ServiceBusContext context)
    {
        return context.Message.ApplicationProperties
            .Where(x => x.Value is string)
            .ToDictionary(x => x.Key, x => (string)x.Value);
    }
}
