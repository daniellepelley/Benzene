using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Http;

namespace Benzene.Mesh.Ui;

/// <summary>
/// Transport-agnostic HTTP middleware that serves the Benzene Mesh Explorer page. Works on any
/// Benzene HTTP transport because it drives the transport-neutral
/// <see cref="IBenzeneResponseAdapter{TContext}"/> directly rather than depending on any one web
/// framework.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// This is a secondary convenience (e.g. local demo/dev, or an aggregator host that wants to
/// self-serve its own dashboard) - the primary deployment target is a plain static file host
/// serving <c>mesh-ui.html</c> alongside the aggregator's published <c>manifest.json</c>/
/// <c>services/*.json</c>, needing no Benzene pipeline at all. On a matching request this writes
/// the page as <c>text/html</c> and short-circuits the pipeline; any other request is passed to
/// <c>next</c>. It emits the response by calling
/// <see cref="IBenzeneResponseAdapter{TContext}.SetContentType"/> / <c>SetStatusCode</c> /
/// <c>SetBody</c> then <see cref="IBenzeneResponseAdapter{TContext}.FinalizeAsync"/> -
/// deliberately bypassing the message-result path, whose body handler forces
/// <c>application/json</c> - matching <c>Benzene.Spec.Ui.SpecUiMiddleware</c>'s exact shape.
/// </remarks>
public class MeshUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly string _path;
    private readonly string _html;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshUiMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="path">The path the UI is served from (for example <c>/mesh-ui</c>).</param>
    /// <param name="manifestUrl">The URL the page fetches <c>manifest.json</c> from.</param>
    /// <param name="httpRequestAdapter">Adapter used to read the request method and path.</param>
    /// <param name="responseAdapter">Adapter used to write the HTML response.</param>
    public MeshUiMiddleware(string path, string manifestUrl,
        IHttpRequestAdapter<TContext> httpRequestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter)
        : this(path, manifestUrl, null, httpRequestAdapter, responseAdapter)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshUiMiddleware{TContext}"/> class, optionally
    /// wiring the live Fleet plane to a wire-envelope endpoint.
    /// </summary>
    /// <param name="path">The path the UI is served from (for example <c>/mesh-ui</c>).</param>
    /// <param name="manifestUrl">The URL the page fetches <c>manifest.json</c> from.</param>
    /// <param name="envelopeUrl">
    /// The wire-envelope endpoint the Fleet plane polls for live <c>mesh:query:*</c> data (same-origin
    /// path or absolute URL). When null/whitespace the page serves the static catalog viewer only.
    /// </param>
    /// <param name="httpRequestAdapter">Adapter used to read the request method and path.</param>
    /// <param name="responseAdapter">Adapter used to write the HTML response.</param>
    public MeshUiMiddleware(string path, string manifestUrl, string? envelopeUrl,
        IHttpRequestAdapter<TContext> httpRequestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter)
    {
        _path = NormalizePath(path);
        _html = MeshUiPage.GetHtml(manifestUrl, envelopeUrl);
        _httpRequestAdapter = httpRequestAdapter;
        _responseAdapter = responseAdapter;
    }

    /// <summary>
    /// Gets the name of the middleware.
    /// </summary>
    public string Name => "MeshUi";

    /// <summary>
    /// Serves the mesh UI page for a matching GET/HEAD request to the configured path; otherwise
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
