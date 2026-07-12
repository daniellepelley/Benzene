using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.EventBridge;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into an <see cref="S3Event"/> to the S3
/// middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by <see cref="Extensions.UseS3"/>.
/// It only handles the invocation if the event has records whose source is <c>aws:s3</c>; otherwise it
/// defers to the next middleware. S3 event notifications don't return a response — this is a
/// fire-and-forget pattern.
/// </remarks>
public class S3LambdaHandler : AwsLambdaMiddlewareRouter<S3Event>
{
    private readonly IMiddlewareApplication<S3Event> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3LambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The S3 application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public S3LambdaHandler(
        IMiddlewareApplication<S3Event> application,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _application = application;
    }

    /// <summary>
    /// Determines whether the deserialized request looks like an S3 event.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the event has at least one record sourced from S3; otherwise, false.</returns>
    protected override bool CanHandle(S3Event request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:s3";
    }

    /// <summary>
    /// Handles the event by running it through the S3 application. No response is written.
    /// </summary>
    /// <param name="request">The S3 event notification batch extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context for this invocation.</param>
    /// <param name="serviceResolver">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(S3Event request, AwsEventStreamContext context, IServiceResolverFactory serviceResolver)
    {
        await _application.HandleAsync(request, serviceResolver);
    }
}
