using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Exceptions;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides the platform-neutral registration and middleware for <see cref="IBenzeneInvocation"/>.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>
    /// Registers the services required to resolve <see cref="IBenzeneInvocation"/>. Called automatically
    /// by <see cref="UseBenzeneInvocation{TContext}"/>; you don't normally need to call this directly.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    public static IBenzeneServiceContainer AddBenzeneInvocation(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<IBenzeneInvocationAccessor, BenzeneInvocationAccessor>();
        services.TryAddScoped<IBenzeneInvocation>(x =>
            x.GetService<IBenzeneInvocationAccessor>().Invocation
            ?? throw new BenzeneException(
                "IBenzeneInvocation was requested before the pipeline's UseBenzeneInvocation() middleware populated it for this invocation."));
        return services;
    }

    /// <summary>
    /// Adds middleware that builds and exposes an <see cref="IBenzeneInvocation"/> for the duration of
    /// the request, so it can be injected wherever needed.
    /// </summary>
    /// <typeparam name="TContext">The context type that the middleware operates on.</typeparam>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <param name="factory">Builds the invocation for a given context. Hosting platforms expose their
    /// own zero-argument overload of this method that supplies this factory (e.g.
    /// <c>Benzene.Aws.Lambda.Core</c>'s or <c>Benzene.AspNet.Core</c>'s <c>UseBenzeneInvocation()</c>).</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseBenzeneInvocation<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Func<IServiceResolver, TContext, IBenzeneInvocation> factory)
    {
        app.Register(x => x.AddBenzeneInvocation());
        return app.Use("BenzeneInvocation", resolver => async (context, next) =>
        {
            resolver.GetService<IBenzeneInvocationAccessor>().Invocation = factory(resolver, context);
            await next();
        });
    }
}
