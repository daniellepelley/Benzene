using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into an <see cref="SQSEvent"/> to the SQS
/// middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by <see cref="Extensions.UseSqs"/>.
/// It only handles the invocation if the event has records whose source is <c>aws:sqs</c>; otherwise it
/// defers to the next middleware.
/// </remarks>
public class SqsLambdaHandler : AwsLambdaMiddlewareRouter<SQSEvent>
{
    // Source-generated JSON metadata for this handler's event types, built once per process, replacing
    // the base router's reflection serializer so the first (cold) invocation skips the metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<SqsJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly IMiddlewareApplication<SQSEvent, SQSBatchResponse> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsLambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The SQS application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public SqsLambdaHandler(
        IMiddlewareApplication<SQSEvent, SQSBatchResponse> application,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _application = application;
    }

    /// <summary>
    /// Determines whether the deserialized request looks like an SQS event.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the event has at least one record sourced from SQS; otherwise, false.</returns>
    protected override bool CanHandle(SQSEvent request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:sqs";
    }

    /// <summary>
    /// Handles the event by running it through the SQS application and writing the batch response.
    /// </summary>
    /// <param name="request">The SQS batch event extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(SQSEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _application.HandleAsync(request, serviceResolverFactory);
        MapResponse(context, response);
    }
}
