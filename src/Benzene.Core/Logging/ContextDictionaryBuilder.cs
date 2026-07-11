using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

/// <summary>
/// Builds a dictionary from a typed context by composing multiple extraction functions.
/// </summary>
/// <typeparam name="TContext">The type of context to extract dictionary values from.</typeparam>
public class ContextDictionaryBuilder<TContext> : IContextDictionaryBuilder<TContext>
{
    private readonly List<Func<IServiceResolver, TContext, IDictionary<string, string>>> _list = new();

    /// <summary>
    /// Adds a function that extracts dictionary values from the context.
    /// </summary>
    /// <param name="dictionaryAction">The function that extracts dictionary values from the context.</param>
    /// <returns>The builder for method chaining.</returns>
    public IContextDictionaryBuilder<TContext> With(Func<IServiceResolver, TContext,  IDictionary<string, string>> dictionaryAction)
    {
        _list.Add(dictionaryAction);
        return this;
    }

    /// <summary>
    /// Builds the final dictionary by executing all registered extraction functions and merging results.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="context">The context to extract values from.</param>
    /// <returns>A dictionary containing all extracted key-value pairs with non-empty values.</returns>
    public IDictionary<string, string> Build(IServiceResolver serviceResolver, TContext context)
    {
        return _list.Select(
                func => func(serviceResolver, context))
            .SelectMany(x => x)
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
