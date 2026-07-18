using System;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Publishes the pipeline context's request to SNS and records the response.
/// </summary>
public class SnsClientMiddleware : IMiddleware<SnsSendMessageContext>
{
    private readonly IAmazonSimpleNotificationService _amazonSns;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsClientMiddleware"/> class.
    /// </summary>
    /// <param name="amazonSns">The SNS client used to publish the message.</param>
    public SnsClientMiddleware(IAmazonSimpleNotificationService amazonSns)
    {
        _amazonSns = amazonSns;
    }

    /// <summary>
    /// Gets the name of this middleware component.
    /// </summary>
    public string Name => nameof(SnsClientMiddleware);

    /// <summary>
    /// Publishes the request to SNS and stores the response on the context. Does not call <paramref name="next"/>.
    /// </summary>
    /// <param name="context">The SNS send message context.</param>
    /// <param name="next">The next middleware in the pipeline (not invoked).</param>
    public async Task HandleAsync(SnsSendMessageContext context, Func<Task> next)
    {
        context.Response = await _amazonSns.PublishAsync(context.Request);
    }
}
