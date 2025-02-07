using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions;

namespace Benzene.Aws.Lambda.ApiGateway;

public static class BenzeneTestHostExtensions
{
    public static Task<APIGatewayProxyResponse> SendApiGatewayAsync(this IBenzeneTestHost source, APIGatewayProxyRequest apiGatewayProxyRequest)
    {
        return source.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
    }

    public static Task<APIGatewayProxyResponse> SendApiGatewayAsync<T>(this IBenzeneTestHost source, IHttpBuilder<T> httpBuilder)
        where T : class
    {
        return source.SendApiGatewayAsync(httpBuilder.AsApiGatewayRequest());
    }
}