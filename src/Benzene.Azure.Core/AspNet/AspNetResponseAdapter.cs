using Benzene.Abstractions.Response;

namespace Benzene.Azure.Core.AspNet;

public class AspNetResponseAdapter : IBenzeneResponseAdapter<AspNetContext>
{
    public void SetResponseHeader(AspNetContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        context.ContentResult.ContentType = headerValue;
    }

    public void SetStatusCode(AspNetContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.ContentResult.StatusCode = Convert.ToInt32(statusCode);
    }

    public void SetBody(AspNetContext context, string body)
    {
        context.EnsureResponseExists();
        context.ContentResult.Content = body;
    }

    public string GetBody(AspNetContext context)
    {
        context.EnsureResponseExists();
        return context.ContentResult.Content;
    }
}
