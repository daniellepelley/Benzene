using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Mappers;
using Benzene.Http.Routing;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayMessageTopicMapper : IMessageTopicMapper<ApiGatewayContext>
{
    private readonly IRouteFinder _routeFinder;

    public ApiGatewayMessageTopicMapper(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(ApiGatewayContext context)
    {
        var route = _routeFinder.Find(context.ApiGatewayProxyRequest.HttpMethod, context.ApiGatewayProxyRequest.Path);
        return new Topic(route?.Topic);
    }
}
