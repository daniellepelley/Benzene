using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.AspNet.Core;

/// <summary>
/// Extracts the message topic by matching the request's method and path against the registered HTTP routes.
/// </summary>
public class AspNetMessageTopicGetter : IMessageTopicGetter<AspNetContext>
{
    private readonly IRouteFinder _routeFinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetMessageTopicGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request's method and path to a route.</param>
    public AspNetMessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    /// <summary>
    /// Gets the topic associated with the matched route, or a topic with a null ID if no route matches.
    /// </summary>
    /// <param name="context">The HTTP context to extract the topic from.</param>
    /// <returns>The topic.</returns>
    public ITopic GetTopic(AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpContext.Request.Method, context.HttpContext.Request.Path);
        return new Topic(route?.Topic);
    }
}
