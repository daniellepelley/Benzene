using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Aws.Lambda.XRay;

/// <summary>
/// Registration for direct-to-X-Ray per-middleware tracing.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers <see cref="XRayMiddlewareWrapper"/> so every middleware in every pipeline is wrapped in
    /// its own AWS X-Ray subsegment (named after the middleware, annotated
    /// <c>benzene_transport</c>/<c>benzene_topic</c>/<c>benzene_version</c>/<c>benzene_handler</c> where
    /// resolvable), nested under the Lambda's X-Ray segment. This is the modern reintroduction of the old
    /// <c>Benzene.Aws.XRay</c> package's <c>UseXRayTracing()</c>, updated to today's
    /// <see cref="IMiddlewareWrapper"/> model - the X-Ray equivalent of <c>AddActivityPerMiddleware()</c>.
    /// </summary>
    /// <remarks>
    /// Idempotent (a registration guard means it never double-wraps a middleware) and composes with
    /// <c>Benzene.Diagnostics.AddDiagnostics()</c>/<c>AddActivityPerMiddleware()</c> - wire both to emit
    /// X-Ray subsegments <em>and</em> OpenTelemetry <c>Activity</c> spans from the same pipeline. Requires
    /// no exporter or collector: the AWS X-Ray SDK sends subsegments to the X-Ray daemon the Lambda
    /// runtime already provides when active tracing is on. Off Lambda it is a safe no-op.
    /// </remarks>
    /// <param name="services">The container to register the wrapper against.</param>
    /// <returns>The same container, for chaining.</returns>
    public static IBenzeneServiceContainer AddXRayTracing(this IBenzeneServiceContainer services)
    {
        if (!services.IsTypeRegistered<XRayMiddlewareWrapper>())
        {
            services.AddSingleton<XRayMiddlewareWrapper>();
            services.AddSingleton<IMiddlewareWrapper, XRayMiddlewareWrapper>();
        }

        return services;
    }
}
