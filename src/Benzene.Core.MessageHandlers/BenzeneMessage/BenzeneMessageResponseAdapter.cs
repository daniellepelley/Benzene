﻿using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Helper;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

public class BenzeneMessageResponseAdapter : IBenzeneResponseAdapter<BenzeneMessageContext>
{
    public void SetResponseHeader(BenzeneMessageContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.BenzeneMessageResponse.Headers, headerKey, headerValue);
    }

    public void SetContentType(BenzeneMessageContext context, string contentType)
    {
        SetResponseHeader(context, Constants.ContentTypeHeader, contentType);
    }

    public void SetStatusCode(BenzeneMessageContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.BenzeneMessageResponse.StatusCode = statusCode;
    }

    public void SetBody(BenzeneMessageContext context, string body)
    {
        context.EnsureResponseExists();
        context.BenzeneMessageResponse.Body = body;
    }

    public string GetBody(BenzeneMessageContext context)
    {
        context.EnsureResponseExists();
        return context.BenzeneMessageResponse.Body;
    }

    public Task FinalizeAsync(BenzeneMessageContext context)
    {
        return Task.CompletedTask;
    }
}