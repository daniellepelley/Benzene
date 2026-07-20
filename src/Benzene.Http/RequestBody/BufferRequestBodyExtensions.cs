using Benzene.Abstractions.Middleware;

namespace Benzene.Http.RequestBody;

/// <summary>
/// Pipeline-builder extension for wiring <see cref="BufferRequestBodyMiddleware{TContext}"/>. HTTP
/// transports whose body lives behind a network stream call this as the first middleware in their
/// pipeline (auto-wired by their <c>UseHttp(...)</c>), so the body is read asynchronously up front
/// and the synchronous body getter never blocks a thread.
/// </summary>
public static class BufferRequestBodyExtensions
{
    /// <summary>
    /// Adds <see cref="BufferRequestBodyMiddleware{TContext}"/> to the pipeline. Requires the
    /// transport to have registered an <see cref="IHttpRequestBodyReader{TContext}"/> and a scoped
    /// <see cref="HttpRequestBodyBuffer"/>.
    /// </summary>
    /// <typeparam name="TContext">The transport-specific context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseBufferedRequestBody<TContext>(this IMiddlewarePipelineBuilder<TContext> app)
    {
        return app.Use(resolver => new BufferRequestBodyMiddleware<TContext>(
            resolver.GetService<IHttpRequestBodyReader<TContext>>(),
            resolver.GetService<HttpRequestBodyBuffer>()));
    }
}
