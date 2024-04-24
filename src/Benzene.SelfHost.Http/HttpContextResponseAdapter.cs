using System.Text;
using Benzene.Abstractions.Response;

namespace Benzene.SelfHost.Http;

public class HttpContextResponseAdapter : IBenzeneResponseAdapter<HttpContext>
{
    private string _body = "";

    public void SetContentType(HttpContext context, string contentType)
    {
        context.HttpListenerContext.Response.ContentType = contentType;
    }

    public void SetStatusCode(HttpContext context, string statusCode)
    {
        context.HttpListenerContext.Response.StatusCode = Convert.ToInt32(statusCode);
    }

    public void SetBody(HttpContext context, string body)
    {
        _body = body;
    }

    public string GetBody(HttpContext context)
    {
        return _body;
    }
    public async Task FinalizeAsync(HttpContext context)
    {
        if (context.HttpListenerContext.Response.StatusCode != 204)
        {
            var bytes = Encoding.UTF8.GetBytes(_body);
            await context.HttpListenerContext.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        context.HttpListenerContext.Response.Close();
    }
}
