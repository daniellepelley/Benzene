using Benzene.Abstractions.Response;
using Benzene.Core.Helper;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspNetResponseAdapter : IBenzeneResponseAdapter<AspNetContext>
{
    private string _body = string.Empty;
    
    public void SetContentType(AspNetContext context, string contentType)
    {
        context.HttpContext.Response.Headers.Add("content-type", contentType);
    }

    public void SetStatusCode(AspNetContext context, string statusCode)
    {
        context.HttpContext.Response.StatusCode = Convert.ToInt32(statusCode);
    }

    public void SetBody(AspNetContext context, string body)
    {
        _body = body;
    }

    public string GetBody(AspNetContext context)
    {
        return _body;
    }

    public async Task FinalizeAsync(AspNetContext context)
    {
        await context.HttpContext.Response.WriteAsync(_body);
    }
}