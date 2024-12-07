
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Mappers;
using Benzene.Http.Routing;

namespace Benzene.AspNet.Core;

public class AspNetMessageTopicMapper : IMessageTopicMapper<AspNetContext>
{
    private readonly IRouteFinder _routeFinder;

    public AspNetMessageTopicMapper(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpContext.Request.Method, context.HttpContext.Request.Path);
        return new Topic(route?.Topic);
    }
}