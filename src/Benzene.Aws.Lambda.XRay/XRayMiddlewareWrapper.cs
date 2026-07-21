using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Aws.Lambda.XRay;

/// <summary>
/// Wraps every middleware in the pipeline in an <see cref="XRayMiddlewareDecorator{TContext}"/>, opening
/// an AWS X-Ray subsegment per pipeline stage. Registered by
/// <see cref="DependencyInjectionExtensions.AddXRayTracing"/>. The direct-to-X-Ray counterpart of
/// <c>Benzene.Diagnostics.ActivityMiddlewareWrapper</c>.
/// </summary>
public class XRayMiddlewareWrapper : IMiddlewareWrapper
{
    /// <inheritdoc />
    public IMiddleware<TContext> Wrap<TContext>(IServiceResolver serviceResolver, IMiddleware<TContext> middleware)
    {
        return new XRayMiddlewareDecorator<TContext>(middleware, serviceResolver);
    }
}
