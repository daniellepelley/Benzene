using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides extension methods for ILogContextBuilder that simplify the configuration of log context properties.
/// These extensions enable fluent configuration of structured logging metadata for request and response processing.
/// </summary>
public static class LogContextBuilderExtensions
{
    /// <summary>
    /// Adds a static key-value pair to the request log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The static value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, string key, string value)
    {
        return source.OnRequest(key, (_, _) => value);
    }

    /// <summary>
    /// Adds a key with a resolver-dependent value to the request log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="valueAction">A function that uses the service resolver to compute the value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, string> valueAction)
    {
        return source.OnRequest(key, (resolver, _) => valueAction(resolver));
    }

    /// <summary>
    /// Adds a key with a context-dependent value to the request log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="valueAction">A function that uses the service resolver and context to compute the value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        return source.OnRequest((resolver, context) =>
            new Dictionary<string, string> { { key, valueAction(resolver, context) } });
    }

    /// <summary>
    /// Adds multiple static properties to the request log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="dictionary">The properties to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, IDictionary<string, string> dictionary)
    {
        return source.OnRequest((_, _) => dictionary);
    }

    /// <summary>
    /// Adds resolver-dependent properties to the request log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="dictionaryAction">A function that uses the service resolver to compute the properties.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnRequest<TContext>(this ILogContextBuilder<TContext> source, Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        return source.OnRequest((resolver, _) => dictionaryAction(resolver));
    }

    /// <summary>
    /// Adds a static key-value pair to the response log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The static value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, string key, string value)
    {
        return source.OnResponse(key, (_, _) => value);
    }

    /// <summary>
    /// Adds a key with a resolver-dependent value to the response log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="valueAction">A function that uses the service resolver to compute the value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, string> valueAction)
    {
        return source.OnResponse(key, (resolver, _) => valueAction(resolver));
    }

    /// <summary>
    /// Adds a key with a context-dependent value to the response log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="key">The property key.</param>
    /// <param name="valueAction">A function that uses the service resolver and context to compute the value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        return source.OnResponse((resolver, context) =>
            new Dictionary<string, string> { { key, valueAction(resolver, context) } });
    }

    /// <summary>
    /// Adds multiple static properties to the response log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="dictionary">The properties to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, IDictionary<string, string> dictionary)
    {
        return source.OnResponse((_, _) => dictionary);
    }

    /// <summary>
    /// Adds resolver-dependent properties to the response log context.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="source">The log context builder.</param>
    /// <param name="dictionaryAction">A function that uses the service resolver to compute the properties.</param>
    /// <returns>The builder for method chaining.</returns>
    public static ILogContextBuilder<TContext> OnResponse<TContext>(this ILogContextBuilder<TContext> source, Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        return source.OnResponse((resolver, _) => dictionaryAction(resolver));
    }
}