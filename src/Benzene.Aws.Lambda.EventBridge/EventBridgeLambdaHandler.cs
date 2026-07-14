using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.EventBridge;

/// <summary>
/// Routes AWS Lambda invocations whose payload is an EventBridge event to the EventBridge pipeline.
/// </summary>
/// <remarks>
/// An EventBridge payload is identified by the presence of both <c>detail-type</c> and <c>source</c> —
/// no other Lambda event source carries those fields (SQS/SNS/S3 payloads have <c>Records</c>;
/// API Gateway and BenzeneMessage payloads have neither). Non-matching payloads defer to the next
/// middleware, allowing the other event source adapters to attempt the same raw payload. The
/// invocation is fire-and-forget (EventBridge targets are invoked asynchronously), so no response
/// is written.
/// </remarks>
public class EventBridgeLambdaHandler : AwsLambdaMiddlewareRouter<EventBridgeEvent>
{
    private readonly IMiddlewareApplication<EventBridgeEvent> _application;

    public EventBridgeLambdaHandler(
        IMiddlewareApplication<EventBridgeEvent> application,
        IServiceResolver serviceResolver)
        : base(serviceResolver)
    {
        _application = application;
    }

    protected override bool CanHandle(EventBridgeEvent request)
    {
        return request?.DetailType != null && request.Source != null;
    }

    protected override async Task HandleFunction(EventBridgeEvent request, AwsEventStreamContext context, IServiceResolverFactory serviceResolverFactory)
    {
        await _application.HandleAsync(request, serviceResolverFactory);
    }
}
