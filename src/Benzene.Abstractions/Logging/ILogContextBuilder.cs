using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides a fluent builder for configuring structured log contexts based on request and response data.
/// This interface enables context-specific logging metadata to be captured automatically during request processing.
/// </summary>
/// <typeparam name="TContext">The type of context (e.g., HttpContext, MessageContext) used to extract log properties.</typeparam>
public interface ILogContextBuilder<TContext> : IRegisterDependency
{
    /// <summary>
    /// Configures properties to include in the log context when processing a request.
    /// </summary>
    /// <param name="dictionaryAction">A function that extracts log properties from the request context.</param>
    /// <returns>The builder for method chaining.</returns>
    ILogContextBuilder<TContext> OnRequest(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);

    /// <summary>
    /// Configures properties to include in the log context when processing a response.
    /// </summary>
    /// <param name="dictionaryAction">A function that extracts log properties from the response context.</param>
    /// <returns>The builder for method chaining.</returns>
    ILogContextBuilder<TContext> OnResponse(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);

    /// <summary>
    /// Creates a log context scope for the request phase using the configured request properties.
    /// </summary>
    /// <param name="benzeneLogContext">The log context to create the scope in.</param>
    /// <param name="serviceResolver">The service resolver for accessing dependencies.</param>
    /// <param name="context">The request context to extract properties from.</param>
    /// <returns>A disposable scope that removes the context when disposed.</returns>
    IDisposable CreateForRequest(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver,
        TContext context);

    /// <summary>
    /// Creates a log context scope for the response phase using the configured response properties.
    /// </summary>
    /// <param name="benzeneLogContext">The log context to create the scope in.</param>
    /// <param name="serviceResolver">The service resolver for accessing dependencies.</param>
    /// <param name="context">The response context to extract properties from.</param>
    /// <returns>A disposable scope that removes the context when disposed.</returns>
    IDisposable CreateForResponse(IBenzeneLogContext benzeneLogContext, IServiceResolver serviceResolver,
        TContext context);
}
