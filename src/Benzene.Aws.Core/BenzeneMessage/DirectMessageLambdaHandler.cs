using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers.BenzeneMessage;

namespace Benzene.Aws.Core.BenzeneMessage;

public class BenzeneMessageLambdaHandler : AwsLambdaMiddlewareRouter<BenzeneMessageRequest>
{
    private readonly BenzeneMessageApplication _directMessageApplication;

    public BenzeneMessageLambdaHandler(
        IMiddlewarePipeline<BenzeneMessageContext> pipeline,
        IServiceResolver serviceResolver)
    :base(serviceResolver)
    {
        _directMessageApplication = new BenzeneMessageApplication(pipeline);
    }

    protected override bool CanHandle(BenzeneMessageRequest request)
    {
        return request?.Topic != null;
    }

    protected override async Task HandleFunction(BenzeneMessageRequest request, AwsEventStreamContext context, IServiceResolverFactory serviceResolver)
    {
        // var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        // setCurrentTransport.SetTransport("direct");
        var response = await _directMessageApplication.HandleAsync(request, serviceResolver);
        MapResponse(context, response);
    }
}
