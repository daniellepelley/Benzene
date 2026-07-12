using System;
using System.Threading.Tasks;
using Amazon.SQS;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Middleware that sends the <see cref="SqsSendMessageContext"/>'s request to SQS and records the
/// response on the context.
/// </summary>
public class SqsClientMiddleware : IMiddleware<SqsSendMessageContext>
{
    private readonly IAmazonSQS _amazonSqs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsClientMiddleware"/> class.
    /// </summary>
    /// <param name="amazonSqs">The SQS client used to send the message.</param>
    public SqsClientMiddleware(IAmazonSQS amazonSqs)
    {
        _amazonSqs = amazonSqs;
    }

    /// <summary>
    /// Gets the name of this middleware.
    /// </summary>
    public string Name => nameof(SqsClientMiddleware);

    /// <summary>
    /// Sends the context's request to SQS and sets the response. This is a terminal middleware; it does
    /// not call <paramref name="next"/>.
    /// </summary>
    /// <param name="context">The context carrying the request to send and to receive the response.</param>
    /// <param name="next">Unused; this middleware does not delegate further down the pipeline.</param>
    public async Task HandleAsync(SqsSendMessageContext context, Func<Task> next)
    {
        context.Response = await _amazonSqs.SendMessageAsync(context.Request);
    }
}
