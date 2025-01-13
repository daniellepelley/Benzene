using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.AspNet.Core;

public class AspNetMessageTopicGetter : IMessageTopicGetter<AspNetContext>
{
    private readonly IRouteFinder _routeFinder;

    public AspNetMessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpContext.Request.Method, context.HttpContext.Request.Path);
        return new Topic(route?.Topic);
    }
}