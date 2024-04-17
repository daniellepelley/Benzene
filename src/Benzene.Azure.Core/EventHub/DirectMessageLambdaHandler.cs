using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.DirectMessage;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Core.EventHub;

public class DirectMessageLambdaHandler : MiddlewareRouter<DirectMessageRequest, EventHubContext>
{
    private readonly DirectMessageApplication _directMessageApplication;
    private readonly ISerializer _serializer;

    public DirectMessageLambdaHandler(
        IMiddlewarePipeline<DirectMessageContext> pipeline,
        IServiceResolver serviceResolver)
    :base(serviceResolver)
    {
        _serializer = serviceResolver.GetService<ISerializer>();
        _directMessageApplication = new DirectMessageApplication(pipeline);
    }

    protected override bool CanHandle(DirectMessageRequest request)
    {
        return request?.Topic != null;
    }

    protected override async Task HandleFunction(DirectMessageRequest request, EventHubContext context, IServiceResolver serviceResolver)
    {
        var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        setCurrentTransport.SetTransport("direct");
        await _directMessageApplication.HandleAsync(request, serviceResolver);
    }

    protected override DirectMessageRequest TryExtractRequest(EventHubContext context)
    {
        try
        {
            return _serializer.Deserialize<DirectMessageRequest>(context.EventData.EventBody.ToString());
        }
        catch
        {
            return default;
        }
    }
}
