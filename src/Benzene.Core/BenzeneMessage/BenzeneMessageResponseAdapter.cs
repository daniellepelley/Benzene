using Benzene.Abstractions.Response;
using Benzene.Core.Helper;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageResponseAdapter : IBenzeneResponseAdapter<BenzeneMessageContext>
{
    public void SetResponseHeader(BenzeneMessageContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.BenzeneMessageResponse.Headers, headerKey, headerValue);
    }

    public void SetContentType(BenzeneMessageContext context, string contentType)
    {
        SetResponseHeader(context, Core.Constants.ContentTypeHeader, contentType);
    }

    public void SetStatusCode(BenzeneMessageContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.BenzeneMessageResponse.StatusCode = statusCode;
    }

    public void SetBody(BenzeneMessageContext context, string body)
    {
        context.EnsureResponseExists();
        context.BenzeneMessageResponse.Message = body;
    }

    public string GetBody(BenzeneMessageContext context)
    {
        context.EnsureResponseExists();
        return context.BenzeneMessageResponse.Message;
    }
}