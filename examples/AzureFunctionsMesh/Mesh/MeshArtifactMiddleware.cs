using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Http;
using Benzene.Http.Cors;
using Benzene.Mesh.Aggregator;

namespace Benzene.Examples.AzureFunctionsMesh.Mesh;

/// <summary>
/// Serves the mesh aggregator's generated catalog artifacts (<c>manifest.json</c>, <c>topology.json</c>,
/// <c>services/*.json</c>, <c>asyncapi.json</c>) over HTTP by reading them from the
/// <see cref="IMeshArtifactStore"/> (Blob Storage), so the Mesh UI — a static page that fetches
/// <c>manifest.json</c> relatively — has something to load. Generic over <see cref="IHttpContext"/>, so it
/// runs unchanged on the Azure Functions HTTP context (<c>AspNetContext</c>). Mirrors the AzureMesh Web
/// App's copy — the two hosting models serve the catalog identically.
/// </summary>
public class MeshArtifactMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly IMeshArtifactStore _store;
    private readonly IHttpRequestAdapter<TContext> _requestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;
    private readonly CorsSettings? _corsSettings;
    private readonly CorsOriginChecker _corsOriginChecker = new();

    public MeshArtifactMiddleware(
        IMeshArtifactStore store,
        IHttpRequestAdapter<TContext> requestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter,
        CorsSettings? corsSettings = null)
    {
        _store = store;
        _requestAdapter = requestAdapter;
        _responseAdapter = responseAdapter;
        _corsSettings = corsSettings;
    }

    public string Name => "MeshArtifacts";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var request = _requestAdapter.Map(context);
        var method = request.Method?.ToLowerInvariant();
        var key = (request.Path ?? "/").TrimStart('/');

        if (!IsArtifact(key))
        {
            await next();
            return;
        }

        // Answer the CORS preflight (OPTIONS) for the artifact paths so a browser fetch that does
        // trigger one still gets through — a plain GET fetch (the Studio case) never preflights.
        if (method == "options")
        {
            ApplyCorsHeaders(context, request);
            _responseAdapter.SetStatusCode(context, "204");
            await _responseAdapter.FinalizeAsync(context);
            return;
        }

        if (method == "get" || method == "head")
        {
            var content = await _store.TryReadAsync(key);
            ApplyCorsHeaders(context, request);
            _responseAdapter.SetStatusCode(context, content == null ? "404" : "200");
            _responseAdapter.SetContentType(context, "application/json");
            _responseAdapter.SetBody(context, content ?? "{\"error\":\"not found\"}");
            await _responseAdapter.FinalizeAsync(context);
            return;
        }

        await next();
    }

    // Dogfoods Benzene.Http's CORS support: reuse the same CorsOriginChecker/CorsSettings the framework's
    // CorsMiddleware uses to validate and echo the Origin. No-op unless a CorsSettings was wired.
    private void ApplyCorsHeaders(TContext context, Benzene.Http.HttpRequest request)
    {
        if (_corsSettings?.AllowedDomains is not { Length: > 0 })
        {
            return;
        }

        var origin = _corsOriginChecker.MatchOrigin(_corsSettings.AllowedDomains, request.AsLowerCase());
        if (origin == null)
        {
            return;
        }

        _responseAdapter.SetResponseHeader(context, "Vary", "Origin");
        _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Origin", origin);
        _responseAdapter.SetResponseHeader(context, "Access-Control-Allow-Methods", "GET,HEAD,OPTIONS");
    }

    private static bool IsArtifact(string key)
        => key is "manifest.json" or "topology.json" or "topics.json" or "registry.json" or "asyncapi.json"
                  or "usage.json" or "annotations.json"
           || (key.StartsWith("services/", StringComparison.Ordinal) && key.EndsWith(".json", StringComparison.Ordinal));
}

/// <summary>Pipeline wiring for <see cref="MeshArtifactMiddleware{TContext}"/>.</summary>
public static class MeshArtifactExtensions
{
    /// <summary>
    /// Serves the mesh catalog artifacts from the registered <see cref="IMeshArtifactStore"/>. Pass a
    /// <paramref name="corsSettings"/> to also stamp CORS headers on the artifact responses.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshArtifacts<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, CorsSettings? corsSettings = null)
        where TContext : IHttpContext
    {
        return app.Use(resolver => new MeshArtifactMiddleware<TContext>(
            resolver.GetService<IMeshArtifactStore>(),
            resolver.GetService<IHttpRequestAdapter<TContext>>(),
            resolver.GetService<IBenzeneResponseAdapter<TContext>>(),
            corsSettings));
    }
}
