using Amazon.Lambda.Model;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// Provides the middleware pipeline context for invoking a single AWS Lambda function.
/// </summary>
public class LambdaSendMessageContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaSendMessageContext"/> class.
    /// </summary>
    /// <param name="request">The Lambda invoke request.</param>
    public LambdaSendMessageContext(InvokeRequest request)
    {
        Request = request;
    }

    /// <summary>
    /// Gets the Lambda invoke request.
    /// </summary>
    public InvokeRequest Request { get; }

    /// <summary>
    /// Gets or sets the Lambda invoke response. Set by <see cref="AwsLambdaClientMiddleware"/>.
    /// </summary>
    public InvokeResponse Response { get; set; }
}
