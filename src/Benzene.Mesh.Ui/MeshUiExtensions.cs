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
    /// <param name="envelopeUrl">
    /// The wire-envelope endpoint the page's live Fleet plane polls for <c>mesh:query:*</c> data
    /// (same-origin path or absolute URL). When null (the default) the page serves the static catalog
    /// viewer only; when set — e.g. <see cref="DefaultEnvelopeUrl"/> on a mesh Lambda that also hosts
    /// a <c>Benzene.Mesh.Collector</c> — the catalog is enriched with live health, observed consumers,
    /// recent flows, and a Fleet landing view. This folds in what <c>UseMeshFleetUi</c> served as a
    /// separate page.
    /// </param>
    /// <returns>The middleware pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshUi<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string path = DefaultPath,
        string manifestUrl = DefaultManifestUrl,
        string? envelopeUrl = null)
        where TContext : IHttpContext
    {
        app.Register(x =>
            x.AddSingleton(resolver => new MeshUiMiddleware<TContext>(
                path, manifestUrl, envelopeUrl,
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>()
            )));

        return app.Use<TContext, MeshUiMiddleware<TContext>>();
    }

    /// <summary>
    /// The default path the mesh-hosted Spec Explorer is served from. It ends in <c>.html</c> so the
    /// single relative link <c>mesh-ui.html</c> builds (<c>mesh-spec-ui.html?service=…</c>) resolves
    /// correctly whether the mesh UI is a static file next to the artifacts or served from this
    /// pipeline at <c>/mesh-ui</c> - see <see cref="MeshSpecUiMiddleware{TContext}"/>.
    /// </summary>
    public const string DefaultSpecUiPath = "/mesh-spec-ui.html";

    /// <summary>
    /// Serves the mesh-hosted Spec Explorer page (<see cref="MeshSpecUiPage"/>) at
    /// <paramref name="path"/> - the per-service spec view <c>mesh-ui.html</c>'s <em>spec</em> link
    /// opens. It renders the verbatim spec the aggregator captured into the same-origin
    /// <c>services/{name}.json</c> snapshot, so a mesh service only ever serves JSON, never HTML. Pair
    /// it with <see cref="UseMeshUi{TContext}"/> (and the artifact-serving middleware) on the same
    /// pipeline. Like <see cref="UseMeshUi{TContext}"/> this is a secondary convenience - a static file
    /// host serving <c>mesh-spec-ui.html</c> alongside the artifacts needs no Benzene pipeline at all.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="path">The path to serve the spec UI from. Defaults to <see cref="DefaultSpecUiPath"/>.</param>
    /// <param name="manifestUrl">
    /// The default URL the page resolves <c>services/{name}.json</c> against when opened without a
    /// <c>?manifest=</c> query parameter. Defaults to <see cref="DefaultManifestUrl"/>.
    /// </param>
    /// <returns>The middleware pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshSpecUi<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string path = DefaultSpecUiPath,
        string manifestUrl = DefaultManifestUrl)
        where TContext : IHttpContext
    {
        app.Register(x =>
            x.AddSingleton(resolver => new MeshSpecUiMiddleware<TContext>(
                path, manifestUrl,
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>()
            )));

        return app.Use<TContext, MeshSpecUiMiddleware<TContext>>();
    }

    /// <summary>The default wire-envelope endpoint the mesh UI's live Fleet plane polls, following the
    /// default service standard's <c>/benzene/</c> prefix (docs/specification/design-principles.md §5).
    /// Pass it as <c>UseMeshUi</c>'s <c>envelopeUrl</c> on a mesh host that also serves a
    /// <c>Benzene.Mesh.Collector</c> over the wire envelope.</summary>
    public const string DefaultEnvelopeUrl = "/benzene/invoke";
}
