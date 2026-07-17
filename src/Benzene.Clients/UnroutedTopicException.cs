namespace Benzene.Clients;

/// <summary>
/// Thrown by <see cref="IBenzeneMessageSender.SendAsync{TRequest,TResponse}"/> when no outbound
/// route is registered for the given topic. Expected to be rare once
/// <c>Benzene.CodeGen.Client</c>'s generated <c>ValidateOutboundRouting()</c> compile-time/startup
/// safety net is the norm - this is the runtime fallback, not the primary safety net.
/// </summary>
public class UnroutedTopicException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnroutedTopicException"/> class.
    /// </summary>
    /// <param name="topic">The topic that has no registered outbound route.</param>
    public UnroutedTopicException(string topic)
        : base($"No outbound route is registered for topic '{topic}'. Register one via OutboundRoutingBuilder.Route(\"{topic}\", ...).")
    {
        Topic = topic;
    }

    /// <summary>Gets the topic that has no registered outbound route.</summary>
    public string Topic { get; }
}
