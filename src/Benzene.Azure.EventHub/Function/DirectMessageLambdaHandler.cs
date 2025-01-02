using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.Middleware;

namespace Benzene.Azure.EventHub.Function;

public class BenzeneMessageLambdaHandler : MiddlewareRouter<BenzeneMessageRequest, EventHubContext>
{
    private readonly BenzeneMessageApplication _directMessageApplication;
    private readonly ISerializer _serializer;

    public BenzeneMessageLambdaHandler(
        IMiddlewarePipeline<BenzeneMessageContext> pipeline,
        IServiceResolver serviceResolver)
    : base(serviceResolver)
    {
        _serializer = serviceResolver.GetService<ISerializer>();
        _directMessageApplication = new BenzeneMessageApplication(pipeline);
    }

    protected override bool CanHandle(BenzeneMessageRequest request)
    {
        return request?.Topic != null;
    }

    protected override async Task HandleFunction(BenzeneMessageRequest request, EventHubContext context, IServiceResolverFactory serviceResolverFactory)
    {
        await _directMessageApplication.HandleAsync(request, serviceResolverFactory);
    }

    protected override BenzeneMessageRequest TryExtractRequest(EventHubContext context)
    {
        try
        {
            return _serializer.Deserialize<BenzeneMessageRequest>(context.EventData.EventBody.ToString());
        }
        catch
        {
            return default;
        }
    }
}
