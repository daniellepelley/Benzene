using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.DynamoDb;

/// <summary>
/// Routes AWS Lambda invocations whose payload deserializes into a <see cref="DynamoDbEvent"/> to
/// the DynamoDB Streams middleware pipeline.
/// </summary>
/// <remarks>
/// Added to the outer <see cref="AwsEventStreamContext"/> pipeline by
/// <see cref="Extensions.UseDynamoDb"/>. It only handles the invocation if the event has records
/// whose source is <c>aws:dynamodb</c>; otherwise it defers to the next middleware.
/// </remarks>
public class DynamoDbLambdaHandler : AwsLambdaMiddlewareRouter<DynamoDbEvent>
{
    // Source-generated JSON metadata for this handler's event types, built once per process, replacing
    // the base router's reflection serializer so the first (cold) invocation skips the metadata build.
    private static readonly SourceGeneratorLambdaJsonSerializer<DynamoDbJsonSerializerContext> SourceGenJsonSerializer = new();

    private readonly IMiddlewareApplication<DynamoDbEvent, DynamoDbBatchResponse> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoDbLambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The DynamoDB application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public DynamoDbLambdaHandler(
        IMiddlewareApplication<DynamoDbEvent, DynamoDbBatchResponse> application,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        JsonSerializer = SourceGenJsonSerializer;
        _application = application;
    }

    /// <summary>
    /// Determines whether the deserialized request looks like a DynamoDB Streams event.
    /// </summary>
    /// <param name="request">The deserialized request.</param>
    /// <returns>True if the event has at least one record sourced from DynamoDB; otherwise, false.</returns>
    protected override bool CanHandle(DynamoDbEvent request)
    {
        return request?.Records != null &&
               request.Records.Any() &&
               request.Records[0].EventSource == "aws:dynamodb";
    }

    /// <summary>
    /// Handles the event by running it through the DynamoDB application and writing the batch response.
    /// </summary>
    /// <param name="request">The stream batch event extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context to write the response to.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(DynamoDbEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        var response = await _application.HandleAsync(request, serviceResolverFactory);
        MapResponse(context, response);
    }
}
