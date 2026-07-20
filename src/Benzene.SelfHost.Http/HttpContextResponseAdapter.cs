using System;
using System.Text;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.SelfHost.Http;

public class HttpContextResponseAdapter : IBenzeneResponseAdapter<SelfHostHttpContext>
{
    private string _body = "";
    // When a handler returns a raw binary body, hold the bytes verbatim and write them unmodified at
    // finalize - the string path (and its UTF-8 encode) is bypassed entirely.
    private ReadOnlyMemory<byte>? _bodyBytes;

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
        _bodyBytes = null;
    }

    public void SetBody(SelfHostHttpContext context, ReadOnlyMemory<byte> body)
    {
        // Write the bytes verbatim - no UTF-8 round-trip, which would corrupt a true binary payload.
        _bodyBytes = body;
    }

    public string GetBody(SelfHostHttpContext context)
    {
        return _body;
    }
    public async Task FinalizeAsync(SelfHostHttpContext context)
    {
        if (context.HttpListenerContext.Response.StatusCode != 204)
        {
            var bytes = _bodyBytes ?? Encoding.UTF8.GetBytes(_body);
            // Unlike the Windows http.sys-backed HttpListener, .NET's cross-platform managed
            // implementation does not infer Content-Length from what's written - without it (or
            // SendChunked), a keep-alive client has no way to know where the body ends and hangs
            // waiting for more data that never comes.
            context.HttpListenerContext.Response.ContentLength64 = bytes.Length;
            await context.HttpListenerContext.Response.OutputStream.WriteAsync(bytes);
        }

        context.HttpListenerContext.Response.Close();
    }
}
