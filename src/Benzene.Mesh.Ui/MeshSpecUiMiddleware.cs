using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Http;

namespace Benzene.Mesh.Ui;

/// <summary>
/// Transport-agnostic HTTP middleware that serves the mesh-hosted Spec Explorer page
/// (<see cref="MeshSpecUiPage"/>) — the per-service spec view <c>mesh-ui.html</c> links to. Works on
/// any Benzene HTTP transport because it drives the transport-neutral
/// <see cref="IBenzeneResponseAdapter{TContext}"/> directly rather than depending on any one web
/// framework, exactly like <see cref="MeshUiMiddleware{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// The default served path (<see cref="MeshUiExtensions.DefaultSpecUiPath"/>) ends in <c>.html</c> on
/// purpose: <c>mesh-ui.html</c>'s per-service <em>spec</em> link is the single relative link
/// <c>mesh-spec-ui.html?service=…</c>, which must resolve the same whether the mesh UI is served as a
/// static file (sitting next to the artifacts) or from this pipeline (e.g. at <c>/mesh-ui</c>). Serving
/// the page at <c>/mesh-spec-ui.html</c> makes that one link work in both worlds. On a matching
/// GET/HEAD this writes the page as <c>text/html</c> and short-circuits; any other request is passed
/// to <c>next</c>.
/// </remarks>
public class MeshSpecUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly string _path;
    private readonly string _html;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshSpecUiMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="path">The path the spec UI is served from (for example <c>/mesh-spec-ui.html</c>).</param>
    /// <param name="manifestUrl">The default URL the page resolves <c>services/{name}.json</c> against.</param>
    /// <param name="httpRequestAdapter">Adapter used to read the request method and path.</param>
    /// <param name="responseAdapter">Adapter used to write the HTML response.</param>
    public MeshSpecUiMiddleware(string path, string manifestUrl,
        IHttpRequestAdapter<TContext> httpRequestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter)
    {
        _path = NormalizePath(path);
        _html = MeshSpecUiPage.GetHtml(manifestUrl);
        _httpRequestAdapter = httpRequestAdapter;
        _responseAdapter = responseAdapter;
    }

    /// <summary>
    /// Gets the name of the middleware.
    /// </summary>
    public string Name => "MeshSpecUi";

    /// <summary>
    /// Serves the mesh spec UI page for a matching GET/HEAD request to the configured path; otherwise
    /// passes the request to the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var request = _httpRequestAdapter.Map(context);
        var method = request.Method?.ToLowerInvariant();

        if ((method == "get" || method == "head") && NormalizePath(request.Path) == _path)
        {
            _responseAdapter.SetStatusCode(context, "200");
            _responseAdapter.SetContentType(context, "text/html; charset=utf-8");
            _responseAdapter.SetBody(context, _html);
            await _responseAdapter.FinalizeAsync(context);
            return;
        }

        await next();
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var trimmed = path.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = "/" + trimmed;
        }

        if (trimmed.Length > 1 && trimmed.EndsWith('/'))
        {
            trimmed = trimmed.TrimEnd('/');
        }

        return trimmed.ToLowerInvariant();
    }
}
