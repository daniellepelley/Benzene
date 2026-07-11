using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

/// <summary>
/// Builds a dictionary from a typed context for logging purposes using a fluent API.
/// </summary>
/// <typeparam name="TContext">The type of context to extract dictionary values from.</typeparam>
public interface IContextDictionaryBuilder<TContext>
{
    /// <summary>
    /// Adds a function that extracts dictionary values from the context.
    /// </summary>
    /// <param name="dictionaryAction">The function that extracts dictionary values from the context.</param>
    /// <returns>The builder for method chaining.</returns>
    IContextDictionaryBuilder<TContext> With(
        Func<IServiceResolver, TContext, IDictionary<string, string>> dictionaryAction);

    /// <summary>
    /// Builds the final dictionary by executing all registered extraction functions.
    /// </summary>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <param name="context">The context to extract values from.</param>
    /// <returns>A dictionary containing all extracted key-value pairs with non-empty values.</returns>
    IDictionary<string, string> Build(IServiceResolver serviceResolver, TContext context);
}