using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Google.Cloud.PubSub.V1;

namespace Benzene.Clients.GoogleCloud.PubSub;

/// <summary>
/// Middleware that publishes the <see cref="PubSubSendMessageContext"/>'s message to Pub/Sub and
/// records the server-assigned message id on the context. A publish failure throws (Pub/Sub has no
/// per-message HTTP status the way SQS does), which the outbound routing pipeline surfaces to the
/// caller.
/// </summary>
public class PubSubClientMiddleware : IMiddleware<PubSubSendMessageContext>
{
    private readonly PublisherServiceApiClient _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubClientMiddleware"/> class.
    /// </summary>
    /// <param name="publisher">The Pub/Sub publisher API client used to publish messages.</param>
    public PubSubClientMiddleware(PublisherServiceApiClient publisher)
    {
        _publisher = publisher;
    }

    /// <summary>Gets the name of this middleware.</summary>
    public string Name => nameof(PubSubClientMiddleware);

    /// <summary>
    /// Publishes the context's message and records the returned message id. Terminal middleware; does
    /// not call <paramref name="next"/>.
    /// </summary>
    /// <param name="context">The context carrying the topic and message to publish.</param>
    /// <param name="next">Unused; this middleware does not delegate further down the pipeline.</param>
    public async Task HandleAsync(PubSubSendMessageContext context, Func<Task> next)
    {
        var response = await _publisher.PublishAsync(context.TopicName, new[] { context.Message });
        context.MessageId = response.MessageIds.FirstOrDefault() ?? "";
    }
}
