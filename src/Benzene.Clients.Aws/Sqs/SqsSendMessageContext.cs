using Amazon.SQS.Model;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Provides the middleware pipeline context for sending a single message to SQS.
/// </summary>
public class SqsSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsSendMessageContext"/> class.
    /// </summary>
    /// <param name="request">The SQS send message request.</param>
    public SqsSendMessageContext(SendMessageRequest request)
    {
        Request = request;
    }

    /// <summary>
    /// Gets the SQS send message request.
    /// </summary>
    public SendMessageRequest Request { get; }

    /// <summary>
    /// Gets or sets the SQS send message response. Set by <see cref="SqsClientMiddleware"/>.
    /// </summary>
    public SendMessageResponse Response { get; set; }
}
