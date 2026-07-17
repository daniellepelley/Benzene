namespace Benzene.Clients;

/// <summary>
/// The pipeline context for one outbound send: the topic being sent on, the request payload, and a
/// settable slot for the response - the outbound mirror of how inbound transport contexts carry a
/// request and a result. Deliberately non-generic (matching every other <c>IMiddleware&lt;TContext&gt;</c>
/// in this codebase, e.g. <c>SqsClientMiddleware</c>/<c>SnsClientMiddleware</c>) rather than
/// <c>OutboundContext&lt;TRequest&gt;</c> - see <c>work/benzene-clients-redesign-plan.md</c> §2.2/§5.
/// </summary>
public class OutboundContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundContext"/> class.
    /// </summary>
    /// <param name="topic">The topic being sent on.</param>
    /// <param name="request">The request payload.</param>
    public OutboundContext(string topic, object request)
    {
        Topic = topic;
        Request = request;
    }

    /// <summary>Gets the topic this send was routed to.</summary>
    public string Topic { get; }

    /// <summary>Gets the request payload being sent.</summary>
    public object Request { get; }

    /// <summary>
    /// Gets or sets the response, set by the outbound pipeline's transport middleware (e.g.
    /// <c>SqsClientMiddleware</c>) once the send completes. Read back by
    /// <see cref="DefaultBenzeneMessageSender"/> after the pipeline finishes.
    /// </summary>
    public object? Response { get; set; }
}
