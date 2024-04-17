using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Aws.Core.AwsEventStream;

namespace Benzene.Aws.XRay;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseXRayTracing(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, bool isEnabled)
    {
        if (isEnabled)
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        return app;
    }


}
