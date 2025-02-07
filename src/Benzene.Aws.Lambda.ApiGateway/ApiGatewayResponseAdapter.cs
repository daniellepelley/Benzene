using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Helper;

namespace Benzene.Aws.Lambda.ApiGateway;

public class ApiGatewayResponseAdapter : IBenzeneResponseAdapter<ApiGatewayContext>
{
    public void SetResponseHeader(ApiGatewayContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.ApiGatewayProxyResponse.Headers, headerKey, headerValue);
    }

    public void SetContentType(ApiGatewayContext context, string contentType)
    {
        SetResponseHeader(context, Constants.ContentTypeHeader, contentType);
    }

    public void SetStatusCode(ApiGatewayContext context, string statusCode)
    {
        context.EnsureResponseExists();
        context.ApiGatewayProxyResponse.StatusCode = Convert.ToInt32(statusCode);
    }

    public void SetBody(ApiGatewayContext context, string body)
    {
        context.EnsureResponseExists();
        context.ApiGatewayProxyResponse.Body = body;
    }

    public string GetBody(ApiGatewayContext context)
    {
        context.EnsureResponseExists();
        return context.ApiGatewayProxyResponse.Body;
    }

    public Task FinalizeAsync(ApiGatewayContext context)
    {
        return Task.CompletedTask;
    }
}