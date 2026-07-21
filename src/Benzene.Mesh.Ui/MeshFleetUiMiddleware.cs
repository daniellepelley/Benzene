using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Http;

namespace Benzene.Mesh.Ui;

/// <summary>
/// Serves the Fleet view page (<see cref="MeshFleetUiPage"/>) for GET/HEAD requests to a
/// configured path - the same shape as <see cref="MeshUiMiddleware{TContext}"/>, pointed at a
/// live collector's wire-envelope endpoint instead of published artifacts.
/// </summary>
/// <typeparam name="TContext">The pipeline's HTTP context type.</typeparam>
public class MeshFleetUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly string _path;
    private readonly string _html;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    public MeshFleetUiMiddleware(string path, string envelopeUrl,
        IHttpRequestAdapter<TContext> httpRequestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter)
    {
        _path = NormalizePath(path);
        _html = MeshFleetUiPage.GetHtml(envelopeUrl);
        _httpRequestAdapter = httpRequestAdapter;
        _responseAdapter = responseAdapter;
    }

    /// <summary>Gets the name of the middleware.</summary>
    public string Name => "MeshFleetUi";

    /// <summary>
    /// Serves the fleet page for a matching GET/HEAD request to the configured path; otherwise
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

        // Case- and trailing-slash-insensitive, matching the sibling MeshUiMiddleware/SpecUiMiddleware
        // convention (the final ToLowerInvariant was previously missing here).
        var normalized = trimmed.Length > 1 ? trimmed.TrimEnd('/') : trimmed;
        return normalized.ToLowerInvariant();
    }
}
