using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Abstractions;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.TestHelpers;

namespace Benzene.Aws.Lambda.ApiGateway.TestHelpers;

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

    // Overloads that dispatch straight off the IAwsLambdaEntryPoint that BuildAwsLambdaHost() returns,
    // so the gold-standard one-liner - BenzeneTestHost.Create<StartUp>().BuildAwsLambdaHost()
    // .SendApiGatewayAsync(request) - compiles without a manual `new AwsLambdaBenzeneTestHost(...)`
    // wrap, exactly like GCP's BuildGooglePubSubFunctionHost().SendPubSubAsync(...). The caller owns
    // the entry point's lifetime, so this transient host wrapper does NOT dispose it.
    public static Task<APIGatewayProxyResponse> SendApiGatewayAsync(this IAwsLambdaEntryPoint source, APIGatewayProxyRequest apiGatewayProxyRequest)
    {
        return new AwsLambdaBenzeneTestHost(source).SendApiGatewayAsync(apiGatewayProxyRequest);
    }

    public static Task<APIGatewayProxyResponse> SendApiGatewayAsync<T>(this IAwsLambdaEntryPoint source, IHttpBuilder<T> httpBuilder)
        where T : class
    {
        return new AwsLambdaBenzeneTestHost(source).SendApiGatewayAsync(httpBuilder);
    }
}