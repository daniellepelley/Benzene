namespace Benzene.Clients;

/// <summary>
/// Thrown by <see cref="ValidateOutboundRoutingExtensions.ValidateOutboundRouting"/> when one or
/// more topics named by a generated client's <c>RequiredTopics</c> have no registered outbound
/// route - a missing <see cref="OutboundRoutingBuilder.Route"/> call caught at startup instead of
/// the first time that rarely-hit code path executes.
/// </summary>
public class MissingOutboundRoutesException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingOutboundRoutesException"/> class.
    /// </summary>
    /// <param name="missingTopics">Every required topic with no registered outbound route.</param>
    public MissingOutboundRoutesException(string[] missingTopics)
        : base($"The following topics are required by a generated client but have no registered outbound route: {string.Join(", ", missingTopics)}. Register each via OutboundRoutingBuilder.Route.")
    {
        MissingTopics = missingTopics;
    }

    /// <summary>Gets every required topic with no registered outbound route.</summary>
    public string[] MissingTopics { get; }
}
