using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.GoogleCloud.Functions.PubSub;

/// <summary>
/// Extracts headers from a Pub/Sub message's attributes.
/// </summary>
public class PubSubMessageHeadersGetter : IMessageHeadersGetter<PubSubContext>
{
    /// <summary>
    /// Gets the headers for the Pub/Sub message from its attributes.
    /// </summary>
    /// <param name="context">The Pub/Sub context to extract headers from.</param>
    /// <returns>The message headers.</returns>
    public IDictionary<string, string> GetHeaders(PubSubContext context)
    {
        return context.Message.Attributes.ToDictionary(x => x.Key, x => x.Value);
    }
}
