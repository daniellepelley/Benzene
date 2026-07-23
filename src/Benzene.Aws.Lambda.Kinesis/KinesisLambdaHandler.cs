using System.Threading.Tasks;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Kinesis;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into a <see cref="KinesisEvent"/> to the
/// Kinesis streaming pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by <see cref="Extensions.UseKinesisStream"/>.
/// It only handles the invocation when the first record's source is <c>aws:kinesis</c>; otherwise it
/// defers to the next event source adapter. Writes back a <see cref="KinesisBatchResponse"/> naming
/// the sequence number to resume from — Kinesis event source mapping invocations are synchronous
/// from Lambda's own perspective once <c>ReportBatchItemFailures</c> is configured on the trigger,
/// even though no response was written here before this type existed.
/// </remarks>
public class KinesisLambdaHandler : AwsLambdaMiddlewareRouter<KinesisEvent>
{
    // Source-generated JSON metadata for this handler's event types, built once per process, replacing
    // the base router's reflection serializer so the first (cold) invocation skips the metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<KinesisJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly IMiddlewareApplication<KinesisEvent, KinesisBatchResponse> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisLambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The Kinesis application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public KinesisLambdaHandler(
        IMiddlewareApplication<KinesisEvent, KinesisBatchResponse> application,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _application = application;
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a Kinesis event.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the event has at least one record sourced from Kinesis; otherwise, false.</returns>
    protected override bool CanHandle(KinesisEvent request)
    {
        return request?.Records != null &&
               request.Records.Count > 0 &&
               request.Records[0].EventSource == "aws:kinesis";
    }

    /// <summary>
    /// Handles the event by running the batch through the Kinesis streaming application and writing
    /// the resulting <see cref="KinesisBatchResponse"/>.
    /// </summary>
    /// <param name="request">The Kinesis event batch extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(KinesisEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _application.HandleAsync(request, serviceResolverFactory);
        MapResponse(context, response);
    }
}
