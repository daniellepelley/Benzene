using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Http.Routing;

namespace Benzene.SelfHost.Http;

public class HttpListenerMessageTopicGetter : IMessageTopicGetter<SelfHostHttpContext>
{
    private readonly IRouteFinder _routeFinder;

    public HttpListenerMessageTopicGetter(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(SelfHostHttpContext context)
    {
        var route = _routeFinder.Find(context.HttpListenerContext.Request.HttpMethod, context.HttpListenerContext.Request.RawUrl);
        return new Topic(route?.Topic);
    }
}
