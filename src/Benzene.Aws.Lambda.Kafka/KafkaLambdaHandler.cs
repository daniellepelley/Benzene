using System.Threading.Tasks;
using Amazon.Lambda.KafkaEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Kafka;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into a <see cref="KafkaEvent"/> to the Kafka
/// middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by <see cref="Extensions.UseKafka"/>.
/// It only handles the invocation if the event source is <c>aws:kafka</c>; otherwise it defers to the
/// next middleware. Writes back a <see cref="KafkaBatchResponse"/> naming each failed partition's
/// resume offset — honoured by the event source mapping when <c>ReportBatchItemFailures</c> is
/// configured on the trigger, and safely ignored otherwise (only a thrown exception retries then).
/// </remarks>
public class KafkaLambdaHandler : AwsLambdaMiddlewareRouter<KafkaEvent>
{
    // Source-generated JSON metadata for this handler's event types, built once per process, replacing
    // the base router's reflection serializer so the first (cold) invocation skips the metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<KafkaJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly IMiddlewareApplication<KafkaEvent, KafkaBatchResponse> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaLambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The Kafka application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public KafkaLambdaHandler(
        IMiddlewareApplication<KafkaEvent, KafkaBatchResponse> application,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _application = application;
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a Kafka event.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the event source is <c>aws:kafka</c>; otherwise, false.</returns>
    protected override bool CanHandle(KafkaEvent request)
    {
        return request?.EventSource == "aws:kafka";
    }

    /// <summary>
    /// Handles the event by running the batch through the Kafka application and writing the resulting
    /// <see cref="KafkaBatchResponse"/>.
    /// </summary>
    /// <param name="request">The Kafka event extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(KafkaEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _application.HandleAsync(request, serviceResolverFactory);
        MapResponse(context, response);
    }
}
