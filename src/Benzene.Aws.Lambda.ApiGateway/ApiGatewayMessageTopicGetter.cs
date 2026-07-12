using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts the message topic from an API Gateway request by matching its HTTP method and path
/// against registered routes.
/// </summary>
public class ApiGatewayMessageTopicGetter : IMessageTopicGetter<ApiGatewayContext>
{
    private readonly IRouteFinder _routeFinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayMessageTopicGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request to a registered topic.</param>
    public ApiGatewayMessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    /// <summary>
    /// Gets the topic for the given API Gateway context.
    /// </summary>
    /// <param name="context">The API Gateway context to extract the topic from.</param>
    /// <returns>The matched topic, or a topic with a null ID if no route matches.</returns>
    public ITopic GetTopic(ApiGatewayContext context)
    {
        var route = _routeFinder.Find(context.ApiGatewayProxyRequest.HttpMethod, context.ApiGatewayProxyRequest.Path);
        return new Topic(route?.Topic);
    }
}
