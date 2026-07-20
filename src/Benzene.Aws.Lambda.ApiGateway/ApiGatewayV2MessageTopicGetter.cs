using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts the message topic from an API Gateway HTTP API v2 request by matching its HTTP method and
/// path (from <c>RequestContext.Http</c>) against registered routes.
/// </summary>
public class ApiGatewayV2MessageTopicGetter : IMessageTopicGetter<ApiGatewayV2Context>
{
    private readonly IRouteFinder _routeFinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2MessageTopicGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request to a registered topic.</param>
    public ApiGatewayV2MessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    /// <summary>
    /// Gets the topic for the given API Gateway v2 context.
    /// </summary>
    /// <param name="context">The API Gateway v2 context to extract the topic from.</param>
    /// <returns>The matched topic, or a topic with a null ID if no route matches.</returns>
    public ITopic GetTopic(ApiGatewayV2Context context)
    {
        var route = _routeFinder.Find(context.Method, context.Path);
        return new Topic(route?.Topic);
    }
}
