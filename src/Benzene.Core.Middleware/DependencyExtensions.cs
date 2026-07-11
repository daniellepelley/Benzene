using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides extension methods for dependency injection container configuration related to middleware.
/// </summary>
public static class DependencyExtensions
{
    /// <summary>
    /// Registers core Benzene middleware services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service container to configure.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Registers the default middleware factory and service resolver, which are required for
    /// middleware pipeline execution.
    /// </remarks>
    public static IBenzeneServiceContainer AddBenzeneMiddleware(this IBenzeneServiceContainer services)
    {
        services.TryAddSingleton<IMiddlewareFactory, DefaultMiddlewareFactory>();
        services.AddServiceResolver();
        return services;
    }

    /// <summary>
    /// Creates and configures a middleware pipeline using a fluent builder pattern.
    /// </summary>
    /// <typeparam name="TContext">The context type that the pipeline operates on.</typeparam>
    /// <param name="source">The dependency registration source.</param>
    /// <param name="action">The action that configures the pipeline builder.</param>
    /// <returns>The built middleware pipeline ready for execution.</returns>
    public static IMiddlewarePipeline<TContext> CreateMiddlewarePipeline<TContext>(this IRegisterDependency source,
        Action<IMiddlewarePipelineBuilder<TContext>> action)
    {
        var middlewareBuilder = new MiddlewarePipelineBuilder<TContext>(source);
        action(middlewareBuilder);
        return middlewareBuilder.Build();
    }
}
