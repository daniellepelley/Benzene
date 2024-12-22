using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;

namespace Benzene.Azure.AspNet;

public class AspNetMessageTopicMapper : IMessageTopicMapper<AspNetContext>
{
    private readonly IRouteFinder _routeFinder;

    public AspNetMessageTopicMapper(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpRequest.Method, context.HttpRequest.Path);
        return new Topic(route?.Topic);
    }
}
