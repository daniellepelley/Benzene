using System;
using Benzene.Abstractions.Response;
using Benzene.Core.Helper;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayResponseAdapter : IBenzeneResponseAdapter<ApiGatewayContext>
{
    public void SetResponseHeader(ApiGatewayContext context, string headerKey, string headerValue)
    {
        context.EnsureResponseExists();
        DictionaryUtils.Set(context.ApiGatewayProxyResponse.Headers, headerKey, headerValue);
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
}