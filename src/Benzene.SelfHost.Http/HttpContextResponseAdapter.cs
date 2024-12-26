using System.Text;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.SelfHost.Http;

public class HttpContextResponseAdapter : IBenzeneResponseAdapter<SelfHostHttpContext>
{
    private string _body = "";

    public void SetResponseHeader(SelfHostHttpContext context, string headerKey, string headerValue)
    {
        context.HttpListenerContext.Response.Headers.Add(headerKey, headerValue);
    }

    public void SetContentType(SelfHostHttpContext context, string contentType)
    {
        context.HttpListenerContext.Response.ContentType = contentType;
    }

    public void SetStatusCode(SelfHostHttpContext context, string statusCode)
    {
        context.HttpListenerContext.Response.StatusCode = Convert.ToInt32(statusCode);
    }

    public void SetBody(SelfHostHttpContext context, string body)
    {
        _body = body;
    }

    public string GetBody(SelfHostHttpContext context)
    {
        return _body;
    }
    public async Task FinalizeAsync(SelfHostHttpContext context)
    {
        if (context.HttpListenerContext.Response.StatusCode != 204)
        {
            var bytes = Encoding.UTF8.GetBytes(_body);
            await context.HttpListenerContext.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        context.HttpListenerContext.Response.Close();
    }
}
