using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Http;
using Benzene.Mesh.Aggregator;

namespace Benzene.Examples.AzureMesh.Mesh;

/// <summary>
/// Serves the mesh aggregator's generated catalog artifacts (<c>manifest.json</c>, <c>topology.json</c>,
/// <c>services/*.json</c>) over HTTP by reading them from the <see cref="IMeshArtifactStore"/> (S3),
/// so the Mesh UI — a static page that fetches <c>manifest.json</c> relatively — has something to load.
/// Mirrors <c>Benzene.Spec.Ui</c>'s middleware pattern (drives the response adapter directly).
/// </summary>
public class MeshArtifactMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext
{
    private readonly IMeshArtifactStore _store;
    private readonly IHttpRequestAdapter<TContext> _requestAdapter;
    private readonly IBenzeneResponseAdapter<TContext> _responseAdapter;

    public MeshArtifactMiddleware(
        IMeshArtifactStore store,
        IHttpRequestAdapter<TContext> requestAdapter,
        IBenzeneResponseAdapter<TContext> responseAdapter)
    {
        _store = store;
        _requestAdapter = requestAdapter;
        _responseAdapter = responseAdapter;
    }

    public string Name => "MeshArtifacts";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var request = _requestAdapter.Map(context);
        var method = request.Method?.ToLowerInvariant();
        var key = (request.Path ?? "/").TrimStart('/');

        if ((method == "get" || method == "head") && IsArtifact(key))
        {
            var content = await _store.TryReadAsync(key);
            _responseAdapter.SetStatusCode(context, content == null ? "404" : "200");
            _responseAdapter.SetContentType(context, "application/json");
            _responseAdapter.SetBody(context, content ?? "{\"error\":\"not found\"}");
            await _responseAdapter.FinalizeAsync(context);
            return;
        }

        await next();
    }

    private static bool IsArtifact(string key)
        => key is "manifest.json" or "topology.json" or "topics.json" or "registry.json"
           || (key.StartsWith("services/", StringComparison.Ordinal) && key.EndsWith(".json", StringComparison.Ordinal));
}

/// <summary>Pipeline wiring for <see cref="MeshArtifactMiddleware{TContext}"/>.</summary>
public static class MeshArtifactExtensions
{
    /// <summary>Serves the mesh catalog artifacts from the registered <see cref="IMeshArtifactStore"/>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseMeshArtifacts<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : IHttpContext
    {
        return app.Use(resolver => new MeshArtifactMiddleware<TContext>(
            resolver.GetService<IMeshArtifactStore>(),
            resolver.GetService<IHttpRequestAdapter<TContext>>(),
            resolver.GetService<IBenzeneResponseAdapter<TContext>>()));
    }
}
