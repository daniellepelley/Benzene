namespace Benzene.Clients;

/// <summary>
/// The set of topics actually registered via <see cref="OutboundRoutingBuilder"/> -
/// registered as a singleton by <c>AddOutboundRouting(...)</c> so
/// <see cref="ValidateOutboundRoutingExtensions.ValidateOutboundRouting"/> can compare it against
/// every generated client's <c>RequiredTopics</c> without re-deriving the routing table.
/// </summary>
public class OutboundRoutingTopics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundRoutingTopics"/> class.
    /// </summary>
    /// <param name="topics">The topics registered via <see cref="OutboundRoutingBuilder.Route"/>.</param>
    public OutboundRoutingTopics(IEnumerable<string> topics)
    {
        Topics = new HashSet<string>(topics);
    }

    /// <summary>Gets the registered topics.</summary>
    public IReadOnlySet<string> Topics { get; }
}
