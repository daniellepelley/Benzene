using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Sns;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into an <see cref="SNSEvent"/> to the SNS
/// middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by <see cref="Extensions.UseSns"/>.
/// It only handles the invocation if the event has records whose source is <c>aws:sns</c>; otherwise it
/// defers to the next middleware. SNS notifications don't return a response — this is a fire-and-forget
/// pattern.
/// </remarks>
public class SnsLambdaHandler : AwsLambdaMiddlewareRouter<SNSEvent>
{
    // Source-generated JSON metadata for this handler's event type, built once per process, replacing
    // the base router's reflection serializer so the first (cold) invocation skips the metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<SnsJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly IMiddlewareApplication<SNSEvent> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsLambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The SNS application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public SnsLambdaHandler(
        IMiddlewareApplication<SNSEvent> application,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _application = application;
    }

    /// <summary>
    /// Determines whether the deserialized request looks like an SNS event.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the event has at least one record sourced from SNS; otherwise, false.</returns>
    protected override bool CanHandle(SNSEvent request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:sns";
    }

    /// <summary>
    /// Handles the event by running it through the SNS application. No response is written.
    /// </summary>
    /// <param name="request">The SNS batch event extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context for this invocation.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(SNSEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        await _application.HandleAsync(request, serviceResolverFactory);
    }
}
