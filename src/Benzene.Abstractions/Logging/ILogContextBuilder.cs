using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides a fluent builder for configuring structured log scopes based on request and response data.
/// This interface enables context-specific logging metadata to be captured automatically during request processing.
/// </summary>
/// <typeparam name="TContext">The type of context (e.g., HttpContext, MessageContext) used to extract log properties.</typeparam>
public interface ILogContextBuilder<TContext> : IRegisterDependency
{
    /// <summary>
    /// Configures properties to include in the log scope when processing a request.
    /// </summary>
    /// <param name="dictionaryAction">A function that extracts log properties from the request context.</param>
    /// <returns>The builder for method chaining.</returns>
    ILogContextBuilder<TContext> OnRequest(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);

    /// <summary>
    /// Configures properties to include in the log scope when processing a response.
    /// </summary>
    /// <param name="dictionaryAction">A function that extracts log properties from the response context.</param>
    /// <returns>The builder for method chaining.</returns>
    ILogContextBuilder<TContext> OnResponse(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);

    /// <summary>
    /// Builds the state object for the request-phase log scope from the configured request properties.
    /// The result is passed to <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope{TState}"/>.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for accessing dependencies.</param>
    /// <param name="context">The request context to extract properties from.</param>
    /// <returns>The scope state as a key/value dictionary.</returns>
    IReadOnlyDictionary<string, object> BuildRequestScope(IServiceResolver serviceResolver, TContext context);

    /// <summary>
    /// Builds the state object for the response-phase log scope from the configured response properties.
    /// The result is passed to <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope{TState}"/>.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for accessing dependencies.</param>
    /// <param name="context">The response context to extract properties from.</param>
    /// <returns>The scope state as a key/value dictionary.</returns>
    IReadOnlyDictionary<string, object> BuildResponseScope(IServiceResolver serviceResolver, TContext context);
}
