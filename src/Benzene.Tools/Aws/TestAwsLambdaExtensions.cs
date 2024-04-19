using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.DirectMessage;
using Benzene.Core.MiddlewareBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Tools.Aws;

public static class TestAwsLambdaExtensions
{
    public static TestAwsLambdaHost BuildHost<TStartUp>
        (this TestAwsLambdaStartUp<TStartUp> source)
        where TStartUp : IStartUp<IServiceCollection, IMiddlewarePipelineBuilder<AwsEventStreamContext>>
    {
        return new TestAwsLambdaHost(source.Build());
    }

    public static Task<DirectMessageResponse> SendDirectMessageAsync(this TestAwsLambdaHost source, DirectMessageRequest directMessageRequest)
    {
        return source.SendEventAsync<DirectMessageResponse>(directMessageRequest);
    }

    public static Task<DirectMessageResponse> SendDirectMessageAsync(this TestAwsLambdaHost source, MessageBuilder messageBuilder)
    {
        return source.SendDirectMessageAsync(messageBuilder.AsDirectMessage());
    }
    
    public static Task<SQSBatchResponse> SendSqsAsync(this TestAwsLambdaHost source, SQSEvent sqsEvent)
    {
        return source.SendEventAsync<SQSBatchResponse>(sqsEvent);
    }

    public static Task<SQSBatchResponse> SendSqsAsync(this TestAwsLambdaHost source, MessageBuilder messageBuilder)
    {
        return source.SendSqsAsync(messageBuilder.AsSqs());
    }
    
    public static Task<APIGatewayProxyResponse> SendApiGatewayAsync(this TestAwsLambdaHost source, APIGatewayProxyRequest apiGatewayProxyRequest, ILambdaContext? lambdaContext = null)
    {
        return source.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest, lambdaContext);
    }

    public static Task<APIGatewayProxyResponse> SendApiGatewayAsync(this TestAwsLambdaHost source, HttpBuilder httpBuilder)
    {
        return source.SendApiGatewayAsync(httpBuilder.AsApiGatewayRequest());
    }

    public static TestAwsLambdaHost BuildHost(this InlineAwsLambdaStartUp source)
    {
        return new TestAwsLambdaHost(source.Build());
    }
}
