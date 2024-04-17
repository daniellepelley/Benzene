using Benzene.Abstractions.Mappers;

namespace Benzene.SelfHost.Http;

public class HttpMessageBodyMapper : IMessageBodyMapper<HttpContext>
{
    public string GetMessage(HttpContext context)
    {
        return context.Request.Body;
    }
}
