using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Mappers;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class HttpMessageTopicMapper : IMessageTopicMapper<HttpContext>
{
    private readonly IRouteFinder _routeFinder;

    public HttpMessageTopicMapper(IRouteFinder routeFinder)
    {
        _routeFinder = routeFinder;
    }

    public ITopic GetTopic(HttpContext context)
    {
        var route = _routeFinder.Find(context.Request.Method, context.Request.Path);
        return new Topic(route?.Topic);
    }
}
