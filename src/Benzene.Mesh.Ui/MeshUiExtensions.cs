using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Http;

namespace Benzene.Mesh.Ui;

/// <summary>
/// Pipeline wiring for the Benzene Mesh Explorer - a catalog viewer for a service mesh's
/// generated <c>manifest.json</c>/<c>services/*.json</c> artifacts (see
/// <c>Benzene.Mesh.Aggregator</c>). Transport-agnostic, so it works on AWS Lambda, Azure
/// Functions, ASP.NET Core, or the self-host server alike - though the primary deployment target
/// is a plain static file host, not a Benzene pipeline at all (see <see cref="MeshUiMiddleware{TContext}"/>).
/// </summary>
public static class MeshUiExtensions
{
    /// <summary>The default path the mesh UI is served from.</summary>
    public const string DefaultPath = "/mesh-ui";

    /// <summary>
    /// The default URL the UI fetches <c>manifest.json</c> from - a relative path, since the
    /// realistic case is the HTML sitting in the same directory as the aggregator's generated
    /// artifacts (unlike <c>Benzene.Spec.Ui</c>'s default, which points at a route on the same
    /// live service).
    /// </summary>
    public const string DefaultManifestUrl = "manifest.json";

    /// <summary>
    /// Serves the Benzene Mesh Explorer page at <paramref name="path"/> on any HTTP pipeline. This
    /// is a secondary convenience - the primary deployment target is a plain static file host
    /// serving <c>mesh-ui.html</c> alongside the aggregator's published artifacts, needing no
    /// Benzene pipeline at all. Add this before the message-handler middleware.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="path">The path to serve the UI from. Defaults to <see cref="DefaultPath"/>.</param>
    /// <param name="manifestUrl">
    /// The URL the UI fetches <c>manifest.json</c> from. Defaults to <see cref="DefaultManifestUrl"/>.
    /// </param>
    /// <returns>The middleware pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshUi<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string path = DefaultPath,
        string manifestUrl = DefaultManifestUrl)
        where TContext : IHttpContext
    {
        app.Register(x =>
            x.AddSingleton(resolver => new MeshUiMiddleware<TContext>(
                path, manifestUrl,
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>()
            )));

        return app.Use<TContext, MeshUiMiddleware<TContext>>();
    }
}
