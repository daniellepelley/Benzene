using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.Azure.AspNet;

public class AspNetMessageTopicGetter : IMessageTopicGetter<AspNetContext>
{
    private readonly IRouteFinder _routeFinder;

    public AspNetMessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(AspNetContext context)
    {
        var route = _routeFinder.Find(context.HttpRequest.Method, context.HttpRequest.Path);
        return new Topic(route?.Topic);
    }
}
