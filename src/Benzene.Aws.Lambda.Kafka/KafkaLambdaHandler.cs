using System.Threading.Tasks;
using Amazon.Lambda.KafkaEvents;
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
/// next middleware. Kafka events don't return a response — this is a fire-and-forget pattern.
/// </remarks>
public class KafkaLambdaHandler : AwsLambdaMiddlewareRouter<KafkaEvent>
{
    private readonly IMiddlewareApplication<KafkaEvent> _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaLambdaHandler"/> class.
    /// </summary>
    /// <param name="application">The Kafka application to dispatch matching invocations to.</param>
    /// <param name="serviceResolver">The service resolver for the current invocation scope.</param>
    public KafkaLambdaHandler(
        IMiddlewareApplication<KafkaEvent> application,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
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
    /// Handles the event by running it through the Kafka application. No response is written.
    /// </summary>
    /// <param name="request">The Kafka event extracted from the invocation payload.</param>
    /// <param name="context">The AWS event stream context for this invocation.</param>
    /// <param name="serviceResolverFactory">The service resolver factory for the current invocation.</param>
    protected override async Task HandleFunction(KafkaEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        // var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        // setCurrentTransport.SetTransport("kafka");
        await _application.HandleAsync(request, serviceResolverFactory);
    }
}
