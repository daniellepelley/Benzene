using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.DirectMessage;

namespace Benzene.Aws.Core.DirectMessage;

public class DirectMessageLambdaHandler : AwsLambdaHandlerMiddleware<DirectMessageRequest>
{
    private readonly DirectMessageApplication _directMessageApplication;

    public DirectMessageLambdaHandler(
        IMiddlewarePipeline<DirectMessageContext> pipeline,
        IServiceResolver serviceResolver)
    :base(serviceResolver)
    {
        _directMessageApplication = new DirectMessageApplication(pipeline);
    }

    protected override bool CanHandle(DirectMessageRequest request)
    {
        return request?.Topic != null;
    }

    protected override async Task HandleFunction(DirectMessageRequest request, AwsEventStreamContext context, IServiceResolver serviceResolver)
    {
        var setCurrentTransport = serviceResolver.GetService<ISetCurrentTransport>();
        setCurrentTransport.SetTransport("direct");
        var response = await _directMessageApplication.HandleAsync(request, serviceResolver);
        MapResponse(context, response);
    }
}
