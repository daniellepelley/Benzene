using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.Aws.XRay;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> UseXRayTracing<TContext>(this IMiddlewarePipelineBuilder<TContext> app, bool isEnabled)
    {
        if (isEnabled)
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        return app;
    }
}
