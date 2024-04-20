using Benzene.Abstractions.Response;
using Benzene.Core.Helper;

namespace Benzene.Core.DirectMessage;

public class DirectMessageResponseAdapter : IBenzeneResponseAdapter<DirectMessageContext>
{
    public void SetResponseHeader(DirectMessageContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.DirectMessageResponse.Headers, headerKey, headerValue);
    }

    public void SetContentType(DirectMessageContext context, string contentType)
    {
        SetResponseHeader(context, "content-type", contentType);
    }

    public void SetStatusCode(DirectMessageContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.DirectMessageResponse.StatusCode = statusCode;
    }

    public void SetBody(DirectMessageContext context, string body)
    {
        context.EnsureResponseExists();
        context.DirectMessageResponse.Message = body;
    }

    public string GetBody(DirectMessageContext context)
    {
        context.EnsureResponseExists();
        return context.DirectMessageResponse.Message;
    }
}