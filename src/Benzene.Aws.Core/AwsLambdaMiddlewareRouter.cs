using Amazon.Lambda.Serialization.SystemTextJson;
using Benzene.Abstractions.DI;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Core;

public abstract class AwsLambdaMiddlewareRouter<TRequest> : MiddlewareRouter<TRequest, AwsEventStreamContext>
{
    protected DefaultLambdaJsonSerializer JsonSerializer = new();

    protected AwsLambdaMiddlewareRouter(IServiceResolver serviceResolver)
        :base(serviceResolver)
    {
    }

    protected override TRequest TryExtractRequest(AwsEventStreamContext context)
    {
        context.Stream.Position = 0;

        try
        {
            return JsonSerializer.Deserialize<TRequest>(context.Stream);
        }
        catch
        {
            return default;
        }
    }

    protected void MapResponse(AwsEventStreamContext context, object response)
    {
        JsonSerializer.Serialize(response, context.Response);
        if (context.Response != null)
        {
            context.Response.Position = 0;
        }
    }
}
