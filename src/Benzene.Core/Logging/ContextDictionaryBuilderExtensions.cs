using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

/// <summary>
/// Provides extension methods for building context dictionaries with a fluent API.
/// </summary>
public static class ContextDictionaryBuilderExtensions
{
    /// <summary>
    /// Adds a static key-value pair to the dictionary builder.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="source">The builder to extend.</param>
    /// <param name="key">The dictionary key.</param>
    /// <param name="value">The dictionary value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, string key, string value)
    {
        return source.With(key, (_, _) => value);
    }
    
    /// <summary>
    /// Adds a static dictionary to the dictionary builder.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="source">The builder to extend.</param>
    /// <param name="dictionary">The dictionary to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, IDictionary<string, string> dictionary)
    {
        return source.With((_, _) => dictionary);
    }

    /// <summary>
    /// Adds a key with a value resolved from a function using the service resolver.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="source">The builder to extend.</param>
    /// <param name="key">The dictionary key.</param>
    /// <param name="valueAction">The function to resolve the value.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, string key, Func<IServiceResolver, string> valueAction)
    {
        return source.With(key, (resolver, _) => valueAction(resolver));
    }
    
    /// <summary>
    /// Adds a dictionary resolved from a function using the service resolver.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="source">The builder to extend.</param>
    /// <param name="dictionaryAction">The function to resolve the dictionary.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        return source.With((resolver, _) => dictionaryAction(resolver));
    }

    /// <summary>
    /// Adds a key with a value resolved from a function using the service resolver and context.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="source">The builder to extend.</param>
    /// <param name="key">The dictionary key.</param>
    /// <param name="valueAction">The function to resolve the value from the service resolver and context.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        return source.With((resolver, context) =>
            new Dictionary<string, string> { { key, valueAction(resolver, context) } });
    }
    
    /// <summary>
    /// Adds a dictionary resolved from a function using the service resolver and context.
    /// </summary>
    /// <typeparam name="TContext">The type of context.</typeparam>
    /// <param name="source">The builder to extend.</param>
    /// <param name="dictionaryAction">The function to resolve the dictionary from the service resolver and context.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, Func<IServiceResolver, TContext,  IDictionary<string, string>> dictionaryAction)
    {
        return source.With(dictionaryAction);
    }
}