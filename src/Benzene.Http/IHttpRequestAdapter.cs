namespace Benzene.Http;

/// <summary>
/// Defines a contract for adapting transport-specific HTTP context to the Benzene <see cref="HttpRequest"/> model.
/// </summary>
/// <typeparam name="TContext">The transport-specific HTTP context type that implements <see cref="IHttpContext"/>.</typeparam>
/// <remarks>
/// This interface enables transport-agnostic HTTP request processing by converting different
/// HTTP context implementations (ASP.NET Core, AWS Lambda API Gateway, etc.) into a unified
/// <see cref="HttpRequest"/> representation that can be processed by Benzene middleware and handlers.
/// </remarks>
public interface IHttpRequestAdapter<TContext> where TContext : IHttpContext
{
    /// <summary>
    /// Maps a transport-specific HTTP context to the Benzene HTTP request model.
    /// </summary>
    /// <param name="context">The transport-specific HTTP context to adapt.</param>
    /// <returns>A <see cref="HttpRequest"/> representation of the HTTP request.</returns>
    HttpRequest Map(TContext context);
}