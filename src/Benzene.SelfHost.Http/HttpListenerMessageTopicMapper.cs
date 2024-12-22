using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;

namespace Benzene.SelfHost.Http;

public class HttpListenerMessageTopicMapper : IMessageTopicMapper<SelfHostHttpContext>
{
    private readonly IRouteFinder _routeFinder;

    public HttpListenerMessageTopicMapper(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(SelfHostHttpContext context)
    {
        var route = _routeFinder.Find(context.HttpListenerContext.Request.HttpMethod, context.HttpListenerContext.Request.RawUrl);
        return new Topic(route?.Topic);
    }
}
