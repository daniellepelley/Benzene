using Amazon.SimpleNotificationService.Model;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Provides the middleware pipeline context for publishing a single message to SNS.
/// </summary>
public class SnsSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsSendMessageContext"/> class.
    /// </summary>
    /// <param name="request">The SNS publish request to send.</param>
    public SnsSendMessageContext(PublishRequest request)
    {
        Request = request;
    }

    /// <summary>
    /// Gets the SNS publish request.
    /// </summary>
    public PublishRequest Request { get; }

    /// <summary>
    /// Gets or sets the SNS publish response. Set by <see cref="SnsClientMiddleware"/>.
    /// </summary>
    public PublishResponse Response { get; set; }
}
