using Benzene.Abstractions.Response;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspNetResponseAdapter : IBenzeneResponseAdapter<AspNetContext>
{
    private string _body = string.Empty;
    private int _statusCode = 404;

    public void SetResponseHeader(AspNetContext context, string headerKey, string headerValue)
    {
        context.HttpContext.Response.Headers.Add(headerKey, headerValue);
    }

    public void SetContentType(AspNetContext context, string contentType)
    {
        context.HttpContext.Response.Headers["content-type"] = contentType;
    }

    public void SetStatusCode(AspNetContext context, string statusCode)
    {
        _statusCode = Convert.ToInt32(statusCode);
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
        context.HttpContext.Response.StatusCode = _statusCode;
        await context.HttpContext.Response.WriteAsync(_body);
        await context.HttpContext.Response.Body.FlushAsync();
    }
}