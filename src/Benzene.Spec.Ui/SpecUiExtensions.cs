using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Http;

namespace Benzene.Spec.Ui;

/// <summary>
/// Pipeline wiring for the Benzene Spec Explorer — the equivalent of <c>UseSwaggerUI()</c>, but for
/// the Benzene message spec (topics, payloads, and validation rules) and transport-agnostic, so it
/// works on AWS Lambda, Azure Functions, ASP.NET Core, or the self-host server alike.
/// </summary>
public static class SpecUiExtensions
{
    /// <summary>The default path the spec UI is served from.</summary>
    public const string DefaultPath = "/spec-ui";

    /// <summary>The default URL the UI fetches the Benzene spec JSON from.</summary>
    public const string DefaultSpecUrl = "/spec?type=benzene";

    /// <summary>
    /// Serves the Benzene Spec Explorer page at <paramref name="path"/> on any HTTP pipeline. The page
    /// fetches the spec JSON from <paramref name="specUrl"/> on load, so expose a Benzene <c>spec</c>
    /// endpoint (for example <c>UseSpec()</c> mapped to <c>GET /spec</c>) alongside it. Add this before
    /// the message-handler middleware.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="path">The path to serve the UI from. Defaults to <see cref="DefaultPath"/>.</param>
    /// <param name="specUrl">
    /// The URL the UI fetches the spec JSON from. Defaults to <see cref="DefaultSpecUrl"/>. Use the
    /// <c>benzene</c> spec type for the topic/payload/validation view this UI is designed around.
    /// </param>
    /// <returns>The middleware pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseSpecUi<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        string path = DefaultPath,
        string specUrl = DefaultSpecUrl)
        where TContext : IHttpContext
    {
        app.Register(x =>
            x.AddSingleton(resolver => new SpecUiMiddleware<TContext>(
                path, specUrl,
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>()
            )));

        return app.Use<TContext, SpecUiMiddleware<TContext>>();
    }
}
