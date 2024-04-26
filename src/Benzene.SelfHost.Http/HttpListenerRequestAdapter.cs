using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class HttpListenerRequestAdapter : IHttpRequestAdapter<SelfHostHttpContext>
{
    public HttpRequest Map(SelfHostHttpContext context)
    {
        return new HttpRequest
        {
            Path = context.HttpListenerContext.Request.RawUrl,
            Method = context.HttpListenerContext.Request.HttpMethod,
            Headers = context.HttpListenerContext.Request.Headers.ToDictionary()
        };
    }
}