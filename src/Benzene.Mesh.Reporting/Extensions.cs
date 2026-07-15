using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Reporting;

/// <summary>
/// Provides extension methods for registering mesh self-reporting with a Benzene service container
/// and middleware pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers <see cref="HttpMeshReportPublisher"/> (as <c>IMeshReportPublisher</c>) against the
    /// given options. Use this when the reporting service isn't colocated with the aggregator's own
    /// storage - see <c>Benzene.Mesh.Aggregator.ArtifactStoreMeshReportPublisher</c> for the
    /// colocated alternative.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">Where to post reports.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddMeshHttpReporting(this IBenzeneServiceContainer services, MeshReportingOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IMeshReportPublisher>(resolver => new HttpMeshReportPublisher(resolver.GetService<HttpClient>(), resolver.GetService<MeshReportingOptions>()));
        return services;
    }

    /// <summary>
    /// Registers <see cref="MeshSelfReportOptions"/> and the singleton <see cref="MeshSelfReportState"/>
    /// <see cref="MeshSelfReportMiddleware{TContext}"/> needs. Requires an <c>IMeshReportPublisher</c>
    /// already registered (via <see cref="AddMeshHttpReporting"/> or
    /// <c>Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator</c>'s default) - resolving
    /// <see cref="MeshSelfReportMiddleware{TContext}"/> without one fails at DI-resolution time, the
    /// same way any other Benzene wiring with a missing prerequisite registration would.
    /// </summary>
    /// <param name="services">The service container to register with.</param>
    /// <param name="options">How to name this service, build its report, and throttle publishing.</param>
    /// <returns>The service container for method chaining.</returns>
    public static IBenzeneServiceContainer AddMeshSelfReport(this IBenzeneServiceContainer services, MeshSelfReportOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<MeshSelfReportState>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="MeshSelfReportMiddleware{TContext}"/> to the pipeline. Requires
    /// <see cref="AddMeshSelfReport"/> (and an <c>IMeshReportPublisher</c> registration) already
    /// called on the same container.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshSelfReport<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.Use(resolver => new MeshSelfReportMiddleware<TContext>(
            resolver.GetService<IMeshReportPublisher>(),
            resolver.GetService<MeshSelfReportOptions>(),
            resolver.GetService<MeshSelfReportState>()));
    }
}
