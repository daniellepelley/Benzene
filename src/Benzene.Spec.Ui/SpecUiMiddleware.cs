using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Http;

namespace Benzene.Spec.Ui;

/// <summary>
/// Transport-agnostic HTTP middleware that serves the Benzene Spec Explorer page. Works on any
/// Benzene HTTP transport — AWS Lambda API Gateway, Azure Functions, ASP.NET Core, or the self-host
/// server — because it drives the transport-neutral <see cref="IBenzeneResponseAdapter{TContext}"/>
/// directly rather than depending on any one web framework.
/// </summary>
/// <typeparam name="TContext">The HTTP context type.</typeparam>
/// <remarks>
/// On a matching request it writes the page as <c>text/html</c> and short-circuits the pipeline; any
/// other request is passed to <c>next</c>. Add it before the message-handler middleware. It emits the
/// response by calling <see cref="IBenzeneResponseAdapter{TContext}.SetContentType"/> /
/// <c>SetStatusCode</c> / <c>SetBody</c> then <see cref="IBenzeneResponseAdapter{TContext}.FinalizeAsync"/> —
/// deliberately bypassing the message-result path, whose body handler forces <c>application/json</c>.
/// </remarks>
public class SpecUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly string _path;
    private readonly string _html;
    private readonly IHttpRequestAdapter<TContext> _httpRequestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecUiMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="path">The path the UI is served from (for example <c>/spec-ui</c>).</param>
    /// <param name="specUrl">The URL the page fetches the Benzene spec JSON from.</param>
    /// <param name="httpRequestAdapter">Adapter used to read the request method and path.</param>
    /// <param name="responseAdapter">Adapter used to write the HTML response.</param>
    public SpecUiMiddleware(string path, string specUrl,
        IHttpRequestAdapter<TContext> httpRequestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter)
    {
        _path = NormalizePath(path);
        _html = SpecUiPage.GetHtml(specUrl);
        _httpRequestAdapter = httpRequestAdapter;
        _responseAdapter = responseAdapter;
    }

    /// <summary>
    /// Gets the name of the middleware.
    /// </summary>
    public string Name => "SpecUi";

    /// <summary>
    /// Serves the spec UI page for a matching GET/HEAD request to the configured path; otherwise
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
