using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;

namespace Benzene.Core.Logging;

public static class ContextDictionaryBuilderExtensions
{
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, string key, string value)
    {
        return source.With(key, (_, _) => value);
    }
    
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, IDictionary<string, string> dictionary)
    {
        return source.With((_, _) => dictionary);
    }

    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, string key, Func<IServiceResolver, string> valueAction)
    {
        return source.With(key, (resolver, _) => valueAction(resolver));
    }
    
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, Func<IServiceResolver, IDictionary<string, string>> dictionaryAction)
    {
        return source.With((resolver, _) => dictionaryAction(resolver));
    }

    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, string key, Func<IServiceResolver, TContext, string> valueAction)
    {
        return source.With((resolver, context) =>
            new Dictionary<string, string> { { key, valueAction(resolver, context) } });
    }
    
    public static IContextDictionaryBuilder<TContext> With<TContext>(this IContextDictionaryBuilder<TContext> source, Func<IServiceResolver, TContext,  IDictionary<string, string>> dictionaryAction)
    {
        return source.With(dictionaryAction);
    }
}