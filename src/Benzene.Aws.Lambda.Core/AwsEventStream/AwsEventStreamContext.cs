using System.IO;
using Amazon.Lambda.Core;

namespace Benzene.Aws.Lambda.Core.AwsEventStream;

/// <summary>
/// Provides the raw stream-based context for an AWS Lambda invocation, before the event has been
/// identified as a specific event source type (API Gateway, SQS, SNS, etc.).
/// </summary>
/// <remarks>
/// This is the context type used by the outermost <see cref="Benzene.Core.Middleware.MiddlewarePipelineBuilder{TContext}"/>
/// configured in a Lambda <c>StartUp</c> class. Event-source-specific middleware (API Gateway, SQS, SNS, ...)
/// reads <see cref="Stream"/>, attempts to deserialize it into its own request type, and if successful
/// writes the response to <see cref="Response"/>.
/// </remarks>
public class AwsEventStreamContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsEventStreamContext"/> class.
    /// </summary>
    /// <param name="stream">The raw Lambda invocation payload stream.</param>
    /// <param name="lambdaContext">The AWS Lambda execution context for this invocation.</param>
    public AwsEventStreamContext(Stream stream, ILambdaContext lambdaContext)
    {
        Stream = stream;
        Response = new MemoryStream();
        LambdaContext = lambdaContext;
    }

    /// <summary>
    /// Gets the raw Lambda invocation payload stream.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Gets the AWS Lambda execution context for this invocation.
    /// </summary>
    public ILambdaContext LambdaContext { get; }

    /// <summary>
    /// Gets or sets the response stream to be returned from the Lambda invocation.
    /// </summary>
    /// <remarks>
    /// Initialized to an empty <see cref="MemoryStream"/>. Middleware that handles the event writes
    /// its response here; if no middleware recognizes the event, this remains unset by any handler
    /// and <see cref="AwsLambdaEntryPoint"/> raises an error.
    /// </remarks>
    public Stream Response { get; set; }
}
