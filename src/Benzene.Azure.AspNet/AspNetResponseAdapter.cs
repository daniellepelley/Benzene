using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Azure.AspNet;

public class AspNetResponseAdapter : IBenzeneResponseAdapter<AspNetContext>
{
    public void SetResponseHeader(AspNetContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        context.HttpRequest.HttpContext.Response.Headers.Add(headerKey, headerValue);
    }

    public void SetContentType(AspNetContext context, string contentType)
    {
        context.ContentResult.ContentType = contentType;
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

    public Task FinalizeAsync(AspNetContext context)
    {
       return Task.CompletedTask; 
    }
}


