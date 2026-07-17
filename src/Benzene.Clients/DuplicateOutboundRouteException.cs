namespace Benzene.Clients;

/// <summary>
/// Thrown by <see cref="OutboundRoutingBuilder.Build"/> when the same topic was registered via
/// <see cref="OutboundRoutingBuilder.Route"/> more than once - a named, purpose-specific replacement
/// for the bare <see cref="ArgumentException"/> the previous <c>ClientsBuilder</c> threw for the
/// same mistake.
/// </summary>
public class DuplicateOutboundRouteException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateOutboundRouteException"/> class.
    /// </summary>
    /// <param name="topic">The topic that was registered more than once.</param>
    public DuplicateOutboundRouteException(string topic)
        : base($"Topic '{topic}' was registered more than once via OutboundRoutingBuilder.Route - each topic may only have one outbound route.")
    {
        Topic = topic;
    }

    /// <summary>Gets the topic that was registered more than once.</summary>
    public string Topic { get; }
}
