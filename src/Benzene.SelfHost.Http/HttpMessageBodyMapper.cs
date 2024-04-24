using Benzene.Abstractions.Mappers;

namespace Benzene.SelfHost.Http;

public class HttpMessageBodyMapper : IMessageBodyMapper<HttpContext>
{
    public string GetBody(HttpContext context)
    {
        using var reader = new StreamReader(context.HttpListenerContext.Request.InputStream);
        return reader.ReadToEnd();
    }
}


