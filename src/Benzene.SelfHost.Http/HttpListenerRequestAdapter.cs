using Benzene.Http;

namespace Benzene.SelfHost.Http;

public class HttpListenerRequestAdapter : IHttpRequestAdapter<SelfHostHttpContext>
{
    public HttpRequest Map(SelfHostHttpContext context)
    {
        return new HttpRequest
        {
            // AbsolutePath, not RawUrl: HttpRequest.Path is contractually the path WITHOUT the query
            // string (the ASP.NET adapter uses Request.Path likewise). RawUrl includes "?query", so a
            // request like POST /benzene-message?trace=1 failed exact-path matching and fell through
            // undispatched on self-host, while it worked on API Gateway/ASP.NET.
            Path = context.HttpListenerContext.Request.Url?.AbsolutePath ?? context.HttpListenerContext.Request.RawUrl,
            Method = context.HttpListenerContext.Request.HttpMethod,
            Headers = context.HttpListenerContext.Request.Headers.ToDictionary()
        };
    }
}