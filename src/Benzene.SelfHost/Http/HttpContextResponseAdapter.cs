using Benzene.Abstractions.Response;
using Benzene.Core;
using Benzene.Core.Helper;

namespace Benzene.SelfHost.Http;

public class HttpContextResponseAdapter : IBenzeneResponseAdapter<HttpContext>
{
    public void SetResponseHeader(HttpContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.Response.Headers, headerKey, headerValue);
    }

    public void SetContentType(HttpContext context, string contentType)
    {
        SetResponseHeader(context, Constants.ContentTypeHeader, contentType);
    }

    public void SetStatusCode(HttpContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.Response.StatusCode = Convert.ToInt32(statusCode);
    }

    public void SetBody(HttpContext context, string body)
    {
        context.EnsureResponseExists();
        context.Response.Body = body;
    }

    public string GetBody(HttpContext context)
    {
        context.EnsureResponseExists();
        return context.Response.Body;
    }
}
