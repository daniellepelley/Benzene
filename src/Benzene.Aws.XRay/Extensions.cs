using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Benzene.Abstractions.Middleware;

namespace Benzene.Aws.XRay;

/// <summary>
/// Provides extension methods for enabling AWS X-Ray tracing on a middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Conditionally registers AWS X-Ray instrumentation for all AWS SDK service clients.
    /// </summary>
    /// <typeparam name="TContext">The context type that the pipeline operates on.</typeparam>
    /// <param name="app">The pipeline builder to configure X-Ray tracing for.</param>
    /// <param name="isEnabled">Whether X-Ray tracing should be enabled.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// When <paramref name="isEnabled"/> is true, this registers X-Ray instrumentation globally
    /// for the process (via <c>AWSSDKHandler.RegisterXRayForAllServices()</c>), not just for this pipeline.
    /// Use <see cref="XRayProcessTimerFactory"/> with <c>UseTimer</c> to record individual pipeline
    /// stages as X-Ray subsegments.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseXRayTracing<TContext>(this IMiddlewarePipelineBuilder<TContext> app, bool isEnabled)
    {
        if (isEnabled)
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        return app;
    }
}
