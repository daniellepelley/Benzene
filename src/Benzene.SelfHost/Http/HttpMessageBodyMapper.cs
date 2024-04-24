using Benzene.Abstractions.Mappers;

namespace Benzene.SelfHost.Http;

public class HttpMessageBodyMapper : IMessageBodyMapper<HttpContext>
{
    public string GetBody(HttpContext context)
    {
        return context.Request.Body;
    }
}
