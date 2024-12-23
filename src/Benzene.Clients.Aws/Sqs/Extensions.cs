using Amazon.SQS;
using Benzene.Abstractions.Middleware;

namespace Benzene.Clients.Aws.Sqs;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<SqsSendMessageContext> UseSqsClient(
        this IMiddlewarePipelineBuilder<SqsSendMessageContext> app, IAmazonSQS amazonSqs)
    {
        return app.Use(resolver => new SqsClientMiddleware(amazonSqs));
    }
}