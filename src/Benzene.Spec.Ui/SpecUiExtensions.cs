using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Benzene.Spec.Ui;

/// <summary>
/// ASP.NET Core wiring for the Benzene Spec Explorer — the equivalent of <c>app.UseSwaggerUI()</c>,
/// but for the Benzene message spec (topics, payloads, and validation rules).
/// </summary>
public static class SpecUiExtensions
{
    /// <summary>The default path the spec UI is served from.</summary>
    public const string DefaultPath = "/spec-ui";

    /// <summary>The default URL the UI fetches the Benzene spec JSON from.</summary>
    public const string DefaultSpecUrl = "/spec?type=benzene";

    /// <summary>
    /// Serves the Benzene Spec Explorer page at <paramref name="path"/>. The page fetches the spec
    /// JSON from <paramref name="specUrl"/> on load, so expose a Benzene <c>spec</c> endpoint (for
    /// example <c>UseSpec()</c> mapped to <c>GET /spec</c>) alongside it.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="path">The path to serve the UI from. Defaults to <see cref="DefaultPath"/>.</param>
    /// <param name="specUrl">
    /// The URL the UI fetches the spec JSON from. Defaults to <see cref="DefaultSpecUrl"/>. Use the
    /// <c>benzene</c> spec type for the topic/payload/validation view this UI is designed around.
    /// </param>
    /// <returns>The application builder, for chaining.</returns>
    public static IApplicationBuilder UseBenzeneSpecUi(
        this IApplicationBuilder app,
        string path = DefaultPath,
        string specUrl = DefaultSpecUrl)
    {
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        var html = SpecUiPage.GetHtml(specUrl);

        app.Map(normalizedPath, branch => branch.Run(async context =>
        {
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html);
        }));

        return app;
    }
}
