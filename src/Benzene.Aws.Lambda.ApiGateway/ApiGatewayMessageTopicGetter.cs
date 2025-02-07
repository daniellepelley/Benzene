using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

public class ApiGatewayMessageTopicGetter : IMessageTopicGetter<ApiGatewayContext>
{
    private readonly IRouteFinder _routeFinder;

    public ApiGatewayMessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(ApiGatewayContext context)
    {
        var route = _routeFinder.Find(context.ApiGatewayProxyRequest.HttpMethod, context.ApiGatewayProxyRequest.Path);
        return new Topic(route?.Topic);
    }
}
