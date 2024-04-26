namespace Benzene.AspNet.Core;

public class AspNetHttpRequestAdapter : IHttpRequestAdapter<AspNetContext>
{
    public HttpRequest2 Map(AspNetContext context)
    {
        return new HttpRequest2
        {
            Path = context.HttpContext.Request.Path,
            Method = context.HttpContext.Request.Method,
            Headers = context.HttpContext.Request.Headers.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value.ToString())
        };
    }
}